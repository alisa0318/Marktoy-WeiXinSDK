using System;
using System.Collections.Generic;
using System.Text;

namespace Loogn.WeiXinSDK
{
    [Serializable]
    public class JSApiTicket
    {
        public int errcode { get; set; }
        public string errmsg { get; set; }

        public string ticket { get; set; }

        /// <summary>
        /// 过期秒数
        /// </summary>
        public int expires_in { get; set; }
    }
}
