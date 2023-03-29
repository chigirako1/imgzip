using MyZipper.src;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Net;
//using static System.Net.Mime.MediaTypeNames;

namespace MyZipper
{
    internal class Zipper
    {
        private Config _config;
        private CoordinateCalculator _coordinateCalculator;

        public Zipper(Config config)
        {
            _config = config;
            _coordinateCalculator = new CoordinateCalculator(_config.TargetScreenSize);
        }

        private string GetTitle(string path)
        {
            return Util.GetTitle(path);
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
                    if (_config.isAppendIdxCover && piclist.PicInfos.Count >= _config.IdxOutThreshold)
                    {   //先頭にサムネイルをまとめた画像を追加する
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
            if (splitNo.col * splitNo.row < piclist.PicInfos.Count)
            {
                splitNo.row++;
            }
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

                        if (p.PicSize.Width >= 640 && p.PicSize.Height>= 480)
                        {
                            MakeImageAndAddZipEntry(ref cnt, p, archive);
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
            if (_config.isRotateAlt && p.IsRotated)
            {   // 回転しない画像も出力する
                MakeImageAndAddZipEntrySub(ref cnt, p, archive, false);
            }

            // 細長い画像を分割して保存する
            if (_config.isSplitLongImage)
            {
                if (p.PicSize.Width > p.PicSize.Height)
                {   // 横長
#if false
                    for (var splitIdx = 1; splitIdx <= 3; splitIdx++)
                    {
                        GetSplitedImageBinary(ref cnt, p, archive, splitIdx);
                    }
#else
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
#endif
                }
                else
                {
                    //if (p.PicSize.Height * 0.9 > _config.TargetScreenSize.Height && p.IsLongImage())
                    if (p.PicSize.Height > _config.TargetScreenSize.Height)
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
            Bitmap bmpCanvas;

            if (img.Width > img.Height)
            {   // 横長
#if falsesf
                var hRatio = (float)_config.TargetScreenSize.Height / (float)img.Height;
                float ratio = Math.Min(1.0f, hRatio);
                var h = Math.Min((int)(img.Height * ratio), _config.TargetScreenSize.Height);

                float scrnAsp = _config.GetCanvasScreenRatio();
                var canvasWidth = (int)(h * scrnAsp);//_config.TargetScreenSize.Width;
                var canvasHeight = Math.Min(_config.TargetScreenSize.Height, h);
                var w = canvasWidth;//Math.Min((int)(img.Width * ratio), _config.TargetScreenSize.Width);

                // dst
                var x = (canvasWidth - w) / 2;
                var y = (canvasHeight - h) / 2;
                var dstRect = new Rectangle(x, y, w, h);
                Console.Error.WriteLine("[LOG] dstRect={0}", dstRect);

                // src
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
#endif
                //var result = _coordinateCalculator.CalcTrimLS(img.Width, img.Height, splitIdx);
                var result = _coordinateCalculator.CalcFitWidth(img.Width, img.Height);
                bmpCanvas = DrawImageAndInfo(p, result.Canvas.Width, result.Canvas.Height, img, result.DstRect, result.SrcRect, result.Ratio);
            }
            else
            {   // 縦長
#if false
                var wRatio = (float)_config.TargetScreenSize.Width / (float)img.Width;
                float ratio = Math.Min(1.0f, wRatio);
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
#endif
                var result = _coordinateCalculator.CalcTrimPL(img.Width, img.Height, splitIdx);
                bmpCanvas = DrawImageAndInfo(p, result.Canvas.Width, result.Canvas.Height, img, result.DstRect, result.SrcRect, result.Ratio);
            }

            //var ms = new MemoryStream();
            //bmpCanvas.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            //return ms.ToArray();
            return GetBmpByteStream(bmpCanvas);
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
            var canvasWidth = _config.TargetScreenSize.Width;
            var canvasHeight = _config.TargetScreenSize.Height;
            var bmpCanvas = new Bitmap(canvasWidth, canvasHeight);
            Graphics g = Graphics.FromImage(bmpCanvas);

            var brush = Brushes.LightGray;
            g.FillRectangle(brush, 0, 0, canvasWidth, canvasHeight);
            g.Dispose();

            //var ms = new MemoryStream();
            //bmpCanvas.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            //byte[] bs = ms.ToArray();
            byte[] bs = GetBmpByteStream(bmpCanvas);

            AddZipEntry(archive, Config.GRAY_IMAGE_ENTRY_NAME, bs);
        }


        private byte[] GetCombineImage(List<PicInfo> picInfos, SplitScreenNumber splitNo, bool thum = false)
        {
            var canvasWidth = _config.TargetScreenSize.Width;
            var canvasHeight = _config.TargetScreenSize.Height;
            var bmpCanvas = new Bitmap(canvasWidth, canvasHeight);
            Graphics g = Graphics.FromImage(bmpCanvas);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

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
#if false
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
#endif
                    var result = _coordinateCalculator.CalcCrop(canvasWidth, canvasHeight, quotaWidth, quotaHeight, img.Width, img.Height, ref x, ref y);
                    g.DrawImage(img, result.DstRect, result.SrcRect, GraphicsUnit.Pixel);
                }
                else
                {
#if false
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
#endif
                    bool tst = true;
                    if (tst)
                    {
                        var result = _coordinateCalculator.CalcFit(canvasWidth, canvasHeight, quotaWidth, quotaHeight, img.Width, img.Height, ref x, ref y);
                        var yohakuW = (quotaWidth - result.DstRect.Width) / 2;
                        var yohakuH = (quotaHeight - result.DstRect.Height) / 2;
                        result.DstRect.X += yohakuW;
                        result.DstRect.Y += yohakuH;
                        g.DrawImage(img, result.DstRect, result.SrcRect, GraphicsUnit.Pixel);
                        DrawPicInfo(p, g, img, result.DstRect.Width, result.DstRect.Height, result.DstRect.X - yohakuW, result.DstRect.Y - yohakuH, result.Ratio);
                    }
                    else
                    {   // はみ出る部分切り取り
                        var result = _coordinateCalculator.CalcCrop(canvasWidth, canvasHeight, quotaWidth, quotaHeight, img.Width, img.Height, ref x, ref y);
                        g.DrawImage(img, result.DstRect, result.SrcRect, GraphicsUnit.Pixel);
                    }
                }
                x += quotaWidth;
            }
            g.Dispose();

            //var ms = new MemoryStream();
            //bmpCanvas.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            //return ms.ToArray();
            return GetBmpByteStream(bmpCanvas);
        }

        private byte[] ResizeImageIfNecessary(PicInfo p, Image img)
        {
            var result =  _coordinateCalculator.Calculate(img.Width, img.Height);

            Bitmap bmpCanvas = DrawImageAndInfo(p, result.Canvas.Width, result.Canvas.Height, img, result.DstRect, result.SrcRect, result.Ratio);
            if (result.Ratio < 1)
            {
                p.ZipEntryName += "-shrink";
            }
            p.ZipEntryName += string.Format("({0}x{1})", result.Canvas.Width, result.Canvas.Height);

            //var ms = new MemoryStream();
            //bmpCanvas.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            //return ms.ToArray();
            return GetBmpByteStream(bmpCanvas);
        }

        private Bitmap DrawImageAndInfo(PicInfo p, int canvasWidth, int canvasHeight, Image img, Rectangle dstRect, Rectangle srcRect, float ratio)
        {
            // draw
            var bmpCanvas = new Bitmap(canvasWidth, canvasHeight);

            Graphics g = Graphics.FromImage(bmpCanvas);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            //var brush = Brushes.LightGray;
            var brush = Brushes.DarkGray;
            g.FillRectangle(brush, 0, 0, canvasWidth, canvasHeight);

            g.DrawImage(img, dstRect, srcRect, GraphicsUnit.Pixel);

            //DrawPicInfo(p, g, img, dstRect.Width, dstRect.Height, dstRect.X, dstRect.Y, ratio);
            DrawPicInfo(p, g, img, dstRect.Width, dstRect.Height, 0, 0, ratio);

            g.Dispose();

            return bmpCanvas;
        }

        private void DrawPicInfo(PicInfo p, Graphics g, Image img, int w, int h, int x, int y, float ratio)
        {
            if (_config.isPicSizeDraw)
            {
                var fsize = 20;
                //var fcolor = Brushes.Cyan;
                //var fcolor = Brushes.Azure;
                var fcolor = Brushes.Navy;
                var fnt = new Font("MS UI Gothic", fsize);
                var drawY = y;
                
                g.DrawString(string.Format("{0,3}:{1}", p.Number, GetTitle(p.Path)), fnt, fcolor, x, drawY);
                drawY += fsize;
                g.DrawString(string.Format("{0,4}x{1,4}", img.Width, img.Height), fnt, fcolor, x, drawY);
                drawY += fsize;
                g.DrawString(string.Format("{0,4}x{1,4}({2}%) [{3}x{4}]", w, h, (int)(ratio * 100), g.VisibleClipBounds.Width, g.VisibleClipBounds.Height), fnt, fcolor, x, drawY);
            }
        }

        private void DrawPicInfoPath(PicInfo p, Graphics g, Image img, int w, int h, int x, int y, float ratio)
        {
            if (_config.isPicSizeDraw)
            {
                // 縁取り。ジャギで見にくいのでやめ。アンチエイリアス？
                var fsize = 30;
                var gp = new GraphicsPath();
                var ff = new FontFamily("メイリオ");
                var str = "";

                var drawY = y;

                str = string.Format("{0,3}:{1}", p.Number, GetTitle(p.Path));
                DrawString(g, gp, str, ff, x, drawY); 
                drawY += fsize;

                str = string.Format("{0,4}x{1,4}", img.Width, img.Height);
                DrawString(g, gp, str, ff, x, drawY);
                drawY += fsize;

                str = string.Format("{0,4}x{1,4}({2}%) [{3}x{4}]", w, h, (int)(ratio * 100), g.VisibleClipBounds.Width, g.VisibleClipBounds.Height);
                DrawString(g, gp, str, ff, x, drawY);

                ff.Dispose();
            }
        }

        private void DrawString(Graphics g, GraphicsPath gp, string str, FontFamily ff, int x, int y)
        {
            gp.AddString(str, ff, 0, 30, new Point(x, y), StringFormat.GenericDefault);
            g.FillPath(Brushes.White, gp);
            g.DrawPath(Pens.Black, gp);
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

        private byte[] GetBmpByteStream(Bitmap bmpCanvas)
        {
            var ms = new MemoryStream();
            bmpCanvas.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            return ms.ToArray();
        }
    }
}
