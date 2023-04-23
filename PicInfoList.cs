using MyZipper.src;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Media.Imaging;
using static System.Net.WebRequestMethods;

namespace MyZipper
{
    internal struct SplitScreenNumber : IComparable
    {
        public int Col;
        public int Row;

        public int CompareTo(object obj)
        {
            if (obj == null) throw new ArgumentNullException();
            if (!(obj is SplitScreenNumber other)) throw new ArgumentException();

            if (Col < other.Col)
            {
                return -1;
            }
            else if (Col == other.Col)
            {
                if (Row < other.Row)
                {
                    return -1;
                }
                else if(Row == other.Row)
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
            return string.Format("<{0}:{1}>", Col, Row);
        }
    }

    internal class PicInfo
    {
        private Config _config;
        private bool IsZipEntry;

        public int Number { get; private set; }

        public string Path { get; private set; }
        public long FileSize { get; private set; }
        public Size PicSize { get; private set; }
        public bool IsDone { get; set; }
        public string ZipEntryName { get; set; }
        public string ZipEntryNameOrig { get; set; }
        public bool IsRotated{ get; set; }

        public PicInfo(int no, FileInfo fi, Config config)
        {
            var image = Image.FromFile(fi.FullName);
            PicSize = image.Size;
            constructor(no, fi.FullName, fi.Length, config);
        }

        public PicInfo(int no, ZipArchiveEntry ent, Config config)
        {
            IsZipEntry = true;
            var image = Image.FromStream(ent.Open());
            PicSize = image.Size;
            constructor(no, ent.FullName, ent.Length, config);
        }

        private void constructor(int no, string fullname, long length, Config config)
        {
            Number = no;
            _config = config;

            Path = fullname;
            FileSize = length;

            ZipEntryNameOrig = string.Format("{0}({1}x{2}){3}", Util.GetEntryName(Path), PicSize.Width, PicSize.Height, GetAspectRatioStr());
            ZipEntryName = ZipEntryNameOrig;
        }

        public Image GetImage()
        {
            if (IsZipEntry)
            {
                using (var archive = ZipFile.OpenRead(_config.Inputpath))
                {
                    var ent = archive.GetEntry(Path);
                    return Image.FromStream(ent.Open());
                }
            }
            else
            {
                return Image.FromFile(Path);
            }
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
                sn.Col = Math.Max(_config.TargetScreenSize.Width / (int)(PicSize.Width * per), 1);
                sn.Row = Math.Max(_config.TargetScreenSize.Height / (int)(PicSize.Height * per), 1);
            }
            else
            {
                sn.Row = Math.Max(_config.TargetScreenSize.Width / (int)(PicSize.Width * per), 1);
                sn.Col = Math.Max(_config.TargetScreenSize.Height / (int)(PicSize.Height * per), 1);
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
                bool hz = w * _config.allowPer > (screenSize.Width / 2) ;
                bool vt = h * _config.allowPer > (screenSize.Height / 2);
                return hz || vt;
            }
        }

        public void PrintInfo(int idx)
        {
            string r = GetAspectRatioStr();
            var si = GetSplitScreenInfo();
            Log.V("{0,3}:'{1}'({2,4}x{3,4}),\tratio={4:f4},\t{5}\t{6}",
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
        private Config Config;

        public int MinWidth { get; private set; }
        public int MinHeight { get; private set; }
        public int MaxWidth { get; private set; }
        public int MaxHeight { get; private set; }

        public PicInfoList(string path, Config config)
        {
            PicInfos = new List<PicInfo>();
            Config = config;

            if (System.IO.File.GetAttributes(path).HasFlag(FileAttributes.Directory))
            {
                List<String> filelist = GetFileList(path);
                SetPicInfos(filelist);
            }
            else
            {// zipとして扱う

                // 一時ディレクトリに展開して処理するのはなんかいや
                List<String> filelist = GetFileListFromZip(path);
                SetPicInfos(filelist, true);
            }
        }

        private List<String> GetFileList(string path)
        {
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).OrderBy(f => f)
                .Where(s =>
                    s.EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase) ||
                    s.EndsWith(".jpeg", StringComparison.CurrentCultureIgnoreCase) ||
                    s.EndsWith(".png", StringComparison.CurrentCultureIgnoreCase) ||
                    s.EndsWith(".gif", StringComparison.CurrentCultureIgnoreCase)
                    );

            var filelist = new List<string>(files);
            filelist.Sort(new NaturalStringComparer());

            return filelist;
        }

