
namespace Loogn.WeiXinSDK.Message
{
    /// <summary>
    /// 发送语音消息
    /// </summary>
    public class SendVoiceMsg:SendBaseMsg
    {
        public override string msgtype
        {
            get { return "voice"; }
        }

        public pVoice voice { get; set; }

        public class pVoice
        {
            /// <summary>
            /// 发送的语音的媒体ID
            /// </summary>
            public string media_id { get; set; }
        }
    }
}