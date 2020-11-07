using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Wox.Plugin;
using Wox.Plugin.DittoLib;

namespace Wox.Plugin.Ditto
{

    public class Main : IPlugin
    {
        private Dictionary<string, string> mods;
        private DataBase db;
        private string querystr;
        public static string error;
    public void Init(PluginInitContext context) {
            this.mods = ModFactory.GetModsDesc();
            this.db = new DataBase(context.CurrentPluginMetadata.PluginDirectory);
        }
        public List<Result> Query(Query query) {
            querystr = query.Search;
            string[] querylist = querystr.Split(' ');
            querylist = QueryToLower(querylist);
            var results = new List<Result>();
            switch (querylist.Length)
            {
                case 0:
                    // 当没有输入任何内容的时候(仅输入了插件名di),返回全部模块的简介
                    results = DictToResultList(mods);
                    break;
                case 1:
                    // 当输入了模块名的时候,模糊匹配输入值,如果精确匹配到模块名,则输出对应模块的帮助信息.
                    results = DictToResultList(GetMatchingItems(mods, querylist[0]));
                    if (IsCompeteMatchingItems(mods, querylist[0]))
                    {
                        results.AddRange(DictToResultList(ModFactory.GetHelp(querylist[0])));
                    }
                    break;
                case 2:
                    // 当输入模块名与对应模块的参数的时候,模糊匹配参数值,如果精确匹配到参数值,输出历史记录.
                    var helpdict = ModFactory.GetHelp(querylist[0]);
                    results = DictToResultList(GetMatchingItems(helpdict,querylist[1]));
                    if (IsCompeteMatchingItems(helpdict, querylist[1]))
                    {
                        var loginfos = db.ReadLog(querylist[0], querylist[1]);
                        foreach (var loginfo in loginfos)
                        {
                            results.AddRange(DictToResultList(new Dictionary<string, string>() { { loginfo.output, loginfo.input } }, loginfo.score, "log"));
                        }
                    }
                    break;
                case 3:
                default:
                    //当输入了模块名与对应模块的参数以及带处理字符串的时候,输出对应待处理字符串的处理结果,并且模糊匹配待处理字符串输出历史记录
                     string[] querylist2 = JoinQuery(querylist);
                    results = DictToResultList(ModFactory.RunMod(querylist2[0], querylist2), action: "log");
                    //  error = ModFactory.RunMod(querylist2[0], querylist2).Count().ToString();
                    var loginfoswithinput = db.ReadLog(querylist2[0].ToLower(), querylist2[1], querylist2[2]);
                    foreach (var loginfo in loginfoswithinput)
                    {
                        results.AddRange(DictToResultList(new Dictionary<string, string>() { { loginfo.output, loginfo.input } }, loginfo.score, "log"));
                    }
                    break;
            }
            // results.Add(new Result() { Title = error });
            return  results ;
        }

        //public Dictionary<string, string> CallHelp(string modname) {
        //    var dict = new Dictionary<string, string>();
        //    // 调用对应模块的帮助
        //    switch (modname)
        //    {
        //        case "b64":
        //            dict = Base64.ShowHelp();
        //            break;
        //        case "hash":
        //            dict = Hash.ShowHelp();
        //            break;
        //        case "hex":
        //            dict = Hex.ShowHelp();
        //            break;
        //        case "case":
        //            dict = AsciiCase.ShowHelp();
        //            break;
        //        default:
        //            dict = new Dictionary<string, string>() { { "error", "未找到对应模块" } };
        //            break;
        //    }
        //    return dict;
        //}
        //public Dictionary<string, string> CallMod(string[] querylist)
        //{
        //    // 调用对应模块
        //    var dict = new Dictionary<string, string>();

        //    switch (querylist[0])
        //    {
        //        case "b64":
        //            dict = Base64.Run(querystr);
        //            break;
        //        case "hash":
        //            dict = Hash.Run(querystr); 
        //            break;
        //        case "hex":
        //            dict = Hex.Run(querystr);
        //            break;
        //        case "case":
        //            dict = AsciiCase.Run(querystr);
        //            break;
        //        default:
        //            break;
        //    }
        //    return dict;
        //}

        //public Dictionary<string, string> ShowMods()
        //{
        //    // 展示当前已有的模块
        //    Dictionary<string, string> ModsDict = new Dictionary<string, string>();
        //    ModsDict.Add("b64", "usage: di b64 en/de <string>");
        //    ModsDict.Add("hash", "usage: di hash md5/sha1/sha256 <string>");
        //    ModsDict.Add("hex", "usage: di hex en/de <string>");
        //    ModsDict.Add("case", "usage: di case up/low <string>");

        //    return ModsDict;
        //}
        public string[] QueryToLower(string[] querylist)
        {
            // 将前两个参数转为小写.
            for (int i = 0; i < querylist.Length; i++)
            {
                if (i<=1)
                {
                    querylist[i] = querylist[i].ToLower();
                }
            }
            return querylist;
        }
        public string[] JoinQuery(string[] querylist)
        {
            // 当空格多余三个,将多余的空格拼接到输入值
            if (querylist.Length>2)
            {
                string[] result = new string[3];
                string val = "";
                for (int i = 2; i < querylist.Length; i++)
                {

                    val = val + querylist[i]+" ";
                }
                
                result[0] = querylist[0];
                result[1] = querylist[1];
                result[2] = val.Trim();
                return result;
            }
            else
            {
                return querylist;
            }
            
        }




        public bool IsCompeteMatchingItems(Dictionary<string, string> sDict, string searchquery)
        {
            // 精确匹配字典键值是否与输入值相等
            Dictionary<string, string> ResultDict = new Dictionary<string, string>();
            foreach (var s in sDict)
            {
                if (searchquery == s.Key)
                {
                    return true;
                }
            }
            return false;
        }
        public Dictionary<string, string> GetMatchingItems(Dictionary<string, string> sDict , string searchquery)
        {
            // 模糊匹配字典键值中包括输入值的项
            Dictionary<string, string> ResultDict = new Dictionary<string, string>();
            foreach (var s in sDict)
            {
                if (s.Key.Contains(searchquery))
                {
                    ResultDict.Add(s.Key,s.Value);
                }
            }
            if (ResultDict.Count == 0)
            {
                ResultDict.Add("模块或参数错误,无匹配值", "error,can't match mod or option" );
            }
            return ResultDict;
        }

        
        public Result FormatResult(string title,string subtitle,int score,string action)
        {
            //标准化Result结构
            Result result = new Result();
            result.Title = title;
            result.SubTitle = subtitle;
            result.Score = score;
            if (action == "copy")
            {
                result.Action = ctx =>

                {
                    Clipboard.SetText(title);
                    return true;
                };
            }
            else if (action == "log" && !subtitle.Contains("错误"))
            {
                string[] querylist = JoinQuery(querystr.Split(' '));
                result.Action = ctx =>

                {
                    Clipboard.SetText(title);
                    db.InsertLog(querylist[0] , querylist[1] , querylist[2],title);
                    return true;
                };
            }
            return result;
        }


        public List<Result> DictToResultList(Dictionary<string, string> Dict,int score = 100, string action = "copy")
        {
            // 将字典转换成Result
            var results = new List<Result>();
            
            foreach (var d in Dict)
            {
                results.Add(FormatResult(d.Key, d.Value, score,action));
            }
            return results;
        }
    }
    
}
 