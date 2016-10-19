using System;
using System.Collections.Generic;
using System.IO;
using Loogn.WeiXinSDK.Menu;
using Loogn.WeiXinSDK.Message;
using Loogn.WeiXinSDK.Mass;
using System.Web;
using System.Text;

namespace Loogn.WeiXinSDK
{
    /// <summary>
    /// 消息事件处理委托
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="t"></param>
    /// <returns></returns>
    public delegate TResult MyFunc<T1, TResult>(T1 t);

    /// <summary>
    /// 微信接口API
    /// </summary>
    public class WeiXin
    {
        //接收到消息时的委托
        public delegate void ReceiveMsg(string msgXmlStr, Dictionary<string, string> msgDict);
        //定义一个委托类型的事件，当接收到消息时抛出此事件
        public static event ReceiveMsg OnReceiveMsgEvent;


        static object lockObj = new object();


        /// <summary>
        /// 验证 Signature
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static bool CheckSignature(string token)
        {
            var request = HttpContext.Current.Request;
            var response = HttpContext.Current.Response;

            var signature = request["signature"];
            var timestamp = request["timestamp"];
            var nonce = request["nonce"];

            return CheckSignature(signature, timestamp, nonce, token);
        }

        /// <summary>
        /// 检验signature
        /// </summary>
        /// <param name="signature">微信加密签名</param>
        /// <param name="timestamp">时间戳</param>
        /// <param name="nonce">随机数</param>
        /// <param name="token">由AppId和AppSecret得到的凭据</param>
        /// <returns></returns>
        public static bool CheckSignature(string signature, string timestamp, string nonce, string token)
        {
            if (string.IsNullOrEmpty(signature)) return false;
            List<string> tmpList = new List<string>(3);
            tmpList.Add(token);
            tmpList.Add(timestamp);
            tmpList.Add(nonce);
            tmpList.Sort();
            var tmpStr = string.Join("", tmpList.ToArray());
            string strResult = Util.SHA1(tmpStr);
            return signature.Equals(strResult, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// 验证服务器地址
        /// </summary>
        /// <param name="token"></param>
        public static void ValidServerURL(string token)
        {
            if (CheckSignature(token))
            {
                HttpContext.Current.Response.Clear();
                HttpContext.Current.Response.ContentType = "text/plain";
                HttpContext.Current.Response.Write(HttpContext.Current.Request["echostr"]);
                HttpContext.Current.Response.End();
            }
        }

        /// <summary>
        /// 获取access_token
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="appSecret"></param>
        /// <returns></returns>
        public static ClientCredential GetAccessToken(string appId, string appSecret)
        {
            var url = "https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}";
            var json = Util.HttpGet2(string.Format(url, appId, appSecret));
            ClientCredential result = null;
            if (json.IndexOf("errcode") > 0)
            {
                result = new ClientCredential();
                result.error = Util.JsonTo<ReturnCode>(json);
            }
            else
            {
                result = Util.JsonTo<ClientCredential>(json);
            }
            return result;
        }

        /// <summary>
        /// jsapi_ticket是公众号用于调用微信JS接口的临时票据
        /// </summary>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static JSApiTicket GetJSApiTicket(string access_token)
        {
            var url = "https://api.weixin.qq.com/cgi-bin/ticket/getticket?access_token={0}&type=jsapi";
            var json = Util.HttpGet2(string.Format(url, access_token));
            var ticket = Util.JsonTo<JSApiTicket>(json);
            return ticket;
        }

        /// <summary>
        /// 得到js签名的一些信息
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="jsapi_ticket"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static JSSignature GetJSSignature(string appId, string jsapi_ticket, string url)
        {
            JSSignature info = new JSSignature();
            info.appId = appId;
            info.nonceStr = Guid.NewGuid().ToString();
            info.timestamp = Util.DateTimeToUnixTimestamp(DateTime.Now);
            SortedDictionary<string, string> sortedDict = new SortedDictionary<string, string>();
            sortedDict.Add("jsapi_ticket", jsapi_ticket);
            sortedDict.Add("noncestr", info.nonceStr);
            sortedDict.Add("timestamp", info.timestamp.ToString());
            sortedDict.Add("url", url);
            StringBuilder sb = new StringBuilder(300);

            foreach (var kv in sortedDict)
            {
                sb.AppendFormat("{0}={1}&", kv.Key, kv.Value);
            }
            sb.Remove(sb.Length - 1, 1);
            info.signature = Util.SHA1_Hash(sb.ToString()).ToLower();
            return info;
        }
        
        /// <summary>
        /// 获取微信服务器IP地址
        /// </summary>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public CallbackIP GetCallbackIP(string access_token)
        {
            var url = "https://api.weixin.qq.com/cgi-bin/getcallbackip?access_token={0}";
            var json = Util.HttpGet2(string.Format(url, access_token));
            CallbackIP result = null;
            if (json.IndexOf("errcode") > 0)
            {
                result = new CallbackIP();
                result.error = Util.JsonTo<ReturnCode>(json);
            }
            else
            {
                result = Util.JsonTo<CallbackIP>(json);
            }
            return result;
        }



        #region 消息

        /// <summary>
        /// 处理用户消息和事件
        /// </summary>
        /// <returns></returns>
        public static ReplyBaseMsg ReplyMsg()
        {
            Stream inputStream = System.Web.HttpContext.Current.Request.InputStream;
            long pos = inputStream.Position;
            inputStream.Position = 0;
            byte[] buffer = new byte[inputStream.Length];
            inputStream.Read(buffer, 0, buffer.Length);
            inputStream.Position = pos;
            string xml = System.Text.Encoding.UTF8.GetString(buffer);
            var dict = Util.GetDictFromXml(xml);
            //产生事件
            if (OnReceiveMsgEvent != null)
            {
                OnReceiveMsgEvent(xml, dict);
            }
            string key = string.Empty;
            ReplyBaseMsg replyMsg = ReplyEmptyMsg.Instance;
            if (dict.ContainsKey("Event"))
            {
                #region 接收事件消息
                var evt = dict["Event"].ToLower();
                key = "event_";
                switch (evt)
                {
                    case "click":
                        {
                            var msg = new EventClickMsg { CreateTime = Int64.Parse(dict["CreateTime"]), FromUserName = dict["FromUserName"], ToUserName = dict["ToUserName"], MyEventType = MyEventType.Click, EventKey = dict["EventKey"] };
                            replyMsg = GetReply<EventClickMsg>(key + MyEventType.Click.ToString(), msg);
                            break;
                        }
                    case "view":
                        {
                            var msg = new EventViewMsg { CreateTime = Int64.Parse(dict["CreateTime"]), FromUserName = dict["FromUserName"], ToUserName = dict["ToUserName"], MyEventType = MyEventType.View, EventKey = dict["EventKey"] };
                            replyMsg = GetReply<EventViewMsg>(key + MyEventType.View.ToString(), msg);
                            break;
                        }
                    case "location":
                        {
                            var msg = new EventLocationMsg { CreateTime = Int64.Parse(dict["CreateTime"]), FromUserName = dict["FromUserName"], ToUserName = dict["ToUserName"], MyEventType = MyEventType.Location, Latitude = double.Parse(dict["Latitude"]), Longitude = double.Parse(dict["Longitude"]), Precision = double.Parse(dict["Precision"]) };
                            replyMsg = GetReply<EventLocationMsg>(key + MyEventType.Location.ToString(), msg);
                            break;
                        }
                    case "scan":
                        {
                            var msg = new EventFansScanMsg { CreateTime = Int64.Parse(dict["CreateTime"]), FromUserName = dict["FromUserName"], ToUserName = dict["ToUserName"], MyEventType = MyEventType.FansScan, EventKey = dict["EventKey"], Ticket = dict["Ticket"] };
                            replyMsg = GetReply<EventFansScanMsg>(key + MyEventType.FansScan.ToString(), msg);
                            break;
                        }
                    case "unsubscribe":
                        {
                            var msg = new EventUnattendMsg { CreateTime = Int64.Parse(dict["CreateTime"]), FromUserName = dict["FromUserName"], ToUserName = dict["ToUserName"], MyEventType = MyEventType.Unattend };
                            replyMsg = GetReply<EventUnattendMsg>(key + MyEventType.Unattend.ToString(), msg);
                            break;
                        }
                    case "subscribe":
                        {
                            if (dict.ContainsKey("Ticket"))
                            {
                                var msg = new EventUserScanMsg { CreateTime = Int64.Parse(dict["CreateTime"]), FromUserName = dict["FromUserName"], ToUserName = dict["ToUserName"], MyEventType = MyEventType.UserScan, Ticket = dict["Ticket"], EventKey = dict["EventKey"] };
                                replyMsg = GetReply<EventUserScanMsg>(key + MyEventType.UserScan.ToString(), msg);
                            }
                            else
                            {
                                var msg = new EventAttendMsg { CreateTime = Int64.Parse(dict["CreateTime"]), FromUserName = dict["FromUserName"], ToUserName = dict["ToUserName"], MyEventType = MyEventType.Attend };
                                replyMsg = GetReply<EventAttendMsg>(key + MyEventType.Attend.ToString(), msg);
                            }
                            break;
                        }
                    case "masssendjobfinish":
                        {
                            var msg = new EventMassSendJobFinishMsg
                            {
                                CreateTime = Int64.Parse(dict["CreateTime"]),
                                FromUserName = dict["FromUserName"],
                                ToUserName = dict["ToUserName"],
                                MyEventType = MyEventType.MASSSENDJOBFINISH,
                                ErrorCount = int.Parse(dict["ErrorCount"]),
                                FilterCount = int.Parse(dict["FilterCount"]),
                                MsgID = int.Parse(dict["MsgID"]),
                                SentCount = int.Parse(dict["SentCount"]),
                                TotalCount = int.Parse(dict["TotalCount"]),
                                Status = dict["Status"]
                            };

                            replyMsg = GetReply<EventMassSendJobFinishMsg>(key + MyEventType.MASSSENDJOBFINISH.ToString(), msg);
                            break;
                        }
                    case "merchant_order":
                        {
                            var msg = new EventMerchantOrderMsg
                            {
                                CreateTime = Int64.Parse(dict["CreateTime"]),
                                FromUserName = dict["FromUserName"],
                                ToUserName = dict["ToUserName"],
                                MyEventType = MyEventType.MerchantOrder,
                                OrderID = dict["OrderID"],
                                OrderStatus = int.Parse(dict["OrderStatus"]),
                                ProductId = dict["ProductId"],
                                SkuInfo = dict["SkuInfo"]
                            };
                            replyMsg = GetReply<EventMerchantOrderMsg>(key + MyEventType.MerchantOrder.ToString(), msg);
                            break;
                        }
                }
                #endregion
            }
            else if (dict.ContainsKey("MsgId"))
            {
                #region 接收普通消息
                var msgType = dict["MsgType"];
                key = msgType;
                switch (msgType)
                {
                    case "text":
                        {
                            var msg = new RecTextMsg { CreateTime = Int64.Parse(dict["CreateTime"]), FromUserName = dict["FromUserName"], ToUserName = dict["ToUserName"], MsgId = long.Parse(dict["MsgId"]), Content = dict["Content"] };
                            replyMsg = GetReply<RecTextMsg>(key, msg);
                            break;
                        }
                    case "image":
                        {

                            var msg = new RecImageMsg { CreateTime = Int64.Parse(dict["CreateTime"]), FromUserName = dict["FromUserName"], ToUserName = dict["ToUserName"], MsgId = long.Parse(dict["MsgId"]), PicUrl = dict["PicUrl"], MediaId = dict["MediaId"] };
                            replyMsg = GetReply<RecImageMsg>(key, msg);
                            break;
                        }
                    case "voice":
                        {
                            string recognition;
                            dict.TryGetValue("Recognition", out recognition);
                            var msg = new RecVoiceMsg { CreateTime = Int64.Parse(dict["CreateTime"]), FromUserName = dict["FromUserName"], ToUserName = dict["ToUserName"], MsgId = long.Parse(dict["MsgId"]), Format = dict["Format"], MediaId = dict["MediaId"], Recognition = recognition };
                            replyMsg = GetReply<RecVoiceMsg>(key, msg);
                            break;
                        }
                    case "video":
                        {
                            var msg = new RecVideoMsg { CreateTime = Int64.Parse(dict["CreateTime"]), FromUserName = dict["FromUserName"], ToUserName = dict["ToUserName"], MsgId = long.Parse(dict["MsgId"]), ThumbMediaId = dict["ThumbMediaId"], MediaId = dict["MediaId"] };
                            replyMsg = GetReply<RecVideoMsg>(key, msg);
                            break;
                        }
                    case "location":
                        {
                            var msg = new RecLocationMsg { CreateTime = Int64.Parse(dict["CreateTime"]), FromUserName = dict["FromUserName"], ToUserName = dict["ToUserName"], MsgId = long.Parse(dict["MsgId"]), Label = dict["Label"], Location_X = double.Parse(dict["Location_X"]), Location_Y = double.Parse(dict["Location_Y"]), Scale = int.Parse(dict["Scale"]) };
                            replyMsg = GetReply<RecLocationMsg>(key, msg);
                            break;
                        }
                    case "link":
                        {
                            var msg = new RecLinkMsg { CreateTime = Int64.Parse(dict["CreateTime"]), FromUserName = dict["FromUserName"], ToUserName = dict["ToUserName"], MsgId = long.Parse(dict["MsgId"]), Description = dict["Description"], Title = dict["Title"], Url = dict["Url"] };
                            replyMsg = GetReply<RecLinkMsg>(key, msg);
                            break;
                        }
                }
                #endregion
            }
            return replyMsg;
        }

        static Dictionary<string, object> m_msgHandlers = new Dictionary<string, object>();

        /// <summary>
        /// 注册消息处理程序
        /// </summary>
        /// <typeparam name="TMsg"></typeparam>
        /// <param name="handler"></param>
        public static void RegisterMsgHandler<TMsg>(MyFunc<TMsg, ReplyBaseMsg> handler) where TMsg : RecBaseMsg
        {
            var type = typeof(TMsg);
            var key = string.Empty;
            if (type == typeof(RecTextMsg))
            {
                key = "text";
            }
            else if (type == typeof(RecImageMsg))
            {
                key = "image";
            }
            else if (type == typeof(RecLinkMsg))
            {
                key = "link";
            }
            else if (type == typeof(RecLocationMsg))
            {
                key = "location";
            }
            else if (type == typeof(RecVideoMsg))
            {
                key = "video";
            }
            else if (type == typeof(RecVoiceMsg))
            {
                key = "voice";
            }
            else
            {
                return;
            }
            m_msgHandlers[key.ToLower()] = handler;
        }
        /// <summary>
        /// 注册事件处理程序
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="handler"></param>
        public static void RegisterEventHandler<TEvent>(MyFunc<TEvent, ReplyBaseMsg> handler) where TEvent : EventBaseMsg
        {
            var type = typeof(TEvent);
            var key = "event_";
            if (type == typeof(EventClickMsg))
            {
                key += MyEventType.Click.ToString();
            }
            else if (type == typeof(EventFansScanMsg))
            {
                key += MyEventType.FansScan.ToString();
            }
            else if (type == typeof(EventAttendMsg))
            {
                key += MyEventType.Attend.ToString();
            }
            else if (type == typeof(EventLocationMsg))
            {
                key += MyEventType.Location.ToString();
            }
            else if (type == typeof(EventUnattendMsg))
            {
                key += MyEventType.Unattend.ToString();
            }
            else if (type == typeof(EventUserScanMsg))
            {
                key += MyEventType.UserScan.ToString();
            }
            else if (type == typeof(EventMassSendJobFinishMsg))
            {
                key += MyEventType.MASSSENDJOBFINISH.ToString();
            }
            else if (type == typeof(EventViewMsg))
            {
                key += MyEventType.View.ToString();
            }
            else if (type == typeof(EventMerchantOrderMsg))
            {
                key += MyEventType.MerchantOrder.ToString();
            }
            else
            {
                return;
            }
            m_msgHandlers[key.ToLower()] = handler;
        }

        static ReplyBaseMsg GetReply<TMsg>(string key, TMsg msg) where TMsg : RecEventBaseMsg
        {
            key = key.ToLower();
            if (m_msgHandlers.ContainsKey(key))
            {
                var handler = m_msgHandlers[key] as MyFunc<TMsg, ReplyBaseMsg>;
                var replyMsg = handler(msg);
                if (replyMsg.CreateTime == 0) replyMsg.CreateTime = DateTime.Now.Ticks;
                if (string.IsNullOrEmpty(replyMsg.FromUserName)) replyMsg.FromUserName = msg.ToUserName;
                if (string.IsNullOrEmpty(replyMsg.ToUserName)) replyMsg.ToUserName = msg.FromUserName;
                return replyMsg;
            }
            else
            {
                return ReplyEmptyMsg.Instance;
            }
        }

        /// <summary>
        /// 主动给用户发消息（用户）
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="access_token">access_token</param>
        /// <returns>errcode=0为成功</returns>
        public static ReturnCode SendMsg(SendBaseMsg msg, string access_token)
        {
            string url = "https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token=" + access_token;
            var json = msg.GetJSON();
            var retJson = Util.HttpPost2(url, json);
            return Util.JsonTo<ReturnCode>(retJson);
        }


        #endregion

        #region 群发
        /// <summary>
        /// 根据分组进行群发
        /// </summary>
        /// <param name="mess"></param>
        /// <param name="access_token">access_token</param>
        /// <returns></returns>
        public static SendReturnCode SendMessByGroup(FilterMess mess, string access_token)
        {
            var url = "https://api.weixin.qq.com/cgi-bin/message/mass/sendall?access_token=" + access_token;
            var json = Util.ToJson(mess);
            var retJson = Util.HttpPost2(url, json);
            return Util.JsonTo<SendReturnCode>(retJson);
        }



        /// <summary>
        /// 根据OpenID列表群发
        /// </summary>
        /// <param name="mess"></param>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static SendReturnCode SendMessByUsers(ToUserMess mess, string access_token)
        {
            var url = "https://api.weixin.qq.com/cgi-bin/message/mass/send?access_token=" + access_token;

            var json = Util.ToJson(mess);
            var retJson = Util.HttpPost2(url, json);
            return Util.JsonTo<SendReturnCode>(retJson);
        }

        /// <summary>
        /// 删除群发.
        /// 请注意，只有已经发送成功的消息才能删除删除消息只是将消息的图文详情页失效，已经收到的用户，还是能在其本地看到消息卡片。 另外，删除群发消息只能删除图文消息和视频消息，其他类型的消息一经发送，无法删除。
        /// </summary>
        /// <param name="msgid"></param>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static ReturnCode DeleteMess(int msgid, string access_token)
        {
            var url = "https://api.weixin.qq.com//cgi-bin/message/mass/delete?access_token=" + access_token;

            var json = "{\"msgid\":" + msgid.ToString() + "}";
            var retJson = Util.HttpPost2(url, json);
            return Util.JsonTo<ReturnCode>(retJson);
        }


        #endregion

        #region 自定义菜单

        /// <summary>
        /// 创建自定义菜单
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static ReturnCode CreateMenu(CustomMenu menu, string access_token)
        {
            var json = menu.GetJSON();
            return CreateMenu(json, access_token);
        }

        /// <summary>
        /// 创建自定义菜单
        /// </summary>
        /// <param name="menuJSON"></param>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static ReturnCode CreateMenu(string menuJSON, string access_token)
        {
            string url = "https://api.weixin.qq.com/cgi-bin/menu/create?access_token=" + access_token;

            var retJson = Util.HttpPost2(url, menuJSON);
            return Util.JsonTo<ReturnCode>(retJson);
        }


        /// <summary>
        /// 直接返回自定义菜单json字符串，
        /// </summary>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static string GetMenu(string access_token)
        {
            string url = "https://api.weixin.qq.com/cgi-bin/menu/get?access_token=" + access_token;

            var json = Util.HttpGet2(url);
            return json;
        }


        /// <summary>
        /// 删除自定义菜单
        /// </summary>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static ReturnCode DeleteMenu(string access_token)
        {
            string url = "https://api.weixin.qq.com/cgi-bin/menu/delete?access_token=" + access_token;

            var json = Util.HttpGet2(url);
            return Util.JsonTo<ReturnCode>(json);
        }


        #endregion

        #region 二维码

        /// <summary>
        /// 创建二维码ticket
        /// </summary>
        /// <param name="isTemp"></param>
        /// <param name="scene_id"></param>
        ///<param name="access_token"></param>
        /// <returns></returns>
        public static QRCodeTicket CreateQRCode(bool isTemp, int scene_id, string access_token)
        {
            string url = "https://api.weixin.qq.com/cgi-bin/qrcode/create?access_token=" + access_token;

            var action_name = isTemp ? "QR_SCENE" : "QR_LIMIT_SCENE";
            string data;
            if (isTemp)
            {
                data = "{\"expire_seconds\": 1800, \"action_name\": \"QR_SCENE\", \"action_info\": {\"scene\": {\"scene_id\":" + scene_id + "}}}";
            }
            else
            {
                data = "{\"action_name\": \"QR_LIMIT_SCENE\", \"action_info\": {\"scene\": {\"scene_id\": " + scene_id + "}}}";
            }

            var json = Util.HttpPost2(url, data);
            if (json.IndexOf("ticket") > 0)
            {
                return Util.JsonTo<QRCodeTicket>(json);
            }
            else
            {
                QRCodeTicket tk = new QRCodeTicket();
                tk.error = Util.JsonTo<ReturnCode>(json);
                return tk;
            }
        }


        /// <summary>
        /// 得到QR图片地址
        /// </summary>
        /// <param name="qrcodeTicket"></param>
        /// <returns></returns>
        public static string GetQRUrl(string qrcodeTicket)
        {
            return "https://mp.weixin.qq.com/cgi-bin/showqrcode?ticket=" + System.Web.HttpUtility.HtmlEncode(qrcodeTicket);
        }

        #endregion

        #region 获取关注者列表

        /// <summary>
        /// 获取关注者列表
        /// </summary>
        /// <param name="next_openid"></param>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static Followers GetFollowers(string next_openid, string access_token)
        {
            string url = "https://api.weixin.qq.com/cgi-bin/user/get?access_token=" + access_token;

            if (!string.IsNullOrEmpty(next_openid))
            {
                url = url + "&next_openid=" + next_openid;
            }
            var json = Util.HttpGet2(url);
            if (json.IndexOf("errcode") > 0)
            {
                var fs = new Followers();
                fs.error = Util.JsonTo<ReturnCode>(json);
                return fs;
            }
            else
            {
                return Util.JsonTo<Followers>(json);
            }
        }


        /// <summary>
        /// 获取所有关注者列表
        /// </summary>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static Followers GetAllFollowers(string access_token)
        {
            Followers allFollower = new Followers();
            allFollower.data = new Followers.Curdata();
            allFollower.data.openid = new List<string>();

            string next_openid = string.Empty;
            do
            {
                var f = GetFollowers(next_openid, access_token);
                if (f.error != null)
                {
                    allFollower.error = f.error;
                    break;
                }
                else
                {
                    //由 吹動雲兒的風 修改
                    if (f.count > 0)
                    {
                        foreach (var opid in f.data.openid)
                        {
                            allFollower.data.openid.Add(opid);
                        }
                        //假如已经到最后直接退出循环
                        if (f.next_openid == allFollower.data.openid[f.count - 1])
                        {
                            allFollower.total = f.total;
                            break;
                        }
                        else
                        {
                            allFollower.total += f.count;
                        }
                    }
                    next_openid = f.next_openid;
                }
            } while (!string.IsNullOrEmpty(next_openid));

            allFollower.count = allFollower.total;
            return allFollower;
        }


        #endregion

        #region 用户信息
        /// <summary>
        /// 得到用户基本信息
        /// </summary>
        /// <param name="openid"></param>
        /// <param name="lang"></param>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static UserInfo GetUserInfo(string openid, LangType lang, string access_token)
        {
            string url = "https://api.weixin.qq.com/cgi-bin/user/info?access_token=";

            url = url + access_token + "&openid=" + openid + "&lang=" + lang.ToString();

            var json = Util.HttpGet2(url);

            if (json.IndexOf("errcode") > 0)
            {
                var ui = new UserInfo();
                ui.openid = openid;
                ui.error = Util.JsonTo<ReturnCode>(json);
                return ui;
            }
            else
            {
                return Util.JsonTo<UserInfo>(json);
            }
        }

        #endregion

        #region 分组

        /// <summary>
        /// 创建分组
        /// </summary>
        /// <param name="name">分组名字（30个字符以内）</param>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static GroupInfo CreateGroup(string name, string access_token)
        {
            string url = "https://api.weixin.qq.com/cgi-bin/groups/create?access_token=" + access_token;

            var post = "{\"group\":{\"name\":\"" + name + "\"}}";
            var json = Util.HttpPost2(url, post);
            if (json.IndexOf("errcode") > 0)
            {
                var gi = new GroupInfo();
                gi.error = Util.JsonTo<ReturnCode>(json);
                return gi;
            }
            else
            {
                var dict = Util.JsonTo<Dictionary<string, Dictionary<string, object>>>(json);
                var gi = new GroupInfo();
                var gpdict = dict["group"];
                gi.id = Convert.ToInt32(gpdict["id"]);
                gi.name = gpdict["name"].ToString();
                return gi;
            }
        }


        /// <summary>
        /// 查询所有分组
        /// </summary>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static Groups GetGroups(string access_token)
        {
            string url = "https://api.weixin.qq.com/cgi-bin/groups/get?access_token=" + access_token;

            string json = Util.HttpGet2(url);
            if (json.IndexOf("errcode") > 0)
            {
                var gs = new Groups();
                gs.error = Util.JsonTo<ReturnCode>(json);
                return gs;
            }
            else
            {
                var dict = Util.JsonTo<Dictionary<string, List<Dictionary<string, object>>>>(json);
                var gs = new Groups();
                var gilist = dict["groups"];
                foreach (var gidict in gilist)
                {
                    var gi = new GroupInfo();
                    gi.name = gidict["name"].ToString();
                    gi.id = Convert.ToInt32(gidict["id"]);
                    gi.count = Convert.ToInt32(gidict["count"]);
                    gs.Add(gi);
                }
                return gs;
            }
        }


        /// <summary>
        /// 查询用户所在分组
        /// </summary>
        /// <param name="openid"></param>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static GroupID GetUserGroup(string openid, string access_token)
        {
            string url = "https://api.weixin.qq.com/cgi-bin/groups/getid?access_token=" + access_token;

            var post = "{\"openid\":\"" + openid + "\"}";
            var json = Util.HttpPost2(url, post);
            if (json.IndexOf("errcode") > 0)
            {
                var gid = new GroupID();
                gid.error = Util.JsonTo<ReturnCode>(json);
                return gid;
            }
            else
            {
                var dict = Util.JsonTo<Dictionary<string, int>>(json);
                var gid = new GroupID();
                gid.id = dict["groupid"];
                return gid;
            }
        }


        /// <summary>
        /// 修改分组名
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static ReturnCode UpdateGroup(int id, string name, string access_token)
        {
            string url = "https://api.weixin.qq.com/cgi-bin/groups/update?access_token=" + access_token;

            var post = "{\"group\":{\"id\":" + id + ",\"name\":\"" + name + "\"}}";
            var json = Util.HttpPost2(url, post);
            return Util.JsonTo<ReturnCode>(json);
        }

        /// <summary>
        /// 移动用户分组
        /// </summary>
        /// <param name="openid"></param>
        /// <param name="groupid"></param>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static ReturnCode MoveGroup(string openid, int groupid, string access_token)
        {
            string url = "https://api.weixin.qq.com/cgi-bin/groups/members/update?access_token=" + access_token;

            var post = "{\"openid\":\"" + openid + "\",\"to_groupid\":" + groupid + "}";
            var json = Util.HttpPost2(url, post);
            return Util.JsonTo<ReturnCode>(json);
        }


        #endregion

        #region 多媒体文件
        /// <summary>
        /// 上传多媒体文件
        /// </summary>
        /// <param name="file"></param>
        /// <param name="type"> 媒体文件类型,image,voice,video,thumb,news</param>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static MediaInfo UploadMedia(string file, string type, string access_token)
        {
            string url = "http://file.api.weixin.qq.com/cgi-bin/media/upload?access_token=";

            url = url + access_token + "&type=" + type.ToString();
            var json = Util.HttpUpload(url, file);
            if (json.IndexOf("errcode") > 0)
            {
                var mi = new MediaInfo();
                mi.error = Util.JsonTo<ReturnCode>(json);
                return mi;
            }
            else
            {
                return Util.JsonTo<MediaInfo>(json);
            }
        }


        public static MediaInfo UploadVideoForMess(UploadVideoInfo videoInfo, string access_token)
        {
            var url = "https://file.api.weixin.qq.com/cgi-bin/media/uploadvideo?access_token=";
            url = url + access_token;
            var json = Util.HttpPost2(url, Util.ToJson(videoInfo));
            if (json.IndexOf("errcode") > 0)
            {
                var mi = new MediaInfo();
                mi.error = Util.JsonTo<ReturnCode>(json);
                return mi;
            }
            else
            {
                return Util.JsonTo<MediaInfo>(json);
            }
        }

        /// <summary>
        ///  上传图文消息素材,用于群发
        /// </summary>
        /// <param name="news"></param>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static MediaInfo UploadNews(News news, string access_token)
        {
            string url = "https://api.weixin.qq.com/cgi-bin/media/uploadnews?access_token=";

            url = url + access_token;
            var json = Util.HttpPost2(url, Util.ToJson(news));
            if (json.IndexOf("errcode") > 0)
            {
                var mi = new MediaInfo();
                mi.error = Util.JsonTo<ReturnCode>(json);
                return mi;
            }
            else
            {
                return Util.JsonTo<MediaInfo>(json);
            }
        }


        /// <summary>
        /// 下载多媒体文件
        /// </summary>
        /// <param name="media_id"></param>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static DownloadFile DownloadMedia(string media_id, string access_token)
        {
            string url = "http://file.api.weixin.qq.com/cgi-bin/media/get?access_token=";
            url = url + access_token + "&media_id=" + media_id;
            var tup = Util.HttpGet(url);
            var dm = new DownloadFile();
            dm.ContentType = tup.Item2;

            if (tup.Item1 == null)
            {
                dm.error = Util.JsonTo<ReturnCode>(tup.Item3);
            }
            else
            {
                dm.Stream = tup.Item1;
            }
            return dm;
        }


        #endregion

        #region 网页授权获取用户基本信息
        /// <summary>
        /// 得到获取code的Url
        /// </summary>
        /// <param name="appid">公众号的唯一标识</param>
        /// <param name="redirect">授权后重定向的回调链接地址，请使用urlencode对链接进行处理</param>
        /// <param name="scope">应用授权作用域，snsapi_base （不弹出授权页面，直接跳转，只能获取用户openid），snsapi_userinfo （弹出授权页面，可通过openid拿到昵称、性别、所在地。并且，即使在未关注的情况下，只要用户授权，也能获取其信息）</param>
        /// <param name="state">重定向后会带上state参数，开发者可以填写a-zA-Z0-9的参数值</param>
        /// <returns></returns>
        public static string BuildWebCodeUrl(string appid, string redirect, string scope, string state = "")
        {

            return string.Format("https://open.weixin.qq.com/connect/oauth2/authorize?appid={0}&redirect_uri={1}&response_type=code&scope={2}&state={3}#wechat_redirect", appid, redirect, scope, state);
        }

        /// <summary>
        /// 通过code换取网页授权access_token
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="appSecret"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static WebCredential GetWebAccessToken(string appId, string appSecret, string code)
        {
            return WebCredential.GetCredential(appId, appSecret, code);
        }

        /// <summary>
        /// 刷新网页授权access_token
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="appSecret"></param>
        /// <param name="refresh_token"></param>
        /// <returns></returns>
        public static WebCredential RefreshWebAccessToken(string appId, string appSecret, string refresh_token)
        {
            return WebCredential.RefreshToken(appId, appSecret, refresh_token);
        }


        /// <summary>
        /// 得到网页授权用户信息
        /// </summary>
        /// <param name="web_access_token">网页授权接口调用凭证,注意：此access_token与基础支持的access_token不同</param>
        /// <param name="openid">用户的唯一标识</param>
        /// <param name="lang">返回国家地区语言版本，zh_CN 简体，zh_TW 繁体，en 英语</param>
        /// <returns></returns>
        public static WebUserInfo GetWebUserInfo(string web_access_token, string openid, LangType lang)
        {
            string url = string.Format("https://api.weixin.qq.com/sns/userinfo?access_token={0}&openid={1}&lang={2}", web_access_token, openid, lang.ToString());

            var json = Util.HttpGet2(url);

            if (json.IndexOf("errcode") > 0)
            {
                var ui = new WebUserInfo();
                ui.error = Util.JsonTo<ReturnCode>(json);
                return ui;
            }
            else
            {
                return Util.JsonTo<WebUserInfo>(json);
            }
        }
        #endregion

        #region 消息模板

        /// <summary>
        /// 主动给用户发送模板消息
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="access_token"></param>
        /// <returns>errcode=0为成功</returns>
        public static TmplReturnCode SendTmplMsg(SendTmplMsg msg, string access_token)
        {
            string url = "https://api.weixin.qq.com/cgi-bin/message/template/send?access_token=";

            url = url + access_token;
            var json = msg.GetJSON();

            var retJson = Util.HttpPost2(url, json);
            return Util.JsonTo<TmplReturnCode>(retJson);
        }


        #endregion

    }
}