using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
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

        private string AppendPostfixToFilename(string origName, string appdStr)
        {
            var dirname = Path.GetDirectoryName(origName);
            var fn = Path.GetFileNameWithoutExtension(origName);
            var ext = Path.GetExtension(origName);

            return Path.Combine(dirname, fn + appdStr + ext);
        }

        private string GetTitle(string path)
        {
            var dirname = Path.GetDirectoryName(path);
            var fn = Path.GetFileNameWithoutExtension(path);

            return Path.Combine(Path.GetFileName(dirname), fn);
        }

#if FALSE
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
                var zipname = AppendPostfixToFilename(_config.OutputPath, "-alt");
                using (var zipToOpen = new FileStream(zipname, filemode))
                {
                    using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                    {
                        OutputSub(piclist, archive);
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
#endif

        // --------------------------------------------------------------------
        // 
        // --------------------------------------------------------------------
        public void OutputCombine(PicInfoList piclist)
        {
            //var zipname = AppendPostfixToFilename(_config.OutputPath, "[" + piclist.PicInfos.Count.ToString() + "]");
            var zipname = _config.OutputPath;

#if DEBUG
            FileMode filemode = FileMode.Create;//デバッグ時は上書きする（消すの面倒なので
#else
            FileMode filemode = FileMode.CreateNew;
#endif
            using (var zipToOpen = new FileStream(zipname, filemode))
            {
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    if (_config.isAppendIdxCover && piclist.PicInfos.Count >= 9)
                    {
                        OutputThumbnailListToArchiveFile(piclist, archive);
                    }

                    OutputFilesToArchiveFile(piclist, archive);
                }
            }
        }

        private void OutputThumbnailListToArchiveFile(PicInfoList piclist, ZipArchive archive)
        {
            SplitScreenNumber splitNo;
            splitNo.col = Math.Min(10, (int)Math.Sqrt(piclist.PicInfos.Count));
            splitNo.row = Math.Min(10, (int)Math.Sqrt(piclist.PicInfos.Count));
            byte[] bs = GetCombineImage(piclist.PicInfos, splitNo, true);

            AddZipEntry(archive, Config.IDX_IMAGE_ENTRY_NAME, bs);
        }

        private void OutputFilesToArchiveFile(PicInfoList piclist, ZipArchive archive)
        {
            var plPicInfos = new List<PicInfo>();//縦portlait
            var lsPicInfos = new List<PicInfo>();//横landscape

            int cnt = 0;
            string entryname;

            int heightSum = 0;

            foreach (var p in piclist.PicInfos)
            {
                if (p.PicSize.Width <= p.PicSize.Height)
                {   // 縦長

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
                            OutCombineImagePL(plPicInfos, archive, entryname);
                        }
                    }
                }
                else
                {   // 横長

                    if (p.IsOutputAlone(_config.TargetScreenSize, true))
                    {  // 一枚絵として出力
                        MakeImageAndAddZipEntry(ref cnt, p, archive);
                    }
                    else
                    {
                        if (heightSum + p.PicSize.Height > _config.TargetScreenSize.Height)
                        {
                            entryname = MakeEntryName(ref cnt, lsPicInfos, "yoko");
                            OutCombineImageLS(lsPicInfos, archive, entryname);
                            heightSum = 0;
                        }

                        lsPicInfos.Add(p);
                        heightSum += p.PicSize.Height;
                        if (lsPicInfos.Count >= _config.NumberOfSplitScreenVforLsImage * _config.NumberOfSplitScreenHforLsImage)
                        {
                            entryname = MakeEntryName(ref cnt, lsPicInfos, "yoko");
                            OutCombineImageLS(lsPicInfos, archive, entryname);
                            heightSum = 0;
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
                OutCombineImagePL(plPicInfos, archive, entryname);
            }

            if (lsPicInfos.Count == 1)
            {
                MakeImageAndAddZipEntry(ref cnt, lsPicInfos[0], archive);
            }
            else if (lsPicInfos.Count >= 2)
            {
                entryname = MakeEntryName(ref cnt, lsPicInfos, "yoko");
                OutCombineImageLS(lsPicInfos, archive, entryname);
            }

            if (_config.IsForce2P && cnt <= 1)
            {
                AddEmptyImage(archive);
            }
        }

        private string MakeEntryName(ref int cnt, List<PicInfo> picInfos, string postAppd)
        {
            cnt++;
            string name = cnt.ToString("D3") + " [" + picInfos.Count.ToString() + "]" + postAppd + ".jpg";

            return name;
        }

        private void OutCombineImagePL(List<PicInfo> picInfos, ZipArchive archive, string entryname)
        {
            int splitNoV = Math.Min(_config.NumberOfSplitScreenVforPlImage, picInfos.Count);
            int splitNoH = Math.Min(_config.NumberOfSplitScreenHforPlImage, picInfos.Count);
            SplitScreenNumber splitNo;
            splitNo.col = splitNoV;
            splitNo.row = splitNoH;

            byte[] bs = GetCombineImage(picInfos, splitNo);

            AddZipEntry(archive, entryname, bs);

            picInfos.Clear();
        }

        private void OutCombineImageLS(List<PicInfo> picInfos, ZipArchive archive, string entryname)
        {
            int splitNoV = Math.Min(_config.NumberOfSplitScreenVforLsImage, picInfos.Count);
            int splitNoH = Math.Min(_config.NumberOfSplitScreenHforLsImage, picInfos.Count);
            SplitScreenNumber splitNo;
            splitNo.col = splitNoV;
            splitNo.row = splitNoH;

            byte[] bs = GetCombineImage(picInfos, splitNo);

            AddZipEntry(archive, entryname, bs);

            picInfos.Clear();
        }

        private void MakeImageAndAddZipEntry(ref int cnt, PicInfo p, ZipArchive archive)
        {
            if (p.IsDone)
            {
                return;
            }

            MakeImageAndAddZipEntrySub(ref cnt, p, archive, true);
            if (p.IsRotated)
            {   // 回転しない画像も出力する
                MakeImageAndAddZipEntrySub(ref cnt, p, archive, false);
            }

            // 細長い画像を分割して保存する
            if (_config.isSplitLongImage)
            {
                if (p.PicSize.Width > p.PicSize.Height)
                {   // 横長
                    if (p.PicSize.Width > _config.TargetScreenSize.Height && p.IsLongImage())
                    {   // 細長いので3分割
                        for (var splitIdx = 1; splitIdx <= 3; splitIdx++)
                        {
                            GetSplitedImageBinary(ref cnt, p, archive, splitIdx);
                        }
                    }
                    else
                    {   // 中央部分だけ
                        GetSplitedImageBinary(ref cnt, p, archive, 2);
                    }
                }
                else
                {
                    if (p.PicSize.Height > _config.TargetScreenSize.Height && p.IsLongImage())
                    {
                        GetSplitedImageBinary(ref cnt, p, archive, 2);
                    }
                }
            }
        }

        private void MakeImageAndAddZipEntrySub(ref int cnt, PicInfo p, ZipArchive archive, bool rot)
        {
            cnt++;
            byte[] bs = GetImageBinary(p, rot);
            var entryname = cnt.ToString("D3") + " " + p.ZipEntryName + ".jpg";
            if (rot && p.IsRotated)
            {
                entryname = "Rot-" + entryname;
            }
            AddZipEntry(archive, entryname, bs);
            p.IsDone = true;
        }

        private void GetSplitedImageBinary(ref int cnt, PicInfo p, ZipArchive archive, int splitIdx)
        {
            var img = Image.FromFile(p.Path);

            var bs = TrimImage(p, img, splitIdx);

            cnt++;
            var entryname = cnt.ToString("D3") + "-" + splitIdx.ToString("D1") + " " + p.ZipEntryNameOrig + ".jpg";
            AddZipEntry(archive, entryname, bs);
        }


        private byte[] TrimImage(PicInfo p, Image img, int splitIdx)
        {
            var ms = new MemoryStream();
            Bitmap bmpCanvas;

            var wRatio = (float)_config.TargetScreenSize.Width / (float)img.Width;
            var hRatio = (float)_config.TargetScreenSize.Height / (float)img.Height;

            float ratio;
            if (img.Width > img.Height)
            {   // 横長
#if false
                ratio = hRatio;
#else
                ratio = Math.Min(1.0f, hRatio);
#endif
                var canvasWidth = _config.TargetScreenSize.Width;
                var canvasHeight = _config.TargetScreenSize.Height;

                // dst
                var w = Math.Min((int)(img.Width * ratio), _config.TargetScreenSize.Width);
                var h = Math.Min((int)(img.Height * ratio), _config.TargetScreenSize.Height);
                var x = (canvasWidth - w) / 2;
                var y = (canvasHeight - h) / 2;
                var dstRect = new Rectangle(x, y, w, h);
                Console.Error.WriteLine("[LOG] {0}", dstRect);

                // src
                //var srcW = Math.Min(canvasWidth, (int)(img.Width * ratio));
                //var srcH = Math.Min(canvasHeight, (int)(img.Height * ratio));
                var srcW = (int)(w * ratio);
                var srcH = (int)(h * ratio);
                var srcX = 0;
                var srcY = 0;
                switch (splitIdx)
                {
                    case 1://right
                        srcX = img.Width - srcW;
                        break;
                    case 3://left
                        break;
                    case 2://center
                    default:
                        srcX = (img.Width - srcW) / 2;
                        break;
                }
                var srcRect = new Rectangle(srcX, srcY, srcW, srcH);
                Console.Error.WriteLine("[LOG] srcRect={0}", srcRect);

                bmpCanvas = DrawImage(p, canvasWidth, canvasHeight, img, dstRect, srcRect, ratio);
            }
            else
            {   // 縦長
                ratio = Math.Min(1.0f, wRatio);
                var canvasWidth = _config.TargetScreenSize.Width;
                var canvasHeight = _config.TargetScreenSize.Height;

                // dst
                var w = Math.Min((int)(img.Width * ratio), _config.TargetScreenSize.Width);
                var h = Math.Min((int)(img.Height * ratio), _config.TargetScreenSize.Height);
                var x = (canvasWidth - w) / 2;
                var y = (canvasHeight - h) / 2;
                var dstRect = new Rectangle(x, y, w, h);
                Console.Error.WriteLine("[LOG] dstRect={0}", dstRect);

                // src
                //var srcW = Math.Min(canvasWidth, (int)(img.Width * ratio));
                //var srcH = Math.Min(canvasHeight, (int)(img.Height * ratio));
                var srcW = (int)(w / ratio);
                var srcH = (int)(h / ratio);
                var srcX = 0;
                var srcY = 0;
                switch (splitIdx)
                {
                    case 1://top
                        srcY = img.Height - srcH;
                        break;
                    case 3://bottom
                        break;
                    case 2://center
                    default:
                        srcY = (img.Height - srcH) / 2;
                        break;
                }
                var srcRect = new Rectangle(srcX, srcY, srcW, srcH);
                Console.Error.WriteLine("[LOG] {0}", srcRect);

                bmpCanvas = DrawImage(p, canvasWidth, canvasHeight, img, dstRect, srcRect, ratio);
            }

            bmpCanvas.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

            return ms.ToArray();
        }

        private byte[] GetImageBinary(PicInfo p, bool rot)
        {
            var img = Image.FromFile(p.Path);

            if (rot && _config.RotatePredicate(img.Size))
            {
                //横長かつターゲット画面サイズ以上の場合は左90度回転させる
                img.RotateFlip(RotateFlipType.Rotate270FlipNone);
                p.ZipEntryName += "-rot270";
                p.IsRotated = true;
            }

            var bs = ResizeImageIfNecessary(p, img);
            return bs;
        }

        private void AddEmptyImage(ZipArchive archive)
        {
            var ms = new MemoryStream();

            var canvasWidth = _config.TargetScreenSize.Width;
            var canvasHeight = _config.TargetScreenSize.Height;
            var bmpCanvas = new Bitmap(canvasWidth, canvasHeight);
            Graphics g = Graphics.FromImage(bmpCanvas);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            var brush = Brushes.LightGray;
            g.FillRectangle(brush, 0, 0, canvasWidth, canvasHeight);
            g.Dispose();

            bmpCanvas.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

            byte[] bs = ms.ToArray();
            AddZipEntry(archive, Config.GRAY_IMAGE_ENTRY_NAME, bs);
        }


        private byte[] GetCombineImage(List<PicInfo> picInfos, SplitScreenNumber splitNo, bool thum = false)
        {
            var canvasWidth = _config.TargetScreenSize.Width;
            var canvasHeight = _config.TargetScreenSize.Height;
            var bmpCanvas = new Bitmap(canvasWidth, canvasHeight);
            Graphics g = Graphics.FromImage(bmpCanvas);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            var brush = Brushes.LightGray;
            g.FillRectangle(brush, 0, 0, canvasWidth, canvasHeight);

            var splitNoV = splitNo.col;
            var splitNoH = splitNo.row;
            var quotaWidth = canvasWidth / splitNoV;
            var quotaHeight = canvasHeight / splitNoH;

            var x = 0;
            var y = 0;
            foreach (var p in picInfos)
            {
                var img = Image.FromFile(p.Path);

                if (thum)
                {
                    if (img.Width > img.Height)
                    {
                        img.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    }
                    var wRatio = (float)quotaWidth / (float)img.Width;
                    var hRatio = (float)quotaHeight / (float)img.Height;
                    var ratio = Math.Max(wRatio, hRatio);

                    // dst
                    var w = quotaWidth;
                    var h = quotaHeight;
                    if (canvasWidth - x < w)
                    {
                        y += quotaHeight;
                        x = 0;
                    }

                    var dstRect = new Rectangle(x, y, w, h);

                    // src
                    var srcW = (int)(dstRect.Width / ratio);
                    var srcH = (int)(dstRect.Height / ratio);
                    var srcX = (img.Width - srcW) /2;
                    var srcY = (img.Height - srcH) / 2;
                    var srcRect = new Rectangle(srcX, srcY, srcW, srcH);

                    g.DrawImage(img, dstRect, srcRect, GraphicsUnit.Pixel);
                }
                else
                {
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
                }
                x += quotaWidth;
            }
            g.Dispose();

            var ms = new MemoryStream();
            bmpCanvas.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

            byte[] bs = ms.ToArray();
            return bs;
        }

        private byte[] ResizeImageIfNecessary(PicInfo p, Image img)
        {
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
            var ms = new MemoryStream();
            bmpCanvas.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            if (ratio < 1)
            {
                p.ZipEntryName += "-shrink";
            }
            p.ZipEntryName += string.Format("({0}x{1})", canvasWidth, canvasHeight);

            return ms.ToArray();
        }

        private Bitmap DrawImage(PicInfo p, int canvasWidth, int canvasHeight, Image img, int x, int y, int w, int h, float ratio)
        {
#if false
            var bmpCanvas = new Bitmap(canvasWidth, canvasHeight);

            Graphics g = Graphics.FromImage(bmpCanvas);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            var brush = Brushes.LightGray;
            g.FillRectangle(brush, 0, 0, canvasWidth, canvasHeight);

            //g.DrawImage(img, x, y, w, h);
            var dstRect = new Rectangle(x, y, w, h);
            var srcRect = new Rectangle(0, 0, img.Width, img.Height);
            //var bmpCanvas = DrawImage(p, img, dstRect, srcRect, ratio);
            g.DrawImage(img, dstRect, srcRect, GraphicsUnit.Pixel);

            DrawPicInfo(p, g, img, w, h, x, y, ratio);

            g.Dispose();

            return bmpCanvas;
#endif
            var dstRect = new Rectangle(x, y, w, h);
            var srcRect = new Rectangle(0, 0, img.Width, img.Height);
            return DrawImage(p, canvasWidth, canvasHeight, img, dstRect, srcRect, ratio);
        }

        private Bitmap DrawImage(PicInfo p, int canvasWidth, int canvasHeight, Image img, Rectangle dstRect, Rectangle srcRect, float ratio)
        {
            // draw
            var bmpCanvas = new Bitmap(canvasWidth, canvasHeight);

            Graphics g = Graphics.FromImage(bmpCanvas);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            var brush = Brushes.LightGray;
            g.FillRectangle(brush, 0, 0, canvasWidth, canvasHeight);

            g.DrawImage(img, dstRect, srcRect, GraphicsUnit.Pixel);

            DrawPicInfo(p, g, img, dstRect.Width, dstRect.Height, dstRect.X, dstRect.Y, ratio);

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
                g.DrawString(string.Format("{0,3}:{1}", p.Number, GetTitle(p.Path)), fnt, fcolor, x, drawY);
                drawY += fsize;
                g.DrawString(string.Format("{0,4}x{1,4}", img.Width, img.Height), fnt, fcolor, x, drawY);
                drawY += fsize;
                g.DrawString(string.Format("{0,4}x{1,4}({2}%)", w, h, (int)(ratio * 100)), fnt, fcolor, x, drawY);
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
