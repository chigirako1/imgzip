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
        public Mode Mode { get; set; }

        // input
        public string Inputpath { get; set; }

        public Size TargetScreenSize { get; set; }

        // output
        public string OutputDir { get; set; }
        public string OutputFileame { get; set; }
        public string OutputPath { get; set; }

        // 出力Zipを分割するファイル数（0なら分割しない)
        public int OutputFileDivedeThreshold { get; set; }

        // 縦長、横長画像を別のzipファイルに出力するか
        public bool isSeparateOutput { get; set; }

        // 横長画像を回転するか
        public bool isRotatePlImage{ get; set; }

        // 画像を結合して一枚の画像にする場合の画面分割数
        //public int NumberOfSplitScreenPL { get; set; }
        //public int NumberOfSplitScreenLS { get; set; }

        //縦長画像用の分割数
        public int NumberOfSplitScreenVforPlImage { get; set; }//垂直分割数
        public int NumberOfSplitScreenHforPlImage{ get; set; }
        //横長画像用の分割数
        public int NumberOfSplitScreenVforLsImage { get; set; }
        public int NumberOfSplitScreenHforLsImage { get; set; }

        public bool isPicSizeDraw { get; set; }

        public double allowPer { get; set; }

        public DateTime? Since { get; set; }

        public Config(string inputpath, string outputPath, Mode mode)
        {
            Inputpath = inputpath;
            OutputPath = outputPath;
            Mode = mode;

            TargetScreenSize = new Size(1200, 1920);//10:16=5:8
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
            NumberOfSplitScreenHforLsImage = 3;
            //NumberOfSplitScreenPL = 2 * 2;
            //NumberOfSplitScreenLS = 1 * 3;

#if DEBUG
            isPicSizeDraw = true;
#else
            isPicSizeDraw = true;
#endif
            allowPer = 0.9;

            Since = null;
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
            return isRotatePlImage && (imgSize.Width > imgSize.Height) && (imgSize.Width > TargetScreenSize.Width);
        }
    }
}
