using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;

namespace MyZipper
{
    internal class Zipper
    {
        static private Brush BG_BRUSH = Brushes.Black;
        static private Brush EMPTY_BG_BRUSH = Brushes.DarkGray;
        //static private Brush FONT_BRUSH = Brushes.Navy;
        static private Brush FONT_BRUSH = Brushes.Cyan;

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
            splitNo.Col = Math.Min(10, (int)Math.Sqrt(piclist.PicInfos.Count));
            splitNo.Row = Math.Min(10, (int)Math.Sqrt(piclist.PicInfos.Count));
            if (splitNo.Col * splitNo.Row < piclist.PicInfos.Count)
            {
                splitNo.Row++;
            }
            if (splitNo.Col * splitNo.Row < piclist.PicInfos.Count)
            {
                splitNo.Col++;
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

                    if (_config.NoComposite || p.IsOutputAlone(_config.TargetScreenSize))
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
                else if (_config.LsCompositeLs && p.PicSize.Width <= 640 && p.PicSize.Height <= 480)
                {
                    if (_config.NoComposite || p.IsOutputAlone(_config.TargetScreenSize, true))
                    {  // 一枚絵として出力
                        MakeImageAndAddZipEntry(ref cnt, p, archive);
                    }
                    else
                    {
                        lsPicInfos.Add(p);
                        if (lsPicInfos.Count >= 3 * 2)
                        {
                            entryname = MakeEntryName(ref cnt, lsPicInfos, "yoko");
                            OutCombineImageLS(lsPicInfos, archive, entryname, _config.LsCompositeLs);
                        }
                    }
                }
                else
                {   // 横長

                    if (_config.NoComposite || p.IsOutputAlone(_config.TargetScreenSize, true))
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
#if false
                        if (p.PicSize.Width >= 640 && p.PicSize.Height>= 480)
                        {
                            MakeImageAndAddZipEntry(ref cnt, p, archive);
                        }
#endif
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
                OutCombineImageLS(lsPicInfos, archive, entryname, false);
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
            splitNo.Col = splitNoV;
            splitNo.Row = splitNoH;

            byte[] bs = GetCombineImage(picInfos, splitNo);

            AddZipEntry(archive, entryname, bs);

            picInfos.Clear();
        }

        private void OutCombineImageLS(List<PicInfo> picInfos, ZipArchive archive, string entryname, bool ls = false)
        {
            int splitNoV = Math.Min(_config.NumberOfSplitScreenVforLsImage, picInfos.Count);
            int splitNoH = Math.Min(_config.NumberOfSplitScreenHforLsImage, picInfos.Count);
            SplitScreenNumber splitNo;
            if (ls)
            {
                splitNo.Col = 3;
                splitNo.Row = 2;
            }
            else
            {
                splitNo.Col = splitNoV;
                splitNo.Row = splitNoH;
            }

            byte[] bs = GetCombineImage(picInfos, splitNo, false, ls);

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
                int denomi;
                if (p.PicSize.Width > p.PicSize.Height)
                {   // 横長
                    denomi = (int)Math.Ceiling((double)p.PicSize.Width / (double)_config.TargetScreenSize.Width);
                }
                else
                {
                    denomi = (int)Math.Ceiling((double)p.PicSize.Height / (double)_config.TargetScreenSize.Height);
                }
                if (denomi > 2)
                {
                    for (var nume = 1; nume <= denomi; nume++)
                    {
                        AddSplitedImage(ref cnt, p, archive, nume, denomi);
                    }
                }
            }
        }

        private void MakeImageAndAddZipEntrySub(ref int cnt, PicInfo p, ZipArchive archive, bool rot)
        {
            byte[] bs = GetImageBinary(p, rot);

            cnt++;
            var entryname = cnt.ToString("D3") + " " + p.ZipEntryName + ".jpg";
            if (rot && p.IsRotated)
            {
                entryname = "Rot-" + entryname;
            }
            AddZipEntry(archive, entryname, bs);
            p.IsDone = true;
        }

        private void AddSplitedImage(ref int cnt, PicInfo p, ZipArchive archive, int nume, int denomi)
        {
            //var img = Image.FromFile(p.Path);
            var img = p.GetImage();

            var result = _coordinateCalculator.CalcCrop(img.Width, img.Height, nume, denomi);
            Bitmap bmpCanvas = DrawImageAndInfo(p, result.Canvas.Width, result.Canvas.Height, img, result.DstRect, result.SrcRect, result.Ratio);
            var bs = GetBmpByteStream(bmpCanvas);

            cnt++;
            var entryname = cnt.ToString("D3") + "-" + nume.ToString("D1") + " " + p.ZipEntryNameOrig + ".jpg";
            AddZipEntry(archive, entryname, bs);
        }

        private byte[] GetImageBinary(PicInfo p, bool rot)
        {
            //var img = Image.FromFile(p.Path);
            var img = p.GetImage();

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

            var brush = EMPTY_BG_BRUSH;
            g.FillRectangle(brush, 0, 0, canvasWidth, canvasHeight);

            var fsize = 20;
            var fcolor = FONT_BRUSH;
            var fnt = new Font("MS UI Gothic", fsize);
            g.DrawString("end", fnt, fcolor, 0, 0);
            g.Dispose();

            byte[] bs = GetBmpByteStream(bmpCanvas);

            AddZipEntry(archive, Config.GRAY_IMAGE_ENTRY_NAME, bs);
        }

