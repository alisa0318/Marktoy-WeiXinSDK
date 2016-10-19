using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Loogn.WeiXinSDK.Commom
{
    public static class Log
    {
        public static void WriteLog(string content)
        {
            string path = @"F:\webchart\log.txt";
            if (!File.Exists(path))
            {
                FileInfo myfile = new FileInfo(path);
                FileStream fs = myfile.Create();
                fs.Close();
            }
            StreamWriter sw = File.AppendText(path);
            sw.WriteLine(content);
            sw.Flush();
            sw.Close();
        }
    }
}
