
namespace Loogn.WeiXinSDK.Message
{
    /// <summary>
    /// 发送文本消息
    /// </summary>
    public class SendTextMsg:SendBaseMsg
    {
        public override string msgtype
        {
            get { return "text";}
        }

        public pText text { get; set; }

        public class pText
        {
            /// <summary>
            /// 文本消息内容
            /// </summary>
            public string content { get; set; }
        }
    }
}