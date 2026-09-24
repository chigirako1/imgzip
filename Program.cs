using MyZipper.src;
using System;
using System.Collections.Generic;
using System.IO;

namespace MyZipper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Log.I("##############################################>>>");
            var config = new Config(args);

            if (config.DivSubDir > 0)
            {
                ProcessSubdir(config);
            }
            else
            {
                var piclist = PrepareFilelist(config, config.InputPath, config.OutputPath, true);
                var result = MainRoutine(config, piclist);
                if (result < 0)
                {
                    Environment.Exit(1);
                }
            }
        }

        static void ProcessSubdir(Config config)
        {
            ProcessEachDirectories(config);

            if (config.DivSubDir >= 1)
            {
                var piclist = PrepareFilelist(config, config.InputPath, config.OutputPath, false);
                Log.I($"piclist = '{piclist.PicInfos.Count}'");

                var result = MainRoutine(config, piclist);
                if (result < 0)
                {
                    Log.E($"!!!!'{config.InputPath}'!!!");
                    Environment.Exit(1);
                }
            }
        }

        static void ProcessEachDirectories(Config config)
        {
            Log.I($"## '{config.InputPath}' ##");

            var orig_in_path = config.InputPath;
            var orig_out_path = config.OutputPath;

            var dir_list = new List<string>();

            var dirs = DirectoryLister.GetDirectoriesAtDepth(config.InputPath, config.DivSubDir);
            foreach (var d in dirs)
            {
                Log.I($"'{d}'");

                config.InputPath = d;

                string apnd_dir_name = GetAppendDirName(config.Mode, config.InputPath);
                config.OutputPath = GetOutputPath(config.OutputPath, apnd_dir_name, orig_out_path);

                var piclist = PrepareFilelist(config, config.InputPath, config.OutputPath, true);
                if (piclist.PicInfos.Count == 0)
                {
                    Log.W($"W:処理対象のファイルがないためスキップします。'{config.InputPath}'");
                    continue;
                }
                else if (piclist.PicInfos.Count < 10)
                {
                    // ファイル数が少ないので後でまとめて処理する
                    dir_list.Add(d);
                    continue;
                }

                var result = MainRoutine(config, piclist);
                if (result < 0)
                {
                    Log.E($"E:{result};'{d}'\n処理は続行...");
                }
            }

            if (dir_list.Count > 0)
            {
                config.InputPath = dir_list[0];

                string apnd_dir_name = GetAppendDirName(config.Mode, config.InputPath);
                config.OutputPath = GetOutputPath(config.OutputPath, apnd_dir_name, orig_out_path);

                var piclist = PrepareFilelist(config, dir_list, config.OutputPath, true);
                var result = MainRoutine(config, piclist);
                if (result < 0)
                {
                    Log.E($"E:{result};'{dir_list}'");
                }
            }

            config.OutputPath = orig_out_path;//相変わらずひどい
            config.InputPath = orig_in_path;
        }

        static PicInfoList PrepareFilelist(Config config, string InputPath, string OutputPath, bool alldir)
        {
            var dirs = new List<string>
            {
                InputPath
            };
            return PrepareFilelist(config, dirs, OutputPath, alldir);
        }

        static PicInfoList PrepareFilelist(Config config, List<string> dirs, string OutputPath, bool alldir)
        {
            Log.I($"処理対象ディレクトリ({dirs.Count})：'{dirs[0]}'");

            if (dirs.Count == 1)
            {
                var piclist = new PicInfoList(dirs[0], config, alldir);
                return piclist;
            }
            else
            {
                var piclist = new PicInfoList(dirs, config);
                return piclist;
            }
        }

        static int MainRoutine(Config config, PicInfoList piclist)
        {
            if (piclist.PicInfos.Count == 0)
            {
                Log.E($"処理対象のファイルが存在しません。:'{config.InputPath}'");
                return -1;
            }

            Log.I("------------------");
            Log.I("↓");
            var (dirname, fn) = Util.DivPathDirAndFile(config.OutputPath);
            Log.I($"出力ファイル名：'{fn}' ('{dirname}')");

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

        static string GetAppendDirName(Mode Mode, string InputPath)
        {
            var apnd_dir_name = "";

            if (Mode == Mode.Pxv)
            {
                var awinfo = PxvArtworkInfo.GetPxvArtworkInfoFromPath(InputPath);

                if (awinfo != null)
                {
                    apnd_dir_name = awinfo.ArtworkTitle;

                    /*var limit_n = 15;
                    if (apnd_dir_name.Length >= limit_n)
                    {
                        apnd_dir_name = apnd_dir_name.Substring(0, limit_n);
                    }*/

                    apnd_dir_name = Util.TruncateString(apnd_dir_name, 15, 14);

                    apnd_dir_name = StringHelper.RemoveEmoji(apnd_dir_name);
                    apnd_dir_name = " " + awinfo.DateToString() + " " + apnd_dir_name;
                }
            }
            return apnd_dir_name;
        }

        static string GetOutputPath(string OutputPath, string apnd_dir_name, string orig_out_path)
        {
            string output_path = Util.GetZipPath(orig_out_path, apnd_dir_name);
            if (File.Exists(output_path))
            {
                output_path = Util.GetUniqueFilePath(OutputPath);
            }
            return output_path;
        }
    }
}
