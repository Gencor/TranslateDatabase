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
    class Program
    {
        static void Main(string[] args)
        {
            Method method = new Method();

            ConsoleColor colorBack = Console.BackgroundColor;
            ConsoleColor colorFore = Console.ForegroundColor;
            SqlSugarClient db = Program.GetInstance();


            //字段集合
            List<string> columns = new List<string>();

            List<string> columnsRes = new List<string>();
            //注释集合
            List<string> remarks = new List<string>();

            Console.WriteLine("请输入需要翻译的表名，以回车结束");
            string table = Console.ReadLine();



            remarks.Add("ALTER TABLE public." + table);
            remarks.Add("    OWNER to postgres;");
            DataTable dt = db.Ado.GetDataTable("SELECT col_description ( A.attrelid, A.attnum ) AS description,format_type ( A.atttypid, A.atttypmod ) AS TYPE,A.attname AS attname,A.attnotnull AS NOTNULL FROM pg_class AS C, pg_attribute AS A WHERE C.relname = @tablename AND A.attrelid = C.oid AND A.attnum > 0", new { tablename = table });


            for (int k = 0; k < dt.Rows.Count; k++)
            {
                columns.Add(dt.Rows[k]["attname"].ToString());
                remarks.Add("COMMENT ON COLUMN \"public\".\"" + table + "\".\"" + dt.Rows[k]["attname"].ToString() + "\" IS '" + dt.Rows[k]["attname"].ToString() + "';");
            }

            //自定义字典
            Dictionary<string, string> customDic = new Dictionary<string, string>();
            if (ConfigurationManager.AppSettings["DictionaryMode2"] == "true")
            {
                //读取字典   
                StreamReader customDicLine = File.OpenText(ConfigurationManager.AppSettings["DictionaryMode2Path"]);
                string d;
                // 循环读出文件的每一行
                while ((d = customDicLine.ReadLine()) != null)
                {
                    customDic.Add(d.Split('=')[0], d.Split('=')[1]);
                }
            }


            Dictionary<string, string> databaseDic = new Dictionary<string, string>();

            //数据库字典模式开启
            if (ConfigurationManager.AppSettings["DictionaryMode1"] == "true")
            {

                DataTable tables = db.Ado.GetDataTable("SELECT tablename FROM pg_tables where schemaname = 'public'");

                for (int i = 0; i < tables.Rows.Count; i++)
                {
                    //表名不是汉字，说明已经被翻译过，可以加入字典
                    if (!method.HasChinese(tables.Rows[i]["tablename"].ToString()))
                    {
                        DataTable columnsDt = db.Ado.GetDataTable("SELECT col_description ( A.attrelid, A.attnum ) AS description,format_type ( A.atttypid, A.atttypmod ) AS TYPE,A.attname AS attname,A.attnotnull AS NOTNULL FROM pg_class AS C, pg_attribute AS A WHERE C.relname = @tablename AND A.attrelid = C.oid AND A.attnum > 0", new { tablename = tables.Rows[i]["tablename"].ToString() });
                        for (int j = 0; j < columnsDt.Rows.Count; j++)
                        {
                            //字段名不是汉字，说明已经被翻译过，可以加入字典
                            if (!method.HasChinese(columnsDt.Rows[j]["attname"].ToString()))
                            {
                                //防止重复加入
                                if (!databaseDic.ContainsKey(columnsDt.Rows[j]["description"].ToString()))
                                {
                                    databaseDic.Add(columnsDt.Rows[j]["description"].ToString(), columnsDt.Rows[j]["attname"].ToString());
                                }
                            }
                        }
                    }
                }
            }


            //绘制界面
            Console.WriteLine("********************* Loading *********************");
            Console.BackgroundColor = ConsoleColor.DarkCyan;
            for (int i = 0; ++i <= 50;)
            {
                Console.Write(" ");
            }
            Console.WriteLine(" ");
            Console.BackgroundColor = colorBack;

            Console.WriteLine("***************************************************");


            #region 弃用读取文档模式
            //// 打开文件准备读取数据   
            //StreamReader rd = File.OpenText(@"d:\data.txt");
            //string line;
            //bool isFindColumn = false;
            //string table = "";

            //// 循环读出文件的每一行
            //while ((line = rd.ReadLine()) != null)
            //{
            //    //找到了字段内容
            //    if (line.Contains("CREATE TABLE \"public\"."))
            //    {
            //        table = line.Split("\"")[3];
            //        isFindColumn = true;
            //        continue;
            //    }

            //    if (isFindColumn == true)
            //    {
            //        if (line.Length > 2)
            //        {
            //            remarks.Add("COMMENT ON COLUMN \"public\".\"" + table + "\".\"" + line.Split("\"")[1] + "\" IS '" + line.Split("\"")[1] + "';");
            //            columns.Add(line.Split("\"")[1]);
            //        }

            //    }

            //    //字段内容已经转换完毕
            //    if (line.Length == 1)
            //    {
            //        isFindColumn = false;
            //    }
            //}
            //rd.Close();
            #endregion



            //循环翻译字段名
            for (int i = 0; i < columns.Count; i++)
            {
                //如果字典中存在，则在字典中查询
                if (databaseDic.ContainsKey(columns[i]))
                {
                    method.draw(Convert.ToInt32(Math.Ceiling(((double)i / columns.Count) * 100)), colorBack, colorFore);
                    if (columns[i] != databaseDic[columns[i]])
                    {
                        columnsRes.Add(" ALTER TABLE " + table + " RENAME \"" + columns[i] + "\" TO " + databaseDic[columns[i]] + ";");
                    }

                }
                else
                {
                    method.draw(Convert.ToInt32(Math.Ceiling(((double)i / columns.Count) * 100)), colorBack, colorFore);
                    string res = method.Trans(columns[i]);
                    if (columns[i] != res)
                    {
                        columnsRes.Add(" ALTER TABLE "+table+" RENAME \"" + columns[i] + "\" TO " + res + ";") ;
                    }

                    //百度免费api限制，一秒一条
                    Thread.Sleep(1000);
                }
            }
            //完成
            method.draw(100, colorBack, colorFore);
            //恢复光标
            Console.SetCursorPosition(0, 5);//设置光标位置,参数为第几列和第几行     

            //循环打印
            foreach (string item in remarks)
            {
                Console.WriteLine(item);
            }
            foreach (string item in columnsRes)
            {
                Console.WriteLine(item);
            }

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }


        /// <summary>
        /// 数据库对象
        /// </summary>
        /// <returns></returns>
        private static SqlSugarClient GetInstance()
        {
            string test = ConfigurationManager.ConnectionStrings["conn"].ToString();//连接符字串
            //创建数据库对象
            SqlSugarClient db = new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ToString(),//连接符字串
                DbType = SqlSugar.DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute//从特性读取主键自增信息
            });

            return db;
        }
    }
}
