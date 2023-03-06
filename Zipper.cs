using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
//using static System.Net.Mime.MediaTypeNames;

namespace MyZipper
{
    internal class Zipper
    {
        private Config _config;

        public Zipper(Config config)
        {
            _config = config;
        }


        public void Output(PicInfoList piclist)
        {
#if DEBUG
            FileMode filemode = FileMode.Create;//デバッグ時は上書きする（消すの面倒なので
#else
            FileMode filemode = FileMode.CreateNew;
#endif
            bool bExistUndone = false;

            using (var zipToOpen = new FileStream(_config.OutputPath, filemode))
            {
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    bExistUndone = OutputSub(piclist, archive);
                }
            }

            if (bExistUndone)
            {
                var dirname = Path.GetDirectoryName(_config.OutputPath);
                var fn = Path.GetFileNameWithoutExtension(_config.OutputPath);
                var ext = Path.GetExtension(_config.OutputPath);

                var zipname = Path.Combine(dirname, fn + "-alt" + ext);

                using (var zipToOpen = new FileStream(zipname, filemode))
                {
                    using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                    {
                        OutputSubAlt(piclist, archive);
                    }
                }

            }
        }

        private bool OutputSub(PicInfoList piclist, ZipArchive archive)
        {
            bool bExistUndone = false;
            int cnt = 0;
            foreach (var p in piclist.PicInfos)
            {

                var bDo = true;

                if (bDo)
                {
                    MakeImageAndAddZipEntry(ref cnt, p, archive);
                }
                else
                {
                    p.IsDone = false;
                    bExistUndone = true;
                }
            }
            return bExistUndone;
        }


