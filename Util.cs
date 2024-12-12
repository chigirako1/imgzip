using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace MyZipper
{
    public class Util
    {
        public static string FormatFileSize(long bytes)
        {
            var unit = 1024;
            if (bytes < unit) { return $"{bytes} B"; }

            var exp = (int)(Math.Log(bytes) / Math.Log(unit));
            return $"{bytes / Math.Pow(unit, exp):F2} {("KMGTPE")[exp - 1]}B";
        }

        static public string GetUploadDate(string path)
        {
            Regex r = new Regex(@"(\d+) \d+ \d+-\d+-\d+");
            Match m = r.Match(path);
            if (m.Success)
            {
                var tweet_id = long.Parse(m.Groups[1].Value);
                //Log.I($"tweet_id={tweet_id}");

                var d = Twt.GetDateTime(tweet_id);
                return d.ToString();
            }

            return "";
        }

        static public string GetEntryName(string path)
        {
            var dirname = Path.GetDirectoryName(path);
            dirname = Path.GetFileName(dirname);
            var fn = Path.GetFileNameWithoutExtension(path);

            return dirname + "-" + fn;
        }

        static public string GetTitle(string path)
        {
            var dirname = Path.GetDirectoryName(path);
            dirname = Path.GetFileName(dirname);
            var fn = Path.GetFileNameWithoutExtension(path);

            return Path.Combine(dirname, fn);
        }
        static public string GetExt(string path)
        {
            return Path.GetExtension(path);
        }

        static public string GetZipPath(string path, int cnt, int totalNo)
        {



            var dirname = Path.GetDirectoryName(path);
            
            var fn = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);

            string type = "D"; // 補間タイプ ("D"=10進数)
            var digit = 3;  // 桁数
            if (totalNo > 999)
            {
                digit = 4;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(type);
            sb.Append(digit);

            //return Path.Combine(dirname, $"{fn}[{cnt:D3}]{ext}");
            return Path.Combine(dirname, $"{fn}[{cnt.ToString(sb.ToString())}]{ext}");
        }

        static public string AppendPostfixToFilename(string origName, string appdStr)
        {
            var dirname = Path.GetDirectoryName(origName);
            var fn = Path.GetFileNameWithoutExtension(origName);
            var ext = Path.GetExtension(origName);

            return Path.Combine(dirname, fn + appdStr + ext);
        }

        static public string GetAspectRatioStr10_16(int width, int height)
        {
            if (width > height)
            {
                double ratio = (double)height / (double)width;
                var str = string.Format("16:{0}", (int)Math.Truncate(16 / ratio));
                return str;
            }
            else
            {
                double ratio = (double)height / (double)width;
                var str = string.Format("{0}:16", (int)Math.Truncate(ratio * 16));
                return str;
            }
        }
    }

    public class Log
    {
        public static bool Quiet;
        public static bool Dbg = false;
        public static bool Verbose = false;

        static public void E(string s, params Object[] args)
        {
            LogOut("[E] ", s, args);
        }

        static public void W(string s, params Object[] args)
        {
            LogOut("[W] ", s, args);
        }

        static public void D(string s, params Object[] args)
        {
            if (Dbg)
            { 
                LogOut("[D] ", s, args);
            }
        }

        static public void I(string s, params Object[] args)
        {
            LogOut("[I] ", s, args);
        }

        static public void V(string s, params Object[] args)
        {
            if (Verbose)
            {
                LogOut("[V] ", s, args);
            }
        }

        static private void LogOut(string prefix, string s, params Object[] args)
        {
            Console.Error.Write(prefix);
            Console.Error.WriteLine(s, args);
        }

        static public void LogOutNoCRLF(string s)
        {
            Console.Error.Write(s);
        }

        static public void LogOut(string s)
        {
            Console.Error.WriteLine(s);
        }
    }

    public class Zip
    {
        static public void CreateEntryFromFile(ZipArchive archive, string rootpath, string infilepath)
        {
            var subdir = infilepath.Replace(rootpath, "");
            Log.V("subidr:" + subdir);
            //var e = archive.CreateEntry(subdir);

            //var filename = Path.GetFileName(fullpath);
            /*var entry = */archive.CreateEntryFromFile(infilepath, subdir);
        }
    }
}
