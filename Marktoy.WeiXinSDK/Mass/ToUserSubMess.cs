using System;
using System.Collections.Generic;
using System.Text;

namespace Loogn.WeiXinSDK.Mass
{
    /// <summary>
    /// 图文消息
    /// </summary>
    public class ToUserMPNewsMess : ToUserMess
    {
        public Media_IdObj mpnews { get; set; }

        public override string msgtype
        {
            get { return "mpnews"; }
        }
    }

    /// <summary>
    /// 文本
    /// </summary>
    public class ToUserTextMess : ToUserMess
    {
        public Media_textobj text { get; set; }
        public override string msgtype
        {
            get { return "text"; }
        }
    }

    /// <summary>
    /// 语音
    /// </summary>
    public class ToUserVoiceMess : ToUserMess
    {
        public Media_IdObj voice { get; set; }
        public override string msgtype
        {
            get { return "voice"; }
        }
    }

    /// <summary>
    /// 图片
    /// </summary>
    public class ToUserImageMess : ToUserMess
    {
        public Media_IdObj image { get; set; }
        public override string msgtype
        {
            get { return "image"; }
        }
    }

    /// <summary>
    /// 按openid群发视频
    /// </summary>
    public class ToUserVideoMess : ToUserMess
    {
        public Media_videoobj video { get; set; }
        public override string msgtype
        {
            get { return "video"; }
        }
    }

    /// <summary>
    /// 分组群发视频
    /// </summary>
    public class ToUserMPVideoMess : ToUserMess
    {
        public Media_IdObj mpvideo { get; set; }
        public override string msgtype
        {
            get { return "mpvideo"; }
        }
    }
}
