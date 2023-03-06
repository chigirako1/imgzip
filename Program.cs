using System;

namespace MyZipper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("引数の数が足りません。Usage: myzipper <mode> <出力パス> <入力パス> <YYYY/MM/DD>");
                Environment.Exit(1);
            }

            string outPath = args[1];
            string inPath = args[2];
            var mode = Mode.Auto;
            switch (args[0])
            {
                case "rclv":
                case "a":
                case "auto":
                    mode = Mode.Auto;
                    break;
                default:
                    Console.Error.WriteLine("E:オプションが不正です。");
                    Environment.Exit(1);
                    break;
            }
            var config = new Config(inPath, outPath, mode);
            if (args.Length >= 4)
            {
                config.Since = DateTime.ParseExact(args[3], "yyyy/MM/dd", null);
            }

            Console.WriteLine("[dbg] '{0}' -> '{1}'", config.Inputpath, config.OutputPath);
            Console.WriteLine("[dbg] {0}({1})", config.TargetScreenSize, config.GetCanvasScreenRatio());

            var piclist = new PicInfoList(config.Inputpath, config);
            if (piclist.PicInfos.Count == 0) {
                Console.Error.WriteLine("E:処理対象のファイルが存在しません。");
            }
            var zipper = new Zipper(config);
            try
            {
                zipper.OutputJoin(piclist);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
            }

        }
    }
}
