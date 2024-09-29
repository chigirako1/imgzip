using System;
using System.Drawing;

namespace MyZipper
{
    public struct CalcResult
    {
        public Size Canvas;
        public float Ratio;
        public Rectangle DstRect;
        public Rectangle SrcRect;

        public CalcResult(Size canvas, float r, Rectangle dstRect, Rectangle srcRect)
        {
            Canvas = canvas;
            Ratio = r;
            DstRect = dstRect;
            SrcRect = srcRect;
        }
    }

    public class CoordinateCalculator
    {
        private Size TargetScreenSize { get; set; }

        public CoordinateCalculator(Size targeScreenSize)
        {
            TargetScreenSize = targeScreenSize;
        }

        public float GetCanvasScreenRatio()
        {
            return (float)TargetScreenSize.Width / (float)TargetScreenSize.Height;
        }

        public CalcResult Calculate(int imgWidth, int imgHeight, bool adjust_by_aspect=true)
        {
            var wRatio = (float)TargetScreenSize.Width / (float)imgWidth;
            var hRatio = (float)TargetScreenSize.Height / (float)imgHeight;
            var ratio = Math.Min(wRatio, hRatio);//画面内に収まるようにする

            var w = imgWidth;
            var h = imgHeight;
            if (ratio < 1)
            {
                w = (int)(w * ratio);
                h = (int)(h * ratio);
            }
            else
            {   //拡大はしない
                ratio = 1;
            }
            var x = 0;
            var y = 0;

            var canvasWidth = w;
            var canvasHeight = h;
            if (adjust_by_aspect)
            {
                float scrnAsp = GetCanvasScreenRatio();
                float picRatio = (float)w / (float)h;
                // キャンバスサイズを表示領域のアスペクト比に合わせる
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
                }
            }

            var srcX = 0;
            var srcY = 0;

            CalcResult result;
            result.Canvas = new Size(canvasWidth, canvasHeight);
            result.DstRect = new Rectangle(x, y, w, h);
            result.SrcRect = new Rectangle(srcX, srcY, imgWidth, imgHeight);
            result.Ratio = ratio;
            return result;
        }

        public CalcResult CalcFit(int canvasWidth, int canvasHeight, int quotaWidth, int quotaHeight, int imgWidth, int imgHeight, ref int x, ref int y)
        {
            var wRatio = (float)quotaWidth / (float)imgWidth;
            var hRatio = (float)quotaHeight / (float)imgHeight;
            var ratio = Math.Min(wRatio, hRatio);// 拡大の場合あり
            var w = (int)(imgWidth * ratio);
            var h = (int)(imgHeight * ratio);

            if (canvasWidth - x < w)
            {
                y += quotaHeight;
                x = 0;
            }

            CalcResult result;
            result.Canvas = new Size(canvasWidth, canvasHeight);
            result.DstRect = new Rectangle(x, y, w, h);
            result.SrcRect = new Rectangle(0, 0, imgWidth, imgHeight);
            result.Ratio = ratio;
            return result;
        }

        public CalcResult CalcCrop(int imgWidth, int imgHeight, bool nomagnify = true)
        {
            int x = 0;
            int y = 0;
            int canvasWidth = TargetScreenSize.Width;
            int canvasHeight = TargetScreenSize.Height;
            var wRatio = (float)canvasWidth / (float)imgWidth;
            var hRatio = (float)canvasHeight / (float)imgHeight;
            var ratio = Math.Max(wRatio, hRatio);

            if (nomagnify && ratio > 1f)
            {//拡大しない
                ratio = 1f;

                float scrnAsp = GetCanvasScreenRatio();
                if (hRatio < 1f || wRatio > hRatio)
                {
                    canvasWidth = imgWidth;
                    canvasHeight = (int)(imgWidth / scrnAsp);
                }
                else
                {
                    canvasHeight = imgHeight;
                    canvasWidth = (int)(imgHeight * scrnAsp); ;
                }
            }
            
            // dst
            var w = canvasWidth;
            var h = canvasHeight;
            var dstRect = new Rectangle(x, y, w, h);

            // src
            var srcW = (int)(dstRect.Width / ratio);
            var srcH = (int)(dstRect.Height / ratio);
            var srcX = (imgWidth - srcW) / 2;
            var srcY = (imgHeight - srcH) / 2;
            var srcRect = new Rectangle(srcX, srcY, srcW, srcH);

            CalcResult result;
            result.Canvas = new Size(canvasWidth, canvasHeight);
            result.SrcRect = srcRect;
            result.DstRect = dstRect;
            result.Ratio = ratio;
            return result;
        }

