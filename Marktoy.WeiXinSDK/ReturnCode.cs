
using System;
namespace Loogn.WeiXinSDK
{
    /// <summary>
    /// 全局返回码请看
    /// http://mp.weixin.qq.com/wiki/17/fa4e1434e57290788bde25603fa2fcbd.html
    /// </summary>
    [Serializable]
    public class ReturnCode
    {
        public int errcode { get; set; }
        public string errmsg { get; set; }
        public override string ToString()
        {
            return "{ \"errcode\":" + errcode + ",\"errmsg\":\"" + errmsg + "\"}";
        }
    }
}
