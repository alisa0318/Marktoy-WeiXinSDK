using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace Loogn.WeiXinSDK
{
    public class Tuple<T1, T2, T3>
    {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }
        public Tuple(T1 t1, T2 t2, T3 t3)
        {
            this.Item1 = t1;
            this.Item2 = t2;
            this.Item3 = t3;
        }
    }

    public class Util
    {
        public static string SHA1(string str)
        {
            return BitConverter.ToString(new SHA1CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(str))).Replace("-", "");
        }

        #region Http
        public static Stream HttpPost(string action, byte[] data)
        {
            HttpWebRequest myRequest;
            myRequest = WebRequest.Create(action) as HttpWebRequest;
            myRequest.Method = "POST";
            myRequest.Timeout = 20 * 1000;
            myRequest.ContentType = "application/x-www-form-urlencoded";
            myRequest.ContentLength = data.Length;
            using (Stream newStream = myRequest.GetRequestStream())
            {
                newStream.Write(data, 0, data.Length);
            }
            HttpWebResponse myResponse = myRequest.GetResponse() as HttpWebResponse;
            return myResponse.GetResponseStream();
        }
        public static string HttpPost2(string action, string data)
        {
            var buffer = Encoding.UTF8.GetBytes(data);
            using (var stream = Util.HttpPost(action, buffer))
            {
                StreamReader sr = new StreamReader(stream);
                data = sr.ReadToEnd();
                return data;
            }
        }

        public static string HttpPost2(string action, byte[] data)
        {
            using (var stream = Util.HttpPost(action, data))
            {
                StreamReader sr = new StreamReader(stream);
                return sr.ReadToEnd();
            }
        }


        public static string HttpUpload(string action, string file)
        {
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            HttpWebRequest myRequest = WebRequest.Create(action) as HttpWebRequest;
            myRequest.Method = "POST";
            myRequest.ContentType = "multipart/form-data;boundary=" + boundary;
            StringBuilder sb = new StringBuilder();
            sb.Append("--" + boundary);
            sb.Append("\r\n");
            sb.Append("Content-Disposition: form-data; name=\"media\"; filename=\"" + file + "\"");
            sb.Append("\r\n");
            sb.Append("Content-Type: application/octet-stream");
            sb.Append("\r\n\r\n");
            string head = sb.ToString();
            long length = 0;
            byte[] form_data = Encoding.UTF8.GetBytes(head);
            byte[] foot_data = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
            length = form_data.Length + foot_data.Length;

            using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                length += fileStream.Length;
                myRequest.ContentLength = length;
                Stream requestStream = myRequest.GetRequestStream();
                requestStream.Write(form_data, 0, form_data.Length);

                byte[] buffer = new Byte[checked((uint)Math.Min(4096, (int)fileStream.Length))];
                int bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                    requestStream.Write(buffer, 0, bytesRead);
                requestStream.Write(foot_data, 0, foot_data.Length);
            }
            HttpWebResponse myResponse = myRequest.GetResponse() as HttpWebResponse;
            StreamReader sr = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
            string json = sr.ReadToEnd().Trim();
            sr.Close();
            if (myResponse != null)
            {
                myResponse.Close();
                myRequest = null;
            }
            if (myRequest != null)
            {
                myRequest = null;
            }
            return json;
        }

        public static Tuple<Stream, string, string> HttpGet(string action)
        {
            HttpWebRequest myRequest = WebRequest.Create(action) as HttpWebRequest;
            myRequest.Method = "GET";
            myRequest.Timeout = 20 * 1000;
            HttpWebResponse myResponse = myRequest.GetResponse() as HttpWebResponse;
            var stream = myResponse.GetResponseStream();
            var ct = myResponse.ContentType;
            if (ct.IndexOf("json") >= 0 || ct.IndexOf("text") >= 0)
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    var json = sr.ReadToEnd();
                    return new Tuple<Stream, string, string>(null, ct, json);
                }
            }
            else
            {
                Stream MyStream = new MemoryStream();
                byte[] buffer = new Byte[4096];
                int bytesRead = 0;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                    MyStream.Write(buffer, 0, bytesRead);
                MyStream.Position = 0;
                return new Tuple<Stream, string, string>(MyStream, ct, string.Empty);
            }
        }

        public static string HttpGet2(string action)
        {
            return Util.HttpGet(action).Item3;
        }
        #endregion

        #region json

        public static string ToJson(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static T JsonTo<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        public static Dictionary<string, string> GetDictFromXml(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                dict.Add(node.Name, node.InnerText.Trim());
            }
            return dict;
        }

        #endregion


        #region datetime
        /// <summary>
        /// 日期转换成unix时间戳
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static long DateTimeToUnixTimestamp(DateTime dateTime)
        {
            dateTime = dateTime.ToUniversalTime();
            var start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((dateTime - start).TotalSeconds);
        }

        /// <summary>
        /// unix时间戳转换成日期
        /// </summary>
        /// <param name="timestamp">时间戳（秒）</param>
        /// <returns></returns>
        public static DateTime UnixTimestampToDateTime(long timestamp)
        {
            var start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return start.AddSeconds(timestamp).ToLocalTime();
        }

        public static readonly DateTime SqlMinDateTime = new DateTime(1753, 1, 1);
        public static readonly DateTime SqlMaxDateTime = new DateTime(9999, 12, 31, 23, 59, 59);
        #endregion

        public static string SHA1_Hash(string str_sha1_in)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] bytes_sha1_in = Encoding.Default.GetBytes(str_sha1_in);
            byte[] bytes_sha1_out = sha1.ComputeHash(bytes_sha1_in);
            string str_sha1_out = BitConverter.ToString(bytes_sha1_out);
            str_sha1_out = str_sha1_out.Replace("-", "");
            return str_sha1_out;
        }
    }
}
