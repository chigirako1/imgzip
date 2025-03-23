using MyZipper.src;
using System;
using System.IO;

namespace MyZipper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Log.I("##############################################>>>");
            var config = new Config(args);

            Log.I("------------------");
            Log.I("処理対象ディレクトリ：'{0}'", config.Inputpath);
            Log.I("------------------");
            Log.I("↓");
            //var dirname = Path.GetDirectoryName(config.OutputPath);
            //dirname = Path.GetFileName(dirname);
            var dirname = Util.GetParentDir(config.OutputPath);
            var fn = Path.GetFileNameWithoutExtension(config.OutputPath);
            Log.I($"出力ファイル名：'{fn}' ('{dirname}')");
            //Log.I($"{config.TargetScreenSize}({config.GetCanvasScreenRatio()})"); //Sizeをstringにすると"{width=99...}"形式になるので失敗

            var piclist = new PicInfoList(config.Inputpath, config);
            if (piclist.PicInfos.Count == 0) {
                Log.E("処理対象のファイルが存在しません。:'{0}'", config.Inputpath);
                Environment.Exit(1);
            }

            Log.I($"ファイル数={piclist.PicInfos.Count}, ファイルサイズ計={Util.FormatFileSize(piclist.FileSizeSum)}, 平均={Util.FormatFileSize(piclist.FileSizeAvg())}");

            try
            {
                if (config.Mode == Mode.PassThrough)
                {
                    var zipper = new Zipper(config);
                    zipper.PassThrough(piclist);
                    zipper.UpdateRecord();
                }
                else if (config.SplitLR == 0)
                {
                    var zipper = new Zipper(config);
                    zipper.OutputCombine(piclist);
                    zipper.UpdateRecord();
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
            Log.LogOutNoCRLF("");
            Log.I("<<<##############################################");
            Log.I("");
        }
    }
}
