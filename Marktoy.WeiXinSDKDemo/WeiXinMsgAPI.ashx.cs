using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Loogn.WeiXinSDK;
using Loogn.WeiXinSDK.Message;
using System.IO;
using System.Web.Security;
using Loogn.WeiXinSDK.Commom;

namespace Loogn.WeiXinSDKDemo
{
    /// <summary>
    /// 微信消息交互url
    /// </summary>
    public class WeiXinMsgAPI : IHttpHandler
    {
        static string WXToken = "marktoy";
        static string sEncodingAESKey = "wjHs9C6Kg3EzFHRsJxEe6FPlsj3oT4JBlwdxYHIuwoN";
        static string sCorpID = "wx0f57f4406ff8e2d7";
        static string corpid = "wx0f57f4406ff8e2d7";//公司组ID
        static string corpsecret = "va9uxL6KPv3L_jSIFSspJzUf70WhTN1ulHG15A1tj_Gpnn0xUdBoOpW2P0wvoVPK";//公司组sercret
        static string access_token = "3n_ig_E-6AcGAUTa0OOCb41ggxWOSL1Jq1SH5ASMxlfw2pRb075QhNUNzjEn9zPx";

        public void ProcessRequest(HttpContext context)
        {
            if (string.IsNullOrEmpty(context.Request.QueryString["echostr"])) { context.Response.End(); }

            #region --------验证URL

            WXBizMsgCrypt wxcpt = new WXBizMsgCrypt(WXToken, sEncodingAESKey, sCorpID);
            string sVerifyMsgSig = HttpUtility.UrlDecode(context.Request.QueryString["msg_signature"].ToString());
            string sVerifyTimeStamp = HttpUtility.UrlDecode(context.Request.QueryString["timestamp"].ToString());
            string sVerifyNonce = HttpUtility.UrlDecode(context.Request.QueryString["nonce"].ToString());
            string sVerifyEchoStr = HttpUtility.UrlDecode(context.Request.QueryString["echostr"].ToString());
            int ret = 0;
            string sEchoStr = "";
            ret = wxcpt.VerifyURL(sVerifyMsgSig, sVerifyTimeStamp, sVerifyNonce, sVerifyEchoStr, ref sEchoStr);
            Log.WriteLog("\n" + sEchoStr);
            if (ret != 0)
            {
                Log.WriteLog("ERR: VerifyURL fail, ret: " + ret);
                return;
            }
            else
            {
                context.Response.Write(sEchoStr);
                context.Response.End();
            }
            //return;
            #endregion

            if (WeiXin.CheckSignature(sVerifyMsgSig, sVerifyTimeStamp, sVerifyNonce, WXToken))
            {
                try
                {
                    var replyMsg = WeiXin.ReplyMsg().GetXML();
                    //这里可以记录日志
                    Log.WriteLog(replyMsg);
                    context.Response.Write(replyMsg);
                }
                catch (Exception exp)
                {
                    //记录异常
                    Log.WriteLog("error");
                }
            }
            else
            {
                context.Response.Write("");
            }
        }


        /// <summary>
        /// 静态构造方法里注册感兴趣的消息和事件
        /// </summary>
        static WeiXinMsgAPI()
        {

            //注册用户主动发消息，有6种，请查看源码RecMsg.cd类图最下面一排
            //下面以用户发文本消息为例，回复消息可以是 ReplyMsg.cd 最下面一排的任何个
            WeiXin.RegisterMsgHandler<RecTextMsg>(msg =>
            {
                //msg是RecTextMsg类型(你注册哪个类型就是哪个类型)的对象，可以查看用户发了什么，以做回应
                var replay = new ReplyTextMsg
                {
                    Content = "你说了：" + msg.Content
                };
                return replay;
            });
            //当用户发图片时
            WeiXin.RegisterMsgHandler<RecImageMsg>(msg =>
            {
                var downInfo = WeiXin.DownloadMedia(msg.MediaId, access_token);
                //所有由我们主动请求微信的结果对象里，基本都有error属性对象,error!=null才成功，如果不为null，请查看error属性对象
                if (downInfo.error != null)
                {
                    using (var fs = new FileStream("D:\\ab.jpg", FileMode.Create, FileAccess.Write))
                    {
                        var buffer = new byte[1024];
                        int count = 0;
                        while ((count = downInfo.Stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fs.Write(buffer, 0, count);
                        }
                    }
                    return new ReplyTextMsg { Content = "图片不错！" };
                }
                else
                {
                    //这里是错误对象字符串返回了，显然不合适！！
                    return new ReplyTextMsg { Content = downInfo.error.ToString() };
                }



            });



            //注册用户事件消息，有7种，请查看源码 EventMsg.cd类图最下面一排
            //下面是未关注的用户扫描带参数二维码时，用户关注后我们会收到这个事件
            WeiXin.RegisterEventHandler<EventUserScanMsg>(msg =>
            {
                //这里可以用msg.EventKey判断用户扫描哪个二维码，1000就是参数
                if (msg.EventKey.Equals("qrscene_1000"))
                {
                    return new ReplyTextMsg { Content = "您好，谢谢关注!" };
                }
                return new ReplyTextMsg { Content = "您好，谢谢关注！" };
            });

            //用户点击自定义菜单事件，这个可以根据定义时的key分别回应
            WeiXin.RegisterEventHandler<EventClickMsg>(msg =>
            {
                switch (msg.EventKey)
                {
                    case "V1001_GOOD":
                        {
                            //这次回复个图文消息
                            var replay = new ReplyNewsMsg();
                            replay.Articles = new List<ReplyNewsMsg.News>();
                            replay.Articles.Add(new ReplyNewsMsg.News { Url = "http://url.com", PicUrl = "pic.jpg", Title = "title", Description = "Description" });
                            replay.Articles.Add(new ReplyNewsMsg.News { Url = "http://url111.com", PicUrl = "pic111.jpg", Title = "title11", Description = "Description11" });
                            return replay;
                        }
                    case "V1100_aaa":
                        {
                            var mediaInfo = WeiXin.UploadMedia("d:\\abc.mp3", "voice", access_token);
                            if (mediaInfo.error != null)
                            {
                                return new ReplyVoiceMsg { MediaId = mediaInfo.media_id };
                            }
                            else
                            {
                                //这里是错误对象字符串返回了，显然不合适！！
                                return new ReplyTextMsg { Content = mediaInfo.error.ToString() };
                            }
                        }
                    default:
                        return ReplyEmptyMsg.Instance;
                }
            });

            /*
             * 注册其他的消息、事件也是一样的，
             * 用户消息，类名都是以Rec开头的，如RecTextMsg、RecImageMsg
             * 用户事件，类名都是以Event开头的，如EventClickMsg、EventAttendMsg
             * 我们回复的消息都是以Reply开头的，如ReplyTextMsg、ReplyNewsMsg，
             * 
             * 还有一种是发送客服消息，即我们主动给用户发，可以SendMsg.cd类图（在index.aspx给出一个例子）
             * 消息都有注释，都很清晰（因为和接口是一一对应的）
             * 
             */
        }



        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}