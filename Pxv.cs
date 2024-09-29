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
                Log.E($"pxv id not found: '{path}'");
                // Environment.Exit(1);
                return 0;
            }
        }

        static public string GetPxvArtworkTitleFromPath(string path)
        {
            Regex rgx = new Regex(@"(\d\d-\d\d-\d\d)\s+(.*)\(\d+\)");
            Match m = rgx.Match(path);
            if (m.Success)
            {
                //var date = m.Groups[1].Value;
                var title = m.Groups[2].Value;
                //var artwork_id = m.Groups[3].Value;
                //Log.I($"{title}\t{path}");
                return title;
            }
            else
            {
                Log.W("'{0}'", path);
                return "";
            }
        }
    }
}
