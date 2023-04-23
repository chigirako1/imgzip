﻿using System;

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

            var zipper = new Zipper(config);
            try
            {
                zipper.OutputCombine(piclist);
            }
            catch (Exception ex)
            {
                Log.E(ex.ToString());
                Environment.Exit(1);
            }
        }
    }
}
