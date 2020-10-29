using HttpHelperNamespace;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;

namespace TransComment
{
    public class Method
    {

        /// <summary>
        /// 调用百度API翻译
        /// </summary>
        /// <param name="words"></param>
        /// <returns></returns>
        public string Trans(string words)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            string sign = MD5Helper.MD5Lower32(ConfigurationManager.AppSettings["APPID"] + words + ConfigurationManager.AppSettings["SecretKey"]);

            dict.Add("q", words);
            dict.Add("from", "auto");
            dict.Add("to", "en");
            dict.Add("appid", ConfigurationManager.AppSettings["APPID"]);
            dict.Add("salt", ConfigurationManager.AppSettings["Salt"]);
            dict.Add("sign", sign);
            var reqPOST = HttpHelper.HttpRequest(
                 "POST"    //string getOrPost,
                , "http://api.fanyi.baidu.com/api/trans/vip/translate"    //string url,
                , null    //List < KeyValuePair < string, string >> headers,
                , dict    //List < KeyValuePair < string, string >> parameters,
                , Encoding.UTF8                    //Encoding dataEncoding,
                , "application/x-www-form-urlencoded;charset=utf-8"                                   //string contentType
                );
            System.IO.StreamReader readerPOST;
            readerPOST = new System.IO.StreamReader(reqPOST.GetResponseStream(), Encoding.UTF8);
            var respHTMLPOST = readerPOST.ReadToEnd(); //得到响应结果
            readerPOST.Close();
            reqPOST.Close();

            //清洗数据
            return cleanData(respHTMLPOST.Split("dst")[1].Split("\"")[2].ToLower().Trim().Replace(" ", "_").Replace("-", ""));
        }

        /// <summary>
        /// 按照自定义字典清洗机翻的数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public string cleanData(string data)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            if (ConfigurationManager.AppSettings["DictionaryMode2"] == "true")
            {
                //读取字典   
                StreamReader rd = File.OpenText(ConfigurationManager.AppSettings["DictionaryMode2Path"]);
                string d;
                // 循环读出文件的每一行
                while ((d = rd.ReadLine()) != null)
                {
                    dic.Add(d.Split('=')[0], d.Split('=')[1]);
                }

                string[] dataSplit = data.Split('_');
                foreach (var key in dic.Keys)
                {       
                    for (int i = 0; i < dataSplit.Length; i++)
                    {
                        if (dataSplit[i] == key)
                        {
                            dataSplit[i] = dic[key];
                        }
                    }
                };

                data = string.Join("_", dataSplit);
            }
            return data;
        }

        /// <summary>
        ///绘制进度条进度   
        /// </summary>
        /// <param name="percent"></param>
        /// <param name="colorBack"></param>
        /// <param name="colorFore"></param>
        public void draw(int percent, ConsoleColor colorBack, ConsoleColor colorFore)
        {
            Console.BackgroundColor = ConsoleColor.Yellow;//设置进度条颜色                
            Console.SetCursorPosition(percent / 2, 3);//设置光标位置,参数为第几列和第几行                
            Console.Write(" ");//移动进度条                
            Console.BackgroundColor = colorBack;//恢复输出颜色                
            //更新进度百分比,原理同上.                
            Console.ForegroundColor = ConsoleColor.Green;
            Console.SetCursorPosition(51, 2);
            Console.Write("{0}%", percent);
            Console.ForegroundColor = colorFore;
        }




        /// <summary>
        /// 判断字符串中是否包含中文
        /// </summary>
        /// <param name="str">需要判断的字符串</param>
        /// <returns>判断结果</returns>
        public bool HasChinese(string str)
        {
            return Regex.IsMatch(str, @"[\u4e00-\u9fa5]");
        }

    }
}