        private List<String> GetFileListFromZip(string path)
        {
            List<string> filelist = new List<string>();

            using (var archive = ZipFile.OpenRead(path))
            {
                foreach (var e in archive.Entries)
                {
                    //filelist.Add(e.FullName);
                    //Console.WriteLine("名前       : {0}", e.Name);
                    //Console.WriteLine("フルパス   : {0}", e.FullName);
                    //Console.WriteLine("サイズ     : {0}", e.Length);
                    //Console.WriteLine("圧縮サイズ : {0}", e.CompressedLength);
                    //Console.WriteLine("更新日時   : {0}", e.LastWriteTime);
                }

                var files = archive.Entries.OrderBy(e => e.FullName).
                    Where(e =>
                        e.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        e.Name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                        e.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        e.Name.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
                );

                files.ToList().ForEach(f => filelist.Add(f.FullName));
                filelist.Sort(new NaturalStringComparer());
            }

            return filelist;
        }


        private void SetPicInfos(List<String> filelist, bool zip = false)
        {
            MinWidth = Int32.MaxValue;
            MinHeight = Int32.MaxValue;
            var dic = new Dictionary<SplitScreenNumber, int>();

            if (zip)
            {
                SetPicInfosFromZip(filelist, dic);
            }
            else
            {
                SetPicInfosFromDir(filelist, dic);
            }
        }

        private void SetPicInfosFromDir(List<String> filelist, Dictionary<SplitScreenNumber, int> dic)
        { 
            foreach (var f in filelist.Select((it, idx) => (it, idx)))
            {
                var fi = new FileInfo(f.it);

                if (Config.Since != null)
                {
                    if (Config.Since > fi.CreationTime)
                    {
                        Console.WriteLine("生成日時={0}:処理対象外のファイルです。\"{1}\"", fi.CreationTime, fi.FullName);
                        continue;
                    }
                }
                var pi = new PicInfo(f.idx + 1, fi, Config);
                pi.PrintInfo(f.idx + 1);
                PicInfos.Add(pi);

                SetPicInfosSub(pi, dic);
            }

            SetPicInfosStat(dic);
        }

        private void SetPicInfosFromZip(List<String> filelist, Dictionary<SplitScreenNumber, int> dic)
        {
            using (var archive = ZipFile.OpenRead(Config.Inputpath))
            {
                foreach (var f in filelist.Select((it, idx) => (it, idx)))
                {
                    var ent = archive.GetEntry(f.it);

                    var pi = new PicInfo(f.idx + 1, ent, Config);
                    pi.PrintInfo(f.idx + 1);
                    PicInfos.Add(pi);

                    SetPicInfosSub(pi, dic);
                }
            }

            SetPicInfosStat(dic);
        }

        private void SetPicInfosSub(PicInfo pi, Dictionary<SplitScreenNumber, int> dic)
        {
            MinWidth = Math.Min(pi.PicSize.Width, MinWidth);
            MinHeight = Math.Min(pi.PicSize.Height, MinHeight);

            MaxWidth = Math.Max(pi.PicSize.Width, MaxWidth);
            MaxHeight = Math.Max(pi.PicSize.Height, MaxHeight);

            var r = pi.GetSplitScreenInfo();
            if (!dic.ContainsKey(r))
            {
                dic.Add(r, 0);
            }
            dic[r]++;

        }

        private void SetPicInfosStat(Dictionary<SplitScreenNumber, int> dic)
        {
            Log.V("-------------------");
            Log.V(String.Format("WxH:[{0,4}-{1,4}]x[{2,4}-{3,4}]", MinWidth, MaxWidth, MinHeight, MaxHeight));
            var min = Int32.MaxValue;
            foreach (var kvp in dic.OrderBy(c => c.Key))
            {
                var splitInf = kvp.Key;
                var cnt = kvp.Value;
                if (splitInf.Row < splitInf.Col)
                {
                    min = Math.Min(min, splitInf.Col);
                }

                Log.V("{0}:{1}", splitInf, cnt);
            }
            Config.NumberOfSplitScreenHforLsImage = min;
            Log.V("-------------------");
        }
    }
}
