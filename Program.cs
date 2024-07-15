using MyZipper.src;
using System;

namespace MyZipper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var config = new Config(args);

            Log.I("'{0}' -> '{1}'", config.Inputpath, config.OutputPath);
            Log.V("{0}({1})", config.TargetScreenSize, config.GetCanvasScreenRatio());

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
