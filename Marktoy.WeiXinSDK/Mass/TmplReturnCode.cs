
namespace Loogn.WeiXinSDK.Mass
{
    /// <summary>
    /// 模板消息发送结果 
    /// </summary>
    public class TmplReturnCode : ReturnCode
    {
        /// <summary>
        /// msgid":200228332
        /// </summary>
        public long msgid { get; set; }

        public override string ToString()
        {
            return "{ \"errcode\":" + errcode + ",\"errmsg\":\"" + errmsg + "\",\"msgid\":\"" + msgid + "\"}";
        }
    }
}