        public CalcResult CalcCrop(int canvasWidth, int canvasHeight, int quotaWidth, int quotaHeight, int imgWidth, int imgHeight, ref int x, ref int y)
        {   // はみ出す部分は切り捨てる

            var wRatio = (float)quotaWidth / (float)imgWidth;
            var hRatio = (float)quotaHeight / (float)imgHeight;
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
            var srcX = (imgWidth - srcW) / 2;
            var srcY = (imgHeight - srcH) / 2;
            var srcRect = new Rectangle(srcX, srcY, srcW, srcH);

            CalcResult result;
            result.Canvas = new Size(canvasWidth, canvasHeight);
            result.SrcRect = srcRect;
            result.DstRect = dstRect; 
            result.Ratio = ratio;
            return result;
        }

        public CalcResult CalcCrop(int imgWidth, int imgHeight, int nume, int denomi)
        {
            var wRatio = (float)TargetScreenSize.Width / (float)imgWidth;
            var hRatio = (float)TargetScreenSize.Height / (float)imgHeight;

            float ratio;
            Rectangle dstRect;
            Rectangle srcRect;
            var canvasWidth = TargetScreenSize.Width;
            var canvasHeight = TargetScreenSize.Height;
            if (wRatio > hRatio)
            {   //縦長画像。横幅に合わせる（上下がはみだす）。

                ratio = Math.Min(wRatio, 1.0f);
                //ratio = wRatio;

                // dst
                var w = (int)(imgWidth * ratio);
                var h = Math.Min((int)(imgHeight * ratio), canvasHeight);
                var x = (canvasWidth - w) / 2;
                var y = (canvasHeight - h) / 2;
                dstRect = new Rectangle(x, y, w, h);

                // src
                var srcW = (int)(w / ratio);
                var srcH = (int)(h / ratio);
                var srcX = 0;
                int srcY;
                if (nume == 1)
                {   //top
                    srcY = 0;
                }
                else if (nume == denomi)
                {   // bottom
                    srcY = imgHeight - srcH;
                }
                else
                {   // middle
                    //srcY = Math.Abs((imgHeight - srcH) / 2);
                    srcY = imgHeight - (imgHeight / denomi) * nume;
                }
                srcRect = new Rectangle(srcX, srcY, srcW, srcH);
            }
            else
            {   // 横長画像。
                ratio = Math.Min(hRatio, 1.0f);
                //ratio = hRatio;

                // dst
                var w = Math.Min((int)(imgWidth * ratio), canvasWidth);
                var h = (int)(imgHeight * ratio);
                var x = (canvasWidth - w) / 2;
                var y = (canvasHeight - h) / 2;
                dstRect = new Rectangle(x, y, w, h);

                // src
                var srcW = (int)(w / ratio);
                var srcH = (int)(h / ratio);
                int srcX;
                var srcY = 0;
                if (nume == 1)
                {   // right
                    srcX = imgWidth - srcW;
                }
                else if (nume == denomi)
                {   // bottom
                    srcX = 0;
                }
                else
                {   // middle
                    srcX = imgWidth - (imgWidth / denomi) * nume;
                }
                srcRect = new Rectangle(srcX, srcY, srcW, srcH);
            }


            CalcResult result;
            result.Canvas = new Size(canvasWidth, canvasHeight);
            result.SrcRect = srcRect;
            result.DstRect = dstRect;
            result.Ratio = ratio;
            return result;
        }
    }
}
