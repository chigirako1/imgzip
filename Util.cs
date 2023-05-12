using System;
using System.IO;

namespace MyZipper
{
    public class Util
    {
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

        static public void E(string s, params Object[] args)
        {
            Console.Error.Write("[E] ");
            Console.Error.WriteLine(s, args);
        }

        static public void W(string s, params Object[] args)
        {
            Console.Error.Write("[W] ");
            Console.Error.WriteLine(s, args);
        }

        static public void D(string s, params Object[] args)
        {
            Console.Error.Write("[D] ");
            Console.Error.WriteLine(s, args);
        }

        static public void I(string s, params Object[] args)
        {
            Console.Error.Write("[I] ");
            Console.Error.WriteLine(s, args);
        }

        static public void V(string s, params Object[] args)
        {
            LogOut("[V] ", s, args);
        }

        static private void LogOut(string prefix, string s, params Object[] args)
        {
            if (Quiet)
            {
                return;
            }
            Console.Write(prefix);
            Console.WriteLine(s, args);
        }
    }
}
