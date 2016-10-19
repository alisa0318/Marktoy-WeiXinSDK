using System.Collections.Generic;

namespace Loogn.WeiXinSDK.Message
{
    /// <summary>
    /// 发送模板消息
    /// 1.模板中参数内容必须以".DATA"结尾，否则视为保留字;
    /// 2.模板保留符号"{{ }}"
    /// </summary>
    public class SendTmplMsg
    {
        public string touser { get; set; }
        /// <summary>
        /// 通过在模板消息功能的模板库中使用需要的模板，可以获得模板ID
        /// </summary>
        public string template_id { get; set; }
        /// <summary>
        /// URL置空，则在发送后，点击模板消息会进入一个空白页面（ios），或无法点击（android）
        /// </summary>
        public string url { get; set; }
        public string topcolor { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> data { get; set; }

        public virtual string GetJSON()
        {
            return Util.ToJson(this);
        }
    }
}