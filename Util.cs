using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyZipper.src
{
    internal class Util
    {
        static public string GetTitle(string path)
        {
            var dirname = Path.GetDirectoryName(path);
            var fn = Path.GetFileNameWithoutExtension(path);

            return Path.Combine(Path.GetFileName(dirname), fn);
        }

        static public string AppendPostfixToFilename(string origName, string appdStr)
        {
            var dirname = Path.GetDirectoryName(origName);
            var fn = Path.GetFileNameWithoutExtension(origName);
            var ext = Path.GetExtension(origName);

            return Path.Combine(dirname, fn + appdStr + ext);
        }
    }
}
