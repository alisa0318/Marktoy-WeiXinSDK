using System;
using System.Collections.Generic;

namespace Loogn.WeiXinSDK
{
    /// <summary>
    /// 凭据
    /// </summary>
    [Serializable]
    public class ClientCredential
    {
        public string access_token { get; set; }
        /// <summary>
        /// 过期秒数
        /// </summary>
        public int expires_in { get; set; }

        public ReturnCode error { get; set; }
        
    }
}
