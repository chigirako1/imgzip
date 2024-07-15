using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyZipper
{
    internal class Pxv
    {
        static public int GetPxvID(string path)
        {
            var dirname = Path.GetFileName(path);

            Regex r = new Regex(@"\w+\(#?(\d+)\)");
            Match m = r.Match(dirname);
            if (m.Success)
            {
                var pxvid = int.Parse(m.Groups[1].Value);
                return pxvid;
            }
            else
            {
                Log.E($"not found: '{path}'");
                // Environment.Exit(1);
                return 0;
            }
        }
    }
}
