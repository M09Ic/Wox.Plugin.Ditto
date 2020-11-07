using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.Entity.Infrastructure;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Media.Effects;
using Wox.Plugin.Ditto;

namespace Wox.Plugin.DittoLib
{

    interface Mod
    {
        Dictionary<string, string> ShowHelp();
        Dictionary<string, string> Run(string[] inputlist);

    }
    class DataBase
    {
        private string cs;
        private SQLiteConnection con;
        public DataBase(string PuginDir)
        {
            cs = $"URI=file:{PuginDir}/ditto.db";
            con = new SQLiteConnection(cs);
            con.Open();
            CreateTable();
        }
        private bool CreateTable()
        {
            try
            {
                var cmd = new SQLiteCommand(con);
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Dittolog(
                id INTEGER  PRIMARY KEY autoincrement,
                mod varchar(128) NOT NULL,
                option varchar(128) ,
                input longtext NOT NULL,
                output longtext,
                key text ,
                `datetime` datetime DEFAULT CURRENT_TIMESTAMP)
                ";
                cmd.ExecuteNonQuery();
                return true;

            }
            catch (Exception)
            {

                return false;
            }
        }
        public bool InsertLog(string mod, string option, string input, string output = "", string key = "")
        {
            try
            {
                var cmd = new SQLiteCommand(con);
                cmd.CommandText = "INSERT INTO Dittolog (`mod`,`option`,`input`,`output`,`key`) VALUES (@mod,@option,@input,@output,@key)";
                cmd.Parameters.AddWithValue("@mod", mod);
                cmd.Parameters.AddWithValue("@option", option);
                cmd.Parameters.AddWithValue("@input", input);
                cmd.Parameters.AddWithValue("@output", output);
                cmd.Parameters.AddWithValue("@key", key);
                cmd.Prepare();
                cmd.ExecuteNonQuery();
                return true;

            }
            catch (Exception)
            {

                return false;
            }
        }

        public List<LogInfo> ReadLog(string mod, string option, string input = "")
        {
            var cmd = new SQLiteCommand(con);
            var loginfolist = new List<LogInfo>();
            cmd.CommandText = "SELECT input,output,count(*) FROM Dittolog WHERE mod=@mod AND option=@option AND input LIKE @input GROUP BY output";
            cmd.Parameters.AddWithValue("@mod", mod);
            cmd.Parameters.AddWithValue("@option", option);
            cmd.Parameters.AddWithValue("@input", $"%{ input}%");
            cmd.Prepare();
            SQLiteDataReader rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {

                loginfolist.Add(new LogInfo()
                {
                    input = rdr.GetString(0),
                    output = rdr.GetString(1),
                    score = rdr.GetInt32(2)
                });
            }
            return loginfolist;
        }
        public void DbClose()
        {
            con.Close();
        }
    }


    class LogInfo
    {
        public string input;
        public string output;
        public int score;
    }

