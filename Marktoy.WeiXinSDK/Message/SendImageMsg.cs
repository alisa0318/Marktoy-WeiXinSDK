﻿
namespace Loogn.WeiXinSDK.Message
{
    /// <summary>
    /// 发送图片消息
    /// </summary>
    public class SendImageMsg:SendBaseMsg
    {
        public override string msgtype
        {
            get { return "image"; }
        }

        public pImage image { get; set; }

        public class pImage
        {
            /// <summary>
            /// 发送的图片的媒体ID
            /// </summary>
            public string media_id { get; set; }
        }
    }
}