using System;
using System.Collections.Generic;
using System.Text;

namespace Loogn.WeiXinSDK.Mass
{
    public abstract class BaseMess
    {
        /// <summary>
        /// 群发的消息类型，图文消息为mpnews，文本消息为text，语音为voice，音乐为music，图片为image，视频为video
        /// </summary>
        public abstract string msgtype { get; }
    }

    public class Media_IdObj
    {
        public string media_id { get; set; }
    }
    
     /// <summary>
    /// 文本群发消息内容
    /// </summary>
    public class Media_textobj
    {
        public string content { get; set; }
    }
    /// <summary>
    /// 按openid视频群发消息内容
    /// </summary>
    public class Media_videoobj
    {
        public string media_id { get; set; }
        public string description { get; set; }
        public string title { get; set; }
    }

}