namespace Loogn.WeiXinSDK.Message
{
    /// <summary>
    /// 在模版消息发送任务完成后，微信服务器会将是否送达成功作为通知
    /// </summary>
    public class EventTemplateMsg : EventBaseMsg
    {
        /// <summary>
        /// 事件类型
        /// </summary>
        public override string Event
        {
            get { return "TEMPLATESENDJOBFINISH"; }
        }
        /// <summary>
        /// 事件KEY值，qrscene_为前缀，后面为二维码的参数值scene_id
        /// </summary>
        public string MsgID { get; set; }
        /// <summary>
        /// 是否送达成功 
        /// success 成功 
        /// failed:user block 送达由于用户拒收（用户设置拒绝接收公众号消息）而失败时
        /// failed: system failed 送达由于其他原因失败时
        /// </summary>
        public string Status { get; set; }
    }
}
