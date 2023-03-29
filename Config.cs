using System;
using System.Drawing;

namespace MyZipper
{


    //mode
    enum Mode
    {
        None,
        Auto = None,
    }

    //横長画像の扱い
    enum LandscapePicTreatmentWay
    {
        AsItIs,//そのまま
        Rotate,//元の位置で回転
        RotateAltogetherLater,//ファイルの最後にまとめて回転
        AsItIs_InAnotherFile,
        RotateAs_InAnotherFile,
    }

    internal class Config
    {
        public static string IDX_IMAGE_ENTRY_NAME = "000 cover_thumbs_idx.jpg";
        public static string GRAY_IMAGE_ENTRY_NAME = "ZZZ999empty.jpg";


        public Mode Mode { get; private set; }

        // input
        public string Inputpath { get; private set; }

        public Size TargetScreenSize { get; private set; }

        // output
        public string OutputDir { get; private set; }
        public string OutputFileame { get; private set; }
        public string OutputPath { get; private set; }

        // 出力Zipを分割するファイル数（0なら分割しない)
        public int OutputFileDivedeThreshold { get; private set; }


        //縦長画像用の分割数
        public int NumberOfSplitScreenVforPlImage { get; private set; }//垂直分割数
        public int NumberOfSplitScreenHforPlImage{ get; private set; }
        //横長画像用の分割数
        public int NumberOfSplitScreenVforLsImage { get; private set; }
        public int NumberOfSplitScreenHforLsImage { get; private set; }




        public double allowPer { get; private set; }

        public DateTime? Since { get; set; }

        // 縦長、横長画像を別のzipファイルに出力するか
        public bool isSeparateOutput { get; private set; }

        // 横長画像を回転するか
        public bool isRotatePlImage { get; private set; }
        public bool isRotateAlt{ get; private set; }
        public bool isPicSizeDraw { get; private set; }
        public bool IsForce2P { get; private set; }
        public bool isAppendIdxCover { get; private set; }
        public bool isSplitLongImage { get; private set; }

        public int IdxOutThreshold { get; private set; }

        public Config(string[] args)
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("引数の数が足りません。Usage: myzipper <options> <出力パス> <入力パス> <YYYY/MM/DD>");
                Environment.Exit(1);
            }

            string outPath = args[1];
            string inPath = args[2];
            var mode = Mode.Auto;

            init(inPath, outPath, mode);

            SetOptions(args[0]);

            if (args.Length >= 4)
            {
                try
                {
                    Since = DateTime.ParseExact(args[3], "yyyy/MM/dd", null);
                }
                catch (FormatException ex)
                {
                    Console.Error.WriteLine("E:オプションが不正です。{}", ex);
                    Environment.Exit(1);
                }
            }
        }


        private void init(string inputpath, string outputPath, Mode mode)
        {
            Inputpath = inputpath;
            OutputPath = outputPath;
            Mode = mode;

            TargetScreenSize = new Size(1200, 1920);//10:16=5:8
            //TargetScreenSize = new Size(1920, 1200);
            OutputFileDivedeThreshold = 0;
            isSeparateOutput = false;
            if (Mode == Mode.Auto)
            {
                isRotatePlImage = true;
            }
            else
            {
                isRotatePlImage = true;// false;
            }
            NumberOfSplitScreenVforPlImage = 2;
            NumberOfSplitScreenHforPlImage = 2;
            NumberOfSplitScreenVforLsImage = 1;
            NumberOfSplitScreenHforLsImage = 2;

            allowPer = 0.9;

            Since = null;

            isPicSizeDraw = false;
            IsForce2P = false;
            isRotateAlt = true;

            IdxOutThreshold = 5;
        }

        private void SetOptions(string options)
        {
            foreach (var c in options)
            {
                switch (c)
                {
                    case '2':
                        IsForce2P = true;
                        break;
                    case 'a':
                        break;
                    case 'c':
                        isAppendIdxCover = true;
                        break;
                    case 'i':
                        isPicSizeDraw = true;
                        break;
                    case 'r':
                        isRotatePlImage = true;
                        break;
                    case 'R':
                        isRotateAlt = false;
                        break;
                    case 's':
                        isSplitLongImage = true;
                        break;
                    default:
                        Console.Error.WriteLine("E:オプションが不正です。");
                        Environment.Exit(1);
                        break;
                }
            }
        }

        public float GetCanvasScreenRatio()
        {
            return (float)TargetScreenSize.Width / (float)TargetScreenSize.Height;
        }

        public bool IsYohakuAddtion(float aspRatio)
        {
            return true;
            //return (aspRatio < 0.55 || aspRatio > 2.0);
        }

        public bool RotatePredicate(Size imgSize)
        {
            if (!isRotatePlImage)
            {
                return false;
            }

            //※横長なら一律回転
            if (imgSize.Width > imgSize.Height)
            {
                return true;
            }

            if (TargetScreenSize.Width <= TargetScreenSize.Height)
            {

                //return isRotatePlImage && (imgSize.Width > imgSize.Height) && (imgSize.Width > TargetScreenSize.Width);
                return (imgSize.Width > imgSize.Height) && (imgSize.Width >= TargetScreenSize.Width);
            }
            else
            {
                return (imgSize.Width < imgSize.Height) && (imgSize.Height >= TargetScreenSize.Height);
            }
        }
    }
}