        private byte[] GetCombineImage(List<PicInfo> picInfos, SplitScreenNumber splitNo, bool thum = false, bool ls = false)
        {
            int canvasWidth;
            int canvasHeight;
            if (ls)
            {   // 縦と横を入れ替える
                canvasWidth = _config.TargetScreenSize.Height; 
                canvasHeight = _config.TargetScreenSize.Width;
            }
            else
            {
                canvasWidth = _config.TargetScreenSize.Width;
                canvasHeight = _config.TargetScreenSize.Height;
            }
            var bmpCanvas = new Bitmap(canvasWidth, canvasHeight);
            Graphics g = Graphics.FromImage(bmpCanvas);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            var brush = BG_BRUSH;
            g.FillRectangle(brush, 0, 0, canvasWidth, canvasHeight);

            var x = 0;
            var y = 0;
            if (thum)
            {
                y = (canvasHeight / 16);//少し上を開ける（
            }

            var splitNoV = splitNo.Col;
            var splitNoH = splitNo.Row;
            var quotaWidth = canvasWidth / splitNoV;
            var quotaHeight = (canvasHeight - y) / splitNoH;

            foreach (var p in picInfos)
            {
                //var img = Image.FromFile(p.Path);
                var img = p.GetImage();


                if (thum)
                {
                    if (img.Width > img.Height)
                    {
                        img.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    }
                    var result = _coordinateCalculator.CalcCrop(canvasWidth, canvasHeight, quotaWidth, quotaHeight, img.Width, img.Height, ref x, ref y);
                    g.DrawImage(img, result.DstRect, result.SrcRect, GraphicsUnit.Pixel);
                }
                else
                {
                    if (_config.isCrop)
                    {   // はみ出る部分切り捨て
                        var result = _coordinateCalculator.CalcCrop(canvasWidth, canvasHeight, quotaWidth, quotaHeight, img.Width, img.Height, ref x, ref y);
                        var yohakuW = (quotaWidth - result.DstRect.Width) / 2;
                        var yohakuH = (quotaHeight - result.DstRect.Height) / 2;
                        g.DrawImage(img, result.DstRect, result.SrcRect, GraphicsUnit.Pixel);
                        DrawPicInfo(p, g, img, result.DstRect.Width, result.DstRect.Height, result.DstRect.X - yohakuW, result.DstRect.Y - yohakuH, result.Ratio);
                    }
                    else
                    {
                        var result = _coordinateCalculator.CalcFit(canvasWidth, canvasHeight, quotaWidth, quotaHeight, img.Width, img.Height, ref x, ref y);
                        var yohakuW = (quotaWidth - result.DstRect.Width) / 2;
                        var yohakuH = (quotaHeight - result.DstRect.Height) / 2;
                        result.DstRect.X += yohakuW;
                        result.DstRect.Y += yohakuH;
                        g.DrawImage(img, result.DstRect, result.SrcRect, GraphicsUnit.Pixel);
                        DrawPicInfo(p, g, img, result.DstRect.Width, result.DstRect.Height, result.DstRect.X - yohakuW, result.DstRect.Y - yohakuH, result.Ratio);
                    }
                }
                x += quotaWidth;
            }
            g.Dispose();

            if (ls)
            {
                bmpCanvas.RotateFlip(RotateFlipType.Rotate270FlipNone);
            }
         
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

            return GetBmpByteStream(bmpCanvas);
        }

        private Bitmap DrawImageAndInfo(PicInfo p, int canvasWidth, int canvasHeight, Image img, Rectangle dstRect, Rectangle srcRect, float ratio)
        {
            // draw
            var bmpCanvas = new Bitmap(canvasWidth, canvasHeight);

            Graphics g = Graphics.FromImage(bmpCanvas);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            var brush = BG_BRUSH;
            g.FillRectangle(brush, 0, 0, canvasWidth, canvasHeight);

            g.DrawImage(img, dstRect, srcRect, GraphicsUnit.Pixel);

            DrawPicInfo(p, g, img, dstRect.Width, dstRect.Height, 0, 0, ratio);

            g.Dispose();

            return bmpCanvas;
        }

        private void DrawPicInfo(PicInfo p, Graphics g, Image img, int w, int h, int x, int y, float ratio)
        {
            if (_config.isPicSizeDraw)
            {
                var fsize = 20;
                var fcolor = FONT_BRUSH;
                var fnt = new Font("MS UI Gothic", fsize);
                var drawY = y;
                
                g.DrawString(string.Format("{0,3}:{1}", p.Number, GetTitle(p.Path)), fnt, fcolor, x, drawY);
                drawY += fsize;
                g.DrawString(string.Format("{0,4}x{1,4}", img.Width, img.Height), fnt, fcolor, x, drawY);
                drawY += fsize;
                g.DrawString(string.Format("{0,4}x{1,4}({2}%) [{3}x{4}]", w, h, (int)(ratio * 100), g.VisibleClipBounds.Width, g.VisibleClipBounds.Height), fnt, fcolor, x, drawY);
            }
        }

#if false
        ///ジャギで見にくいのでやめ。アンチエイリアス？
        private void DrawPicInfoPath(PicInfo p, Graphics g, Image img, int w, int h, int x, int y, float ratio)
        {
            if (_config.isPicSizeDraw)
            {
                // 縁取り。
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
#endif

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
