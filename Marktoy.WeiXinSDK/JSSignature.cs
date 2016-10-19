using System;
using System.Collections.Generic;
using System.Text;

namespace Loogn.WeiXinSDK
{
    /// <summary>
    /// js签名信息
    /// </summary>
    public class JSSignature
    {
        /// <summary>
        /// 公众号的唯一标识
        /// </summary>
        public string appId { get; set; }
        /// <summary>
        /// 生成签名的时间戳
        /// </summary>
        public long timestamp { get; set; }
        /// <summary>
        /// 生成签名的随机串
        /// </summary>
        public string nonceStr { get; set; }
        /// <summary>
        /// 签名
        /// </summary>
        public string signature { get; set; }

    }
}
