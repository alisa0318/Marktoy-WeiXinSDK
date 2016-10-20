weixinsdk
=========
    
    //作者:Marktoy  
    //作者联系方法：
    //email:yanbeike520@163.com

微信公开帐号接口

    public class WeiXinUrl : IHttpHandler
    {
        static string token = "token";
        static string AppId = "AppId";
        static string AppSecret = "AppSecret";
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            var signature = context.Request["signature"] ?? string.Empty;
            var timestamp = context.Request["timestamp"] ?? string.Empty;
            var nonce = context.Request["nonce"] ?? string.Empty;
            
            //var echostr = context.Request.QueryString["echostr"] ?? string.Empty;
            if (WeiXin.CheckSignature(signature, timestamp, nonce, token))
            {
                //context.Response.Write(echostr);
                var replyMsg = WeiXin.ReplyMsg().GetXML();
                context.Response.Write(replyMsg);
            }
            else
            {
                context.Response.Write("fuck you");
            }

        }

        static WeiXinUrl()
        {
            WeiXin.ConfigGlobalCredential(AppId, AppSecret);
            //消息通知事件
            WeiXin.OnReceiveMsgEvent += new WeiXin.ReceiveMsg(WeiXin_OnReceiveMsgEvent);
            
            WeiXin.RegisterMsgHandler<RecTextMsg>(msg =>
            {
                return new ReplyTextMsg { Content = "你说：" + msg.Content };
            });

            WeiXin.RegisterEventHandler<EventAttendMsg>(msg =>
            {
                return new ReplyTextMsg { Content = "谢谢关注！" };
            });
        }

        
        //消息通知事件处理处理程序
        static void WeiXin_OnReceiveMsgEvent(string xml,Dictionary<string, string> dict)
        {
            if (dict.ContainsKey("Event"))
            {
               

            }
            else if (dict.ContainsKey("MsgId"))
            {

              
            }
        }
        
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }