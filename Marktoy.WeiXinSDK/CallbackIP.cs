using System;
using System.Collections.Generic;
using System.Text;

namespace Loogn.WeiXinSDK
{
    [Serializable]
    public class CallbackIP
    {
        public string[] ip_list { get; set; }

        public ReturnCode error { get; set; }
    }
}
