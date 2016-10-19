
namespace Loogn.WeiXinSDK.Message
{
    /// <summary>
    /// 用户关注事件
    /// </summary>
    public class EventAttendMsg : EventBaseMsg
    {
        public override string Event
        {
            get { return "subscribe"; }
        }
    }
}
