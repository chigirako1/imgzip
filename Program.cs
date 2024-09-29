using MyZipper.src;
using System;
using System.IO;

namespace MyZipper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var config = new Config(args);

            Log.I("##############################################");
            Log.I("'{0}'", config.Inputpath);
            Log.I("↓");
            var dirname = Path.GetDirectoryName(config.OutputPath);
            dirname = Path.GetFileName(dirname);
            Log.I($"'{dirname}'");
            var fn = Path.GetFileNameWithoutExtension(config.OutputPath);
            Log.I($"'{fn}'");
            Log.V("{0}({1})", config.TargetScreenSize, config.GetCanvasScreenRatio());
            Log.I("##############################################");

            var piclist = new PicInfoList(config.Inputpath, config);
            if (piclist.PicInfos.Count == 0) {
                Log.E("処理対象のファイルが存在しません。:'{0}'", config.Inputpath);
                Environment.Exit(1);
            }

            try
            {
                if (config.Mode == Mode.PassThrough)
                {
                    var zipper = new Zipper(config);
                    zipper.PassThrough(piclist);
                }
                else if (config.SplitLR == 0)
                {
                    var zipper = new Zipper(config);
                    zipper.OutputCombine(piclist);
                }
                else
                {
                    var splitter = new Splitter(config);
                    splitter.Split(piclist);
                }
            }
            catch (Exception ex)
            {
                Log.E(ex.ToString());
                Environment.Exit(1);
            }
        }
    }
}
