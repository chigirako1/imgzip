using System;

namespace MyZipper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var config = new Config(args);

            Console.WriteLine("[dbg] '{0}' -> '{1}'", config.Inputpath, config.OutputPath);
            Console.WriteLine("[dbg] {0}({1})", config.TargetScreenSize, config.GetCanvasScreenRatio());

            var piclist = new PicInfoList(config.Inputpath, config);
            if (piclist.PicInfos.Count == 0) {
                Console.Error.WriteLine("E:処理対象のファイルが存在しません。");
                Environment.Exit(1);
            }

            var zipper = new Zipper(config);
            try
            {
                zipper.OutputCombine(piclist);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
        }
    }
}
