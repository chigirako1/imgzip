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

            var orig_in_path = config.InputPath;
            var orig_out_path = config.OutputPath;

            if (config.DivSubDir > 0)
            {
                var dirs = Util.GetDirectories(config.InputPath);
                foreach (var d in dirs)
                {
                    var apnd_dir_name = Path.GetFileName(d);
                    config.InputPath = Path.Combine(orig_in_path, apnd_dir_name);


                    config.OutputPath = Util.GetZipPath(orig_out_path, apnd_dir_name);
                    //Log.D($"'{orig_in_path}'/'{apnd_dir_name}'/' => {config.InputPath}'");
                    //Log.D($"'{orig_out_path}' => '{config.OutputPath}'");

                    //TODO: 同一のファイル名になった場合の処理 ファイル名+数字もしくは時間？
                    //ループ追加
                    if (Directory.Exists(config.OutputPath))
                    {
                        config.OutputPath = Util.GetZipPath(config.OutputPath, "1");
                    }

                    var piclist = PrepareFilelist(config);
                    var result = MainRoutine(config, piclist);
                    if (result < 0)
                    {
                        Log.E($"処理を続行");
                    }
                }
            }
            else
            {
                var piclist = PrepareFilelist(config);
                var result = MainRoutine(config, piclist);
                if (result < 0)
                {
                    Environment.Exit(1);
                }
            }
        }

        static PicInfoList PrepareFilelist(Config config)
        {
            Log.I("------------------");
            Log.I("処理対象ディレクトリ：'{0}'", config.InputPath);
            Log.I("------------------");
            Log.I("↓");
            var (dirname, fn) = Util.DivPathDirAndFile(config.OutputPath);
            Log.I($"出力ファイル名：'{fn}' ('{dirname}')");

            var piclist = new PicInfoList(config.InputPath, config);
            return piclist;
        }

        static int MainRoutine(Config config, PicInfoList piclist)
        {
            if (piclist.PicInfos.Count == 0)
            {
                Log.E($"処理対象のファイルが存在しません。:'{config.InputPath}'");
                return -1;
            }

            Log.I($"{piclist.PicInfos.Count} ファイル。 ファイルサイズ計={Util.FormatFileSize(piclist.FileSizeSum)}, 平均={Util.FormatFileSize(piclist.FileSizeAvg())}");

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

            return 0;
        }
    }
}