    class ModFactory
    {
        private static IEnumerable<Type> mods = Initmods();
        private static string[] modnames = GetModNames() ;
        private static Dictionary<string, string> modsdesc = InitModsDesc();
        private static Dictionary<string, string> modsdict = InitModsDict();
        private static IEnumerable<Type> Initmods()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(Mod))));
        }

       
        private static Dictionary<string, string> InitModsDesc()
        {
            Dictionary<string, string> modsdesctmp = new Dictionary<string, string>();
            foreach (var mod in mods)
            {
                var modnameField = mod.GetField("modname");
                var descField = mod.GetField("desc");
                modsdesctmp.Add(modnameField.GetValue(mod).ToString(), descField.GetValue(mod).ToString());
            }
            return modsdesctmp;
        }

        private static Dictionary<string, string> InitModsDict()
        {
            Dictionary<string, string> modsdicttmp = new Dictionary<string, string>();
            foreach (var mod in mods)
            {
                var modnameField = mod.GetField("modname");

                modsdicttmp.Add(modnameField.GetValue(mod).ToString(), mod.FullName);
            }
            return modsdicttmp;
        }
        public static Dictionary<string, string> GetModsDesc() { return modsdesc; }

        public static string[] GetModNames() {
            string[] modnames = { };
            foreach (var mod in mods)
            {
                modnames.Append(mod.Name);
            }
            return modnames;
        }
        public static Dictionary<string, string> GetHelp(string modname)
        {

            Type modtype = Type.GetType(modsdict[modname]);
            Object modclass = System.Activator.CreateInstance(modtype);
            var helpmethod = modtype.GetMethod("ShowHelp");
            var resultdict = helpmethod.Invoke(modclass, null) as Dictionary<string, string>;
            return resultdict;
        }
        public static Dictionary<string, string> RunMod(string modname, string[] inputlist)
        {
            Type modtype = Type.GetType(modsdict[modname]);
            Object modclass = System.Activator.CreateInstance(modtype);
            var helpmethod = modtype.GetMethod("Run");
            Main.error = modname;
            var resultdict = helpmethod.Invoke(modclass, new object[] { inputlist }) as Dictionary<string, string>;
            return resultdict;
        }
    }
    class Hex : Mod
    {
        public static string modname = "hex";
        public static string desc = "hex encode or decode";
        public Dictionary<string, string> ShowHelp()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("en", "usage: di hex en <string>/<file>");
            dict.Add("de", "usage: di hex de <string>/<file>");
            return dict;
        }
        public string Hexlify(string s)
        {
            System.Text.Encoding chs = System.Text.Encoding.GetEncoding("ascii");
            byte[] bytes = chs.GetBytes(s);
            string str = "";
            for (int i = 0; i < bytes.Length; i++)
            {
                str += string.Format("{0:X}", bytes[i]);
            }
            return str.ToLower();
        }
        public string Unhexlify(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = byte.Parse(hex.Substring(i * 2, 2),
                System.Globalization.NumberStyles.HexNumber);
            }
            System.Text.Encoding chs = System.Text.Encoding.GetEncoding("ascii");
            return chs.GetString(bytes);
        }
        public Dictionary<string, string> Run(string[] inputlist)
        {
            
            var dict = new Dictionary<string, string>();
            switch (inputlist[1])
            {
                case "en":
                    dict.Add(Hexlify(inputlist[2]), inputlist[2]);
                    break;
                case "de":
                    try
                    {
                        dict.Add(Unhexlify(inputlist[2]), inputlist[2]);
                    }
                    catch (Exception)
                    {
                        if (inputlist[2].Length % 2 !=0)
                        {
                            dict.Add("解码错误:", "hex长度错误: " + inputlist[2]);
                        }
                        else if (!System.Text.RegularExpressions.Regex.IsMatch(inputlist[2], "^[0-9A-Fa-f]+$"))
                        {
                            dict.Add("解码错误:", "存在非16进制字符串: " + inputlist[2]);
                        }
                        else
                        {
                            dict.Add("解码错误:", "解码错误: " + inputlist[2]);
                        }
                    }
                    break;
                default:
                    dict.Add("编码/解码错误", "参数错误");
                    break;
            }
            return dict;
        }
    }

    class AsciiCase : Mod
    {
        public static string modname = "case";
        public static string desc = "Upper or Lower String";
        public  Dictionary<string, string> ShowHelp()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("low", "usage: di case low <string>/<file>");
            dict.Add("up", "usage: di case up <string>/<file>");
            return dict;
        }
        public string Low(string s)
        {
            return s.ToLower();
        }
        public string Up(string s)
        {
            return s.ToUpper();
        }

        public  Dictionary<string, string> Run(string[] inputlist)
        {
            var dict = new Dictionary<string, string>();
            switch (inputlist[1])
            {
                case "low":
                    dict.Add(Low(inputlist[2]), inputlist[2]);
                    break;
                case "up":
                    dict.Add(Up(inputlist[2]), inputlist[2]);
                    break;
                default:
                    dict.Add("编码/解码错误", "参数错误");
                    break;
            }
            return dict;
        }
    }

    class Base64 :Mod
    {
        public static string modname = "b64";
        public static string desc = "Base64 encode or decode string";

        public  Dictionary<string, string> ShowHelp()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("en", "usage: di b64 en <string>/<file>");
            dict.Add("de", "usage: di b64 de <string>/<file>");
            return dict;
        }
        public  string Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public  string Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);

        }

        public  Dictionary<string,string> Run(string[] inputlist)
        {
            var dict = new Dictionary<string, string>();
            switch (inputlist[1])
            {
                case "en":
                    dict.Add(Encode(inputlist[2]),inputlist[2]);
                    break;
                case "de":
                    try
                    {
                        dict.Add(Decode(inputlist[2]),inputlist[2]);
                    }
                    catch (Exception)
                    {

                        dict.Add("解码错误:", "输入错误: "+inputlist[2]);
                    }
                    break;
                default:
                    dict.Add("编码/解码错误", "参数错误");
                    break;
            }
            return dict;
        }
    }
    class Hash : Mod
    {
        public static string modname = "hash";
        public static string desc = "Use md5/sha* hash string";

        public  Dictionary<string, string> ShowHelp()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("md5", "usage: di hash md5 <string>/<file>");
            dict.Add("sha1", "usage: di hash sha1 <string>/<file>");
            dict.Add("sha256", "usage: di hash sha256 <string>/<file>");
            dict.Add("sha512", "usage: di hash sha512 <string>/<file>");
            return dict;
        }
        public  string MD5Encode(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
        public  string SHA1Encode(string input)
        {
            using (System.Security.Cryptography.SHA1 sha1 = System.Security.Cryptography.SHA1.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = sha1.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public  string SHA256Encode(string input)
        {
            using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }


        public  string SHA512Encode(string input)
        {
            using (System.Security.Cryptography.SHA512 sha512 = System.Security.Cryptography.SHA512.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = sha512.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public  Dictionary<string, string> Run(string[] inputlist)
        {
            var dict = new Dictionary<string, string>();
            switch (inputlist[1])
            {
                case "md5":
                    dict.Add(MD5Encode(inputlist[2]), inputlist[2]);
                    break;
                case "sha1":
                    dict.Add(SHA1Encode(inputlist[2]), inputlist[2]);
                    break;
                case "sha256":
                    dict.Add(SHA256Encode(inputlist[2]),inputlist[2]);
                    break;
                case "sha512":
                    dict.Add(SHA512Encode(inputlist[2]),inputlist[2]);
                    break;
                default:
                    dict.Add("编码/解码错误", "参数错误");
                    break;
            }
            return dict;
        }
    }
}