        // --------------------------------------------------------------------
        // 
        // --------------------------------------------------------------------
        public void OutputJoin(PicInfoList piclist)
        {
#if DEBUG
            FileMode filemode = FileMode.Create;//デバッグ時は上書きする（消すの面倒なので
#else
            FileMode filemode = FileMode.CreateNew;
#endif
            using (var zipToOpen = new FileStream(_config.OutputPath, filemode))
            {
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    OutputJoinSub(piclist, archive);
                }
            }
        }

        private void OutputJoinSub(PicInfoList piclist, ZipArchive archive)
        {
            // 縦長画像は2x2 or 4x4 or 9x9とか？
            // 　→少しはみ出る場合は縮小？トリミング？
            //
            // 横長画像は1x3 or 1x4で原則出力する？

            var plPicInfos = new List<PicInfo>();//縦portlait
            var lsPicInfos = new List<PicInfo>();//横landscape

            int cnt = 0;
            string entryname;

            foreach (var p in piclist.PicInfos)
            {
                if (p.PicSize.Width <= p.PicSize.Height)
                {
                    // 縦長


                    if (p.IsOutputAlone(_config.TargetScreenSize))
                    {   // 一枚絵として出力
                        MakeImageAndAddZipEntry(ref cnt, p, archive);
                    }
                    else
                    {
                        plPicInfos.Add(p);
                        if (plPicInfos.Count >= _config.NumberOfSplitScreenVforPlImage * _config.NumberOfSplitScreenHforPlImage)
                        {
                            entryname = MakeEntryName(ref cnt, plPicInfos, "tate");
                            OutCombineImage(plPicInfos, archive, entryname);
                        }
                    }
                }
                else
                {
                    // 横長
                    if (p.IsOutputAlone(_config.TargetScreenSize, true))
                    {  // 一枚絵として出力
                        MakeImageAndAddZipEntry(ref cnt, p, archive);
                    }
                    else
                    {
                        lsPicInfos.Add(p);
                        if (lsPicInfos.Count >= _config.NumberOfSplitScreenVforLsImage * _config.NumberOfSplitScreenHforLsImage)
                        {
                            entryname = MakeEntryName(ref cnt, lsPicInfos, "yoko");
                            OutCombineImage(lsPicInfos, archive, entryname);
                        }

                    }
                }
            }

            if (plPicInfos.Count == 1)
            {
                MakeImageAndAddZipEntry(ref cnt, plPicInfos[0], archive);
            }
            else if (plPicInfos.Count >= 2)
            {
                entryname = MakeEntryName(ref cnt, plPicInfos, "tate");
                OutCombineImage(plPicInfos, archive, entryname);
            }

            if (lsPicInfos.Count == 1)
            {
                MakeImageAndAddZipEntry(ref cnt, lsPicInfos[0], archive);
            }
            else if (lsPicInfos.Count >= 2)
            {
                entryname = MakeEntryName(ref cnt, lsPicInfos, "yoko");
                OutCombineImage(lsPicInfos, archive, entryname);
            }
        }

        private string MakeEntryName(ref int cnt, List<PicInfo> picInfos, string postAppd)
        {
            cnt++;
            string name = cnt.ToString("D3") + "[" + picInfos.Count.ToString() + "]" + postAppd + ".jpg";

            return name;
        }

        private void OutCombineImage(List<PicInfo> picInfos, ZipArchive archive, string entryname)
        {
            byte[] bs = GetCombineImage(picInfos);

            AddZipEntry(archive, entryname, bs);

            picInfos.Clear();
        }

        private void OutputSubAlt(PicInfoList piclist, ZipArchive archive)
        {
            int cnt = 0;
            foreach (var p in piclist.PicInfos)
            {
                if (p.IsDone)
                {
                    continue;
                }
                MakeImageAndAddZipEntry(ref cnt, p, archive);
            }
        }

        private void MakeImageAndAddZipEntry(ref int cnt, PicInfo p, ZipArchive archive)
        {
            cnt++;
            byte[] bs = GetImageBinary(p);
            var entryname = cnt.ToString("D3") + " " + p.ZipEntryName + ".jpg";
            if (p.IsRotated)
            {
                entryname = "Rot-" + entryname;
            }
            AddZipEntry(archive, entryname, bs);
            p.IsDone = true;
        }

        private byte[] GetImageBinary(PicInfo p)
        {
            var img = Image.FromFile(p.Path);

            if (_config.RotatePredicate(img.Size))
            {
                //横長かつターゲット画面サイズ以上の場合は左90度回転させる
                img.RotateFlip(RotateFlipType.Rotate270FlipNone);
                p.ZipEntryName += "-rot270";
                p.IsRotated = true;
            }

            var ms = ResizeImageIfNecessary(p, img);
            byte[] bs = ms.ToArray();
            return bs;
        }

        private byte[] GetCombineImage(List<PicInfo> picInfos)
        {
            var ms = new MemoryStream();

            var canvasWidth = _config.TargetScreenSize.Width;
            var canvasHeight = _config.TargetScreenSize.Height;
            var bmpCanvas = new Bitmap(canvasWidth, canvasHeight);
            Graphics g = Graphics.FromImage(bmpCanvas);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            var x = 0;
            var y = 0;
            foreach (var p in picInfos)
            {
                var img = Image.FromFile(p.Path);

                bool isPortlait = img.Height >= img.Width;
                int quotaWidth;
                int quotaHeight;
                if (isPortlait)
                {
                    int splitNoV = Math.Min(_config.NumberOfSplitScreenVforPlImage, picInfos.Count);
                    int splitNoH = Math.Min(_config.NumberOfSplitScreenHforPlImage, picInfos.Count);
                    quotaWidth = canvasWidth / splitNoV;
                    quotaHeight = canvasHeight / splitNoH;
                }
                else
                {
                    int splitNoV = Math.Min(_config.NumberOfSplitScreenVforLsImage, picInfos.Count);
                    int splitNoH = Math.Min(_config.NumberOfSplitScreenHforLsImage, picInfos.Count);
                    quotaWidth = canvasWidth / splitNoV;
                    quotaHeight = canvasHeight / splitNoH;
                }

                var wRatio = (float)quotaWidth / (float)img.Width;
                var hRatio = (float)quotaHeight / (float)img.Height;
                var ratio = Math.Min(wRatio, hRatio);

                var w = (int)(img.Width * ratio);
                var h = (int)(img.Height * ratio);
                if (canvasWidth - x < w)
                {
                    y += quotaHeight;
                    x = 0;
                }
                var yohakuW = (quotaWidth - w) / 2;
                var yohakuH = (quotaHeight - h) / 2;


                g.DrawImage(img, x + yohakuW, y + yohakuH, w, h);
                DrawPicInfo(p, g, img, w, h, x, y, ratio);
                x += quotaWidth;
            }
            g.Dispose();

            bmpCanvas.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

            byte[] bs = ms.ToArray();
            return bs;
        }

        private MemoryStream ResizeImageIfNecessary(PicInfo p, Image img)
        {
            var ms = new MemoryStream();

            //p.ZipEntryName += "-yohaku";

            var wRatio = (float)_config.TargetScreenSize.Width / (float)img.Width;
            var hRatio = (float)_config.TargetScreenSize.Height / (float)img.Height;
            var ratio = Math.Min(wRatio, hRatio);

            var w = img.Width;
            var h = img.Height;
            if (ratio < 1)
            {
                w = (int)(w * ratio);
                h = (int)(h * ratio);
            }
            else
            {
                ratio = 1;
            }
            var x = 0;
            var y = 0;

            var canvasWidth = w;
            var canvasHeight = h;
            float scrnAsp = _config.GetCanvasScreenRatio();
            float picRatio = (float)w / (float)h;
            if (scrnAsp < picRatio)
            {
                canvasHeight = (int)(w / scrnAsp);
                y = (canvasHeight - h) / 2;
            }
            else if (scrnAsp > picRatio)
            {
                canvasWidth = (int)(h * scrnAsp);
                x = (canvasWidth - w) / 2;
            }
            else
            {
                p.ZipEntryName += "-asis";
            }

            Bitmap bmpCanvas = DrawImage(p, canvasWidth, canvasHeight, img, x, y, w, h, ratio);
            bmpCanvas.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            if (ratio < 1)
            {
                p.ZipEntryName += "-shrink";
            }
            p.ZipEntryName += string.Format("({0}x{1})", canvasWidth, canvasHeight);

            return ms;
        }

        private Bitmap DrawImage(PicInfo p, int canvasWidth, int canvasHeight, Image img, int x, int y, int w, int h, float ratio)
        {
            var bmpCanvas = new Bitmap(canvasWidth, canvasHeight);
            Graphics g = Graphics.FromImage(bmpCanvas);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(img, x, y, w, h);
            DrawPicInfo(p, g, img, w, h, x, y, ratio);
            g.Dispose();

            return bmpCanvas;
        }

        private void DrawPicInfo(PicInfo p, Graphics g, Image img, int w, int h, int x, int y, float ratio)
        {
            if (_config.isPicSizeDraw)
            {
                var fsize = 20;
                var fcolor = Brushes.Cyan;
                var fnt = new Font("MS UI Gothic", fsize);
                var drawY = y;
                g.DrawString(string.Format("{0,4}x{1,4}", img.Width, img.Height), fnt, fcolor, x, drawY);
                drawY += fsize;
                g.DrawString(string.Format("{0,4}x{1,4}({2}%)", w, h, (int)(ratio * 100)), fnt, fcolor, x, drawY);
                drawY += fsize;
                g.DrawString(string.Format("{0,3}", p.Number), fnt, fcolor, x, drawY);
            }
        }

        private void AddZipEntry(ZipArchive archive, string entryName, byte[] bs)
        {
            var readmeEntry = archive.CreateEntry(entryName);
            using (var writer = new BinaryWriter(readmeEntry.Open()))
            {
                writer.Write(bs, 0, bs.Length);
                Console.Error.WriteLine(entryName);
            }
        }
    }
}
