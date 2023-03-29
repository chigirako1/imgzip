using MyZipper.src;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MyZipper
{
    internal struct SplitScreenNumber : IComparable
    {
        public int col;
        public int row;

        public int CompareTo(object obj)
        {
            if (obj == null) throw new ArgumentNullException();
            if (!(obj is SplitScreenNumber other)) throw new ArgumentException();

            //var other = (SplitScreenNumber)obj;

            if (this.col < other.col)
            {
                return -1;
            }
            else if (this.col == other.col)
            {
                if ((this.row < other.row))
                {
                    return -1;
                }
                else if((this.row == other.row))
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            else
            {
                return 1;
            }
        }

        public override string ToString()
        {
            return string.Format("<{0}:{1}>", col, row);
        }
    }

    internal class PicInfo
    {
        private Config _config;

        public int Number { get; }

        public string Path { get; }
        public long FileSize { get; }
        public Size PicSize { get; }
        public bool IsDone { get; set; }
        public string ZipEntryName { get; set; }
        public string ZipEntryNameOrig { get; set; }
        public bool IsRotated{ get; set; }


        public PicInfo(int no, FileInfo fi, Config config)
        {
            Number = no;
            _config = config;

            
            Path = fi.FullName;
            FileSize = fi.Length;

            var image = Image.FromFile(fi.FullName);
            PicSize = image.Size;
            ZipEntryNameOrig = string.Format("{0}({1}x{2}){3}", System.IO.Path.GetFileNameWithoutExtension(Path), PicSize.Width, PicSize.Height, GetAspectRatioStr());
            ZipEntryName = ZipEntryNameOrig;
        }

        public float GetAspectRatio()
        {
            float ratio;

            if (IsRotated)
            {
                ratio = (float)PicSize.Height / (float)PicSize.Width;
            }
            else
            {
                ratio = (float)PicSize.Width / (float)PicSize.Height;
            }

            return ratio;
        }

        public string GetAspectRatioStr()
        {
            string r = "";
            if (PicSize.Width > PicSize.Height)
            {
                r = string.Format("[16-{0}]", (int)Math.Truncate(16 / GetAspectRatio()));
            }
            else
            {
                r = string.Format("[{0}-16]", (int)Math.Truncate(GetAspectRatio() * 16));
            }
            return r;
        }

        public bool IsLongImage()
        {
            float ratio = GetAspectRatio();
            float screenRatioPL = (float)_config.TargetScreenSize.Width / (float)_config.TargetScreenSize.Height;
            float screenRatioLS = (float)_config.TargetScreenSize.Height / (float)_config.TargetScreenSize.Width;
            if (ratio < screenRatioPL * 0.9 || ratio > screenRatioLS)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public SplitScreenNumber GetSplitScreenInfo()
        {
            double per = _config.allowPer;
            SplitScreenNumber sn;

            if (_config.isRotatePlImage &&
                _config.TargetScreenSize.Height > _config.TargetScreenSize.Width &&
                PicSize.Width < PicSize.Height
                )
            {
                sn.col = Math.Max(_config.TargetScreenSize.Width / (int)(PicSize.Width * per), 1);
                sn.row = Math.Max(_config.TargetScreenSize.Height / (int)(PicSize.Height * per), 1);
            }
            else
            {
                sn.row = Math.Max(_config.TargetScreenSize.Width / (int)(PicSize.Width * per), 1);
                sn.col = Math.Max(_config.TargetScreenSize.Height / (int)(PicSize.Height * per), 1);
            }

            return sn;
        }

        public bool IsInRange(Size screenSize)
        {

            return PicSize.Width <= screenSize.Width && PicSize.Height <= screenSize.Height;
        }

        public bool IsOutputAlone(Size screenSize, bool swapVH = false)
        {
            int w = PicSize.Width;
            int h = PicSize.Height;
            if (swapVH)
            {//横長
                if (h > screenSize.Height / 2 || w >= screenSize.Width)
                {
                    return true;
                }
                (w, h) = (h, w);
                bool hz = w >= screenSize.Width;
                bool vt = h >= screenSize.Height;
                return hz || vt;
            }
            else
            {
                /*var allowPer = _config.allowPer;
                bool hz = w  >= (screenSize.Width * allowPer);
                bool vt = h >= (screenSize.Height * allowPer);*/
                bool hz = w * _config.allowPer > (screenSize.Width / 2) ;
                bool vt = h * _config.allowPer > (screenSize.Height / 2);
                return hz || vt;
            }
        }

        public void PrintInfo(int idx)
        {
            string r = GetAspectRatioStr();
            var si = GetSplitScreenInfo();
            Console.Error.WriteLine("{0,3}:'{1}'({2,4}x{3,4}),\tratio={4:f4},\t{5}\t{6}",
                idx, 
                Path, 
                PicSize.Width, 
                PicSize.Height, 
                GetAspectRatio(),
                r,
                si
                );
        }
    }

    internal class PicInfoList
    {
        public List<PicInfo> PicInfos { get; }
        private Config _config;


        public int MinWidth { get; }
        public int MinHeight { get; }
        public int MaxWidth { get; }
        public int MaxHeight { get; }

        public PicInfoList(string path, Config config)
        {
            PicInfos = new List<PicInfo>();
            _config = config;

            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).OrderBy(f => f)
                .Where(s =>
                    s.EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase)
                    ||
                    s.EndsWith(".jpeg", StringComparison.CurrentCultureIgnoreCase)
                    ||
                    s.EndsWith(".png", StringComparison.CurrentCultureIgnoreCase)
                    ||
                    s.EndsWith(".gif", StringComparison.CurrentCultureIgnoreCase)
                    );

            var filelist = new List<string>(files);
            filelist.Sort(new NaturalStringComparer());

            MinWidth = Int32.MaxValue;
            MinHeight = Int32.MaxValue;

            var dic = new Dictionary<SplitScreenNumber, int>();

            foreach (var f in filelist.Select((it, idx) => (it, idx)))
            {
                var fi = new FileInfo(f.it);

                if (_config.Since != null)
                {
                    if (_config.Since > fi.CreationTime)
                    {
                        Console.WriteLine("生成日時={0}:処理対象外のファイルです。\"{1}\"", fi.CreationTime, fi.FullName);
                        continue;
                    }
                }

                var pi = new PicInfo(f.idx + 1, fi, _config);
                pi.PrintInfo(f.idx + 1);
                PicInfos.Add(pi);

                MinWidth = Math.Min(pi.PicSize.Width, MinWidth);
                MinHeight = Math.Min(pi.PicSize.Height, MinHeight);

                //max
                MaxWidth = Math.Max(pi.PicSize.Width, MaxWidth);
                MaxHeight = Math.Max(pi.PicSize.Height, MaxHeight);

                var r = pi.GetSplitScreenInfo();
                // var r = pi.GetAspectRatio();

                if (!dic.ContainsKey(r))
                {
                    dic.Add(r, 0);
                }
                dic[r]++;
            }
            Console.Error.WriteLine("-------------------");
            Console.Error.WriteLine("WxH:[{0,4}-{1,4}]x[{2,4}-{3,4}]", MinWidth, MaxWidth, MinHeight, MaxHeight);
            foreach (var kvp in dic.OrderBy(c => c.Key))
            {
                Console.Error.WriteLine("{0}:{1}", kvp.Key, kvp.Value);
            }
            Console.Error.WriteLine("-------------------");
            _config = config;
        }
    }
}
