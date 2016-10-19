using System;
using System.Collections.Generic;
using System.Text;

namespace Loogn.WeiXinSDK.Message
{
    /// <summary>
    /// 订单状态改变事件
    /// </summary>
    public class EventMerchantOrderMsg : EventBaseMsg
    {
        /// <summary>
        /// 事件名称
        /// </summary>
        public override string Event
        {
            get { return "merchant_order"; }
        }

        /// <summary>
        /// 订单编号
        /// </summary>
        public string OrderID { get; set; }
        /// <summary>
        /// 2待发货，3已发货，5已完成，8维权中
        /// </summary>
        public int OrderStatus { get; set; }
        public string ProductId { get; set; }
        /// <summary>
        /// 格式为："1001：10000012;1002:1000032"
        /// </summary>
        public string SkuInfo { get; set; }
    }
}
