using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
//using System.Windows.Media;

namespace MyZipper
{
    internal class Zipper
    {
        private static readonly Brush BG_BRUSH = Brushes.Black;
        private static readonly Brush BG_BRUSH_ROT = Brushes.DarkCyan; 
        private static readonly Brush BG_BRUSH_COMB = Brushes.DarkBlue; 
        //static private Brush EMPTY_BG_BRUSH = Brushes.DarkGray;
        //static private Brush FONT_BRUSH = Brushes.Navy;
        private static readonly Brush FONT_BRUSH = Brushes.Cyan;

        private readonly Config _config;
        private readonly CoordinateCalculator _coordinateCalculator;

        public Zipper(Config config)
        {
            _config = config;
            _coordinateCalculator = new CoordinateCalculator(_config.TargetScreenSize);
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
            if (_config.IsAppendIdxCover && piclist.PicInfos.Count >= _config.IdxOutThreshold)
            {   //先頭にサムネイルをまとめた画像を追加する
                OutputThumbnailListToArchiveFile(piclist, archive);
            }

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
                    {
                        if (plPicInfos.Count > 0)
                        {   
                            entryname = MakeEntryName(ref cnt, plPicInfos, "tate");
                            OutCombineImagePL(plPicInfos, archive, entryname);
                        }

                        // 一枚絵として出力
                        MakeImageAndAddZipEntry(ref cnt, p, archive);
                    }
                    else
                    {
                        plPicInfos.Add(p);
                        if (plPicInfos.Count >= _config.PlNumberOfCol * _config.PlNumberOfRow)
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
                        if (lsPicInfos.Count >= _config.LsNumberOfCol * _config.LsNumberOfRow)
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
                //AddEmptyImage(archive);
                AddEmptyImage(piclist, archive, ref cnt);
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
            int splitNoV = Math.Min(_config.PlNumberOfCol, picInfos.Count);
            int splitNoH = Math.Min(_config.PlNumberOfRow, picInfos.Count);
            SplitScreenNumber splitNo;
            splitNo.Col = splitNoV;
            splitNo.Row = splitNoH;

            byte[] bs = GetCombineImage(picInfos, splitNo);

            AddZipEntry(archive, entryname, bs);

            picInfos.Clear();
        }

        private void OutCombineImageLS(List<PicInfo> picInfos, ZipArchive archive, string entryname, bool ls = false)
        {
            int splitNoV = Math.Min(_config.LsNumberOfCol, picInfos.Count);
            int splitNoH = Math.Min(_config.LsNumberOfRow, picInfos.Count);
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
            if (_config.IsRotateAlt && p.IsRotated)
            {   // 回転しない画像も出力する
                MakeImageAndAddZipEntrySub(ref cnt, p, archive, false);
            }

            // 細長い画像を分割して保存する
            if (_config.IsSplitLongImage)
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

        private void MakeImageAndAddZipEntrySub2(ref int cnt, PicInfo p, ZipArchive archive)
        {
            p.ZipEntryName = p.ZipEntryNameOrig;

            byte[] bs = GetImageBinary(p, true, true);

            cnt++;
            var entryname = cnt.ToString("D3") + " " + p.ZipEntryName + ".jpg";
            if (p.IsRotated)
            {
                entryname = "Rot-" + entryname;
            }
            AddZipEntry(archive, entryname, bs);
            p.IsDone = true;
        }


        private void AddSplitedImage(ref int cnt, PicInfo p, ZipArchive archive, int nume, int denomi)
        {
            var img = p.GetImage();

            var result = _coordinateCalculator.CalcCrop(img.Width, img.Height, nume, denomi);
            Bitmap bmpCanvas = DrawImageAndInfo(p, result.Canvas.Width, result.Canvas.Height, img, result.DstRect, result.SrcRect, result.Ratio);
            var bs = GetBmpByteStream(bmpCanvas);

            cnt++;
            var entryname = cnt.ToString("D3") + "-" + nume.ToString("D1") + " " + p.ZipEntryNameOrig + ".jpg";
            AddZipEntry(archive, entryname, bs);
        }

        private byte[] GetImageBinary(PicInfo p, bool rot, bool crop = false)
        {
            var img = p.GetImage();

            if (rot && _config.RotatePredicate(img.Size))
            {
                //横長かつターゲット画面サイズ以上の場合は左90度回転させる
                img.RotateFlip(RotateFlipType.Rotate270FlipNone);
                p.ZipEntryName += "-rot270";
                p.IsRotated = true;
            }

            var bs = ResizeImageIfNecessary(p, img, crop);
            
            return bs;
        }

        /*private void AddEmptyImage(ZipArchive archive)
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
        }*/

        private void AddEmptyImage(PicInfoList picInfos, ZipArchive archive, ref int cnt)
        {
            var p = picInfos.PicInfos[0];
            MakeImageAndAddZipEntrySub2(ref cnt, p, archive);
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

            // 背景描画
            var onecolor = false;
            if (onecolor)
            {
                var brush = BG_BRUSH_COMB;
                g.FillRectangle(brush, 0, 0, canvasWidth, canvasHeight);
            }
            else
            {
                var p = picInfos[0];
                var img = p.GetImage();

                var result = _coordinateCalculator.CalcCrop(img.Width, img.Height, false);
                g.DrawImage(img, result.DstRect, result.SrcRect, GraphicsUnit.Pixel);

                var opaqueBrush = new SolidBrush(Color.FromArgb(128, 0, 0, 0)); ;
                g.FillRectangle(opaqueBrush, 0, 0, canvasWidth, canvasHeight);
            }

            var x = 0;
            var y = 0;
            if (thum)
            {
                var fsize = 40;
                var fcolor = FONT_BRUSH;
                var fnt = new Font("MS UI Gothic", fsize);
                var drawY = y;
                string str;
                str = string.Format($"{_config.Inputpath}");
                g.DrawString(str, fnt, fcolor, x, drawY);

                // 上に余白をあける
                y = (canvasHeight / 16);
            }

            var splitNoV = splitNo.Col;
            var splitNoH = splitNo.Row;
            var quotaWidth = canvasWidth / splitNoV;
            var quotaHeight = (canvasHeight - y) / splitNoH;

            foreach (var p in picInfos)
            {
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
                    if (_config.IsCrop)
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

        private byte[] ResizeImageIfNecessary(PicInfo p, Image img, bool crop = false)
        {
            CalcResult result;
            if (crop)
            {
                result = _coordinateCalculator.CalcCrop(img.Width, img.Height);
            }
            else
            {
                result = _coordinateCalculator.Calculate(img.Width, img.Height);
            }
            

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
            var bmpCanvas = new Bitmap(canvasWidth, canvasHeight);

            Graphics g = Graphics.FromImage(bmpCanvas);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            var asp = p.GetAspectRatio();
            var bgimage = _config.IsHugeDiff(asp); 
            if (bgimage)
            {
                var result = _coordinateCalculator.CalcCrop(img.Width, img.Height, false);
                g.DrawImage(img, result.DstRect, result.SrcRect, GraphicsUnit.Pixel);

                Color bkcolor;
                if (p.IsRotated)
                {
                    //bkcolor = Color.DarkCyan;
                    bkcolor = Color.Black;
                }
                else
                {
                    bkcolor = Color.Black;
                }
                var opaqueBrush = new SolidBrush(Color.FromArgb(192, bkcolor)); ;
                g.FillRectangle(opaqueBrush, 0, 0, canvasWidth, canvasHeight);
            }
            else
            {
                Brush brush;
                if (p.IsRotated)
                {
                    brush = BG_BRUSH_ROT;
                }
                else
                {
                    brush = BG_BRUSH;
                }
                g.FillRectangle(brush, 0, 0, canvasWidth, canvasHeight);
            }

            g.DrawImage(img, dstRect, srcRect, GraphicsUnit.Pixel);

            DrawPicInfo(p, g, img, dstRect.Width, dstRect.Height, 0, 0, ratio);

            g.Dispose();

            return bmpCanvas;
        }

        private void DrawPicInfo(PicInfo p, Graphics g, Image img, int w, int h, int x, int y, float ratio)
        {
            if (_config.IsPicSizeDraw)
            {
                var fsize = 20;
                var fcolor = FONT_BRUSH;
                var fnt = new Font("MS UI Gothic", fsize);
                var drawY = y;

                string str;
                str = string.Format("{0,3}:{1}", p.Number, p.GetTitle());
                g.DrawString(str, fnt, fcolor, x, drawY);
                drawY += fsize;

                if (_config.Mode == Mode.Twt)
                {
                    str = string.Format("uploaded at {0}", p.GetUploadDate());
                    g.DrawString(str, fnt, fcolor, x, drawY);
                    drawY += fsize;
                }

                // WxH [10:16]
                str = string.Format("{0,4}x{1,4}{2} {3}", img.Width, img.Height, p.GetAspectRatioStr(), p.FileSizeStr());
                g.DrawString(str, fnt, fcolor, x, drawY);
                drawY += fsize;

                str = string.Format("{0,4}x{1,4}({2}%) [{3}x{4}]({5})",
                    w,
                    h,
                    (int)(ratio * 100),
                    g.VisibleClipBounds.Width,
                    g.VisibleClipBounds.Height,
                    _config.GetMagRatio(w, h)
                    );
                g.DrawString(str, fnt, fcolor, x, drawY);
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
                Log.V(entryName);
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
