using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using Loogn.WeiXinSDK;
using Loogn.WeiXinSDK.Message;
using Loogn.WeiXinSDK.Mass;

namespace Loogn.WeiXinSDKDemo
{
    public partial class Index : System.Web.UI.Page
    {
        public string access_token = "3n_ig_E-6AcGAUTa0OOCb41ggxWOSL1Jq1SH5ASMxlfw2pRb075QhNUNzjEn9zPx";
        protected void Page_Load(object sender, EventArgs e)
        {
            var temp = GetMenuList();
            tbMsg.Text = temp.errcode + "," + temp.errmsg;
        }

        private TmplReturnCode GetMenuList()
        {
            string url = " https://qyapi.weixin.qq.com/cgi-bin/menu/get?access_token=";

            url = url + access_token;

            var retJson = Util.HttpGet2(url);
            return Util.JsonTo<TmplReturnCode>(retJson);
        }

        protected void btnCreateMenu_Click(object sender, EventArgs e)
        {
            var json = File.ReadAllText(Server.MapPath("~/WeiXinMenu.js"));
            var ret = WeiXin.CreateMenu(json, access_token);

            Response.Write(ret.ToString());
        }

        protected void btnSendMsg_Click(object sender, EventArgs e)
        {
            var msg = new SendTextMsg();
            msg.text = new SendTextMsg.pText();
            msg.text.content = tbMsg.Text;
            msg.touser = "@all"; //这个是用户的openid，你可以在用户关注的时候记录在数据库，这里就可以用了

            var retcode = WeiXin.SendMsg(msg, access_token);
            if (retcode.errcode == 0)
            {
                //成功
            }
            else
            {
                //失败
            }
        }

        protected void btnCreaeQR_Click(object sender, EventArgs e)
        {
            var isTemp = true;//是临时的，还是永久的

            var ticket = WeiXin.CreateQRCode(isTemp, int.Parse(tbQR.Text), access_token);
            if (ticket.error == null)
            {
                //出错了，请查看error
            }
            else
            {
                imgQR.ImageUrl = WeiXin.GetQRUrl(ticket.ticket);
            }
        }

        protected void btnGetUser_Click(object sender, EventArgs e)
        {
            //获取关注者列表
            var list = WeiXin.GetAllFollowers(access_token);
            //循环关注者
            foreach (var openid in list.data.openid)
            {
                //得到用户信息
                var userinfo = WeiXin.GetUserInfo(openid, LangType.zh_CN, access_token);
            }


        }

        protected void btnGroup_Click(object sender, EventArgs e)
        {
            //创建分组
            var ginfo = WeiXin.CreateGroup("groupName", access_token);
            //ginfo.error 一如既往的判断error

            //修改分组
            var retcode = WeiXin.UpdateGroup(23, "newGroupName", access_token);
            //移动分组
            int newGroupid = 1;
            retcode = WeiXin.MoveGroup("openid", newGroupid, access_token);


            //得到所有分组
            var groups = WeiXin.GetGroups(access_token);
            //循环分组
            foreach (var g in groups)
            {
                //g.name
            }

            //得到openid的分组
            var gid = WeiXin.GetUserGroup("openid", access_token);
            //gid.error 一如既往的判断error





        }

    }
}