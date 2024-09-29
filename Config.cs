using System;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;

namespace MyZipper
{
    //mode
    enum Mode
    {
        None,
        Auto = None,
        Twt,
        Pxv,
        PassThrough,
        WinTablet,

        MAX
    }

    //sort
    enum Sort
    {
        NONE,
        AUTO = NONE,
        TITLE,
        PXVID,

        MAX
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
        public Sort Sort { get; private set; }

        // input
        public string Inputpath { get; private set; }

        public Size TargetScreenSize { get; set; }

        // output
        public string OutputDir { get; private set; }
        public string OutputFileame { get; private set; }
        public string OutputPath { get; private set; }

        // 出力Zipを分割するファイル数（0なら分割しない)
        public int OutputFileDivedeThreshold { get; private set; }

        public bool UseOrigName { get; private set; }

        public int SplitLR { get; private set; }


        //縦長画像用の分割数
        public int PlNumberOfCol { get;  set; }//幅に対しての分割数（列
        public int PlNumberOfRow{ get; set; }//高さに対しての分割数（行
        //横長画像用の分割数
        public int LsNumberOfCol { get;  set; }
        public int LsNumberOfRow { get; set; }

        public double AllowPer { get; private set; }

        public DateTime? Since { get; set; }

        // 縦長、横長画像を別のzipファイルに出力するか
        public bool IsSeparateOutput { get; private set; }

        // 横長画像を回転するか
        public bool IsRotatePlImage { get; private set; }
        public bool IsRotateAlt{ get; private set; }
        public bool IsPicSizeDraw { get; private set; }
        public bool IsForce2P { get; private set; }
        public bool IsAppendIdxCover { get; private set; }
        public bool IsSplitLongImage { get; private set; }
        public bool NoComposite { get; private set; }
        public bool IsCrop { get; private set; }
        public bool IsShrink { get; private set; }

        public int SeparateFileNumberThreashold{ get; private set; }
        public int SeparateFileNumber { get; private set; }

        public bool LsCompositeLs { get; private set; }
        public bool Quiet { get; private set; }

        public int IdxOutThreshold { get; private set; }

        public long FileSizeSum { get; set; }//ひどすぎる

        public Config(string[] args)
        {
            if (args.Length < 3)
            {
                Log.E("引数の数が足りません。Usage: myzipper <options> <出力パス> <入力パス> [since=<YYYY/MM/DD>]");
                Environment.Exit(1);
            }

            string outPath = args[1];
            string inPath = args[2];

            Init(inPath, outPath);

            SetOptions(args[0]);
            ParseOptions(args);
        }


        private void Init(string inputpath, string outputPath)
        {
            Inputpath = inputpath;
            OutputPath = outputPath;
            UseOrigName = false;

            Mode = Mode.Auto;
            Sort = Sort.AUTO;

            TargetScreenSize = new Size(1200, 1920);//10:16=5:8
            //TargetScreenSize = new Size(1920, 1200);
            OutputFileDivedeThreshold = 0;
            PlNumberOfCol = 2;
            PlNumberOfRow = 2;
            LsNumberOfCol = 1;
            LsNumberOfRow = 2;

            //allowPer = 0.9;
            AllowPer = 1.0;

            Since = null;

            IsSeparateOutput = false;
            IsRotatePlImage = false;
            IsPicSizeDraw = false;
            IsForce2P = false;
            IsRotateAlt = false;
            NoComposite = false;
            IsCrop = false;
            LsCompositeLs = false;
            IsShrink = true;

            SeparateFileNumberThreashold = 0;
            SeparateFileNumber = 0;

            IdxOutThreshold = 4;
        }

        private void SetOptions(string options)
        {
            foreach (var c in options)
            {
                switch (c)
                {
                    case '1':
                        NoComposite = true;
                        break;
                    case '2':
                        IsForce2P = true;
                        break;
                    case 'a':
                        break;
                    case 'c':
                        IsAppendIdxCover = true;
                        break;
                    case 'C':
                        IsCrop = true;
                        break;
                    case 'i':
                        IsPicSizeDraw = true;
                        break;
                    case 'r':
                        IsRotatePlImage = true;
                        break;
                    case 'R':
                        IsRotateAlt = true;
                        break;
                    case 's':
                        IsSplitLongImage = true;
                        break;
                    case 'Q':
                        Quiet = true;
                        Log.Quiet = true;
                        break;
                    default:
                        Log.E("オプションが不正です。");
                        Environment.Exit(1);
                        break;
                }
            }
        }

        private void ParseOptions(string[] args)
        {
            if (args.Length < 4)
            {
                return;
            }

            string[] tmpargs = new string[args.Length - 3];
            Array.Copy(args, 3, tmpargs, 0, tmpargs.Length);

            foreach (var arg in tmpargs)
            {
                var opt = arg.Split('=');
                switch (opt[0])
                {
                    case "useOrigName":
                        UseOrigName = true;
                        Log.I("UseOrigName={0}", UseOrigName);
                        break;
                    case "splitLR":
                        SplitLR = int.Parse(opt[1]);
                        Log.I("SplitLR={0}", SplitLR);
                        break;
                    case "since":
                        try
                        {
                            Since = DateTime.ParseExact(opt[1], "yyyy/MM/dd", null);
                            Log.I("since={0}", Since);
                        }
                        catch (FormatException ex)
                        {
                            Log.E("オプションが不正です。{0}", ex);
                            Environment.Exit(1);
                        }
                        break;
                    case "screensize":
                        Regex r = new Regex(@"(\d+)x(\d+)");
                        Match m = r.Match(opt[1]);
                        if (m.Success)
                        {
                            var w = int.Parse(m.Groups[1].Value);
                            var h = int.Parse(m.Groups[2].Value);
                            TargetScreenSize = new Size(w, h);
                            Log.I("screen={0}", TargetScreenSize);
                        }
                        else
                        {
                            Log.E($"オプションが不正です。{opt[1]}");
                            Environment.Exit(1);
                        }
                        break;
                    case "mode":
                        if (opt[1] == "twt")
                        {
                            Mode = Mode.Twt;
                        }
                        else if (opt[1] == "pxv")
                        {
                            Mode = Mode.Pxv;
                        }
                        else if (opt[1] == "passthrough")
                        {
                            Mode = Mode.PassThrough;
                        }
                        else if (opt[1] == "WinTablet")
                        {
                            Mode = Mode.WinTablet;
                        }
                        else
                        {
                            Log.E("オプションが不正です");
                            Environment.Exit(1);
                        }
                        Log.I("mode={0}", Mode);
                        break;
                    case "sort":
                        if (opt[1] == "title")
                        {
                            Sort = Sort.TITLE;
                        }
                        Log.I("sort={0}", Sort);
                        break;
                    case "separate":
                        Regex rgx = new Regex(@"(\d+):(\d+)");
                        Match match = rgx.Match(opt[1]);
                        if (match.Success)
                        {

                            SeparateFileNumberThreashold = int.Parse(match.Groups[1].Value);
                            SeparateFileNumber = int.Parse(match.Groups[2].Value);
                            Log.I("sepa={0}:{1}", SeparateFileNumberThreashold, SeparateFileNumber);
                        }
                        else
                        {
                            Log.E($"オプションが不正です。{opt[1]}");
                            Environment.Exit(1);
                        }
                        break;
                    default:
                        Log.E($"オプションが不正です。=> {opt[0]}");
                        break;
                }
            }
        }

        public float GetCanvasScreenRatio()
        {
            return (float)TargetScreenSize.Width / (float)TargetScreenSize.Height;
        }

        public bool IsHugeDiff(float imgAsp)
        {
            float canvasAsp = GetCanvasScreenRatio();
            float delta = Math.Abs(canvasAsp - imgAsp);
            if (delta > 0.2)
            {
                return true;
            }
            return false;
        }

        public bool RotatePredicate(Size imgSize)
        {
            if (!IsRotatePlImage)
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
                return (imgSize.Width > imgSize.Height) && (imgSize.Width >= TargetScreenSize.Width);
            }
            else
            {
                return (imgSize.Width < imgSize.Height) && (imgSize.Height >= TargetScreenSize.Height);
            }
        }

        public int GetMagRatio(int w, int h)
        {
            var wRatio = TargetScreenSize.Width * 100 / w;
            var hRatio = TargetScreenSize.Height * 100 / h;
            var ratio = Math.Min(wRatio, hRatio);
            return ratio;
        }

        public string GetTwtID()
        {
            Debug.Assert(this.Mode == Mode.Twt);

            {
                return Twt.GetTwtID(Inputpath);
            }
        }

        public int GetPxvID()
        {
            Debug.Assert(this.Mode == Mode.Pxv);

            {
                return Pxv.GetPxvID(Inputpath);
            }
        }
    }
}
