<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Index.aspx.cs" Inherits="Loogn.WeiXinSDKDemo.Index" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <style type="text/css">
        #divbody div
        {
            border: 2px solid #ccc;
            margin: 10px;
            padding: 10px;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <div id="divbody">
    <div>
        1,很多接口都有限制，请仔细看<a href="http://mp.weixin.qq.com/wiki/index.php" target="_blank">接口说明文档</a><br />
        2,注册Global文件里的配置
    </div>
    <div>
        创建菜单<br />
        <asp:Button runat="server" ID="btnCreateMenu" Text="创建菜单" OnClick="btnCreateMenu_Click" />
    </div>
    <div>
        主动发消息<br />
        <asp:TextBox runat="server" ID="tbMsg" TextMode="MultiLine" Text="消息内容" Height="60px"
            Width="250px">消息内容</asp:TextBox><br />
        <asp:Button runat="server" ID="btnSendMsg" Text="主动发消息" OnClick="btnSendMsg_Click" />
    </div>
    <div>
        二维码<br />
        参数：
        <asp:TextBox runat="server" ID="tbQR"></asp:TextBox><br />
        <asp:Image runat="server" ID="imgQR" /><br />
        <asp:Button runat="server" ID="btnCreaeQR" Text="生成二维码" OnClick="btnCreaeQR_Click" />
    </div>

    <div>
        获取关注者列表、得到用户信息<br />
        (这里没输出，直接看代码便知,如果取列表和用户信息)
        <br />
        <asp:Button runat="server" ID="btnGetUser" Text="获取关注者列表" 
            onclick="btnGetUser_Click" />

    </div>

    <div>
        分组<br />
        看事件方法的代码
        <br />
        <asp:Button runat="server" ID="btnGroup" Text="分组" onclick="btnGroup_Click" />
    </div>


    <div>
    多媒体文件在WeiXinMsgAPI.ashx里有</div>

    <div>
    网页授权获取用户基本信息，要详细看接口文档，牵扯到微信和我们网站的交互，<br />
    看明白文档上的注册，调用以下方法即可<br />
    WeiXin.BuildWebCodeUrl<br />
    WeiXin.GetWebAccessToken<br />
    WeiXin.GetWebUserInfo<br />
    注：由于我还没用到网页授权，所以不确定成功与否
    </div>

    </div>
    </form>
</body>
</html>
