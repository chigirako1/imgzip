using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public CalcResult Calculate(int imgWidth, int imgHeight)
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
            float scrnAsp = GetCanvasScreenRatio();// (float)TargetScreenSize.Width / (float)TargetScreenSize.Height;
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

            CalcResult result;
            result.Canvas = new Size(canvasWidth, canvasHeight);
            result.DstRect = new Rectangle(x, y, w, h);
            result.SrcRect = new Rectangle(0, 0, imgWidth, imgHeight); 
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

        public CalcResult CalcFitWidth(int imgWidth, int imgHeight)
        {
            var w = Math.Min(imgWidth, TargetScreenSize.Width);
            var ratio = 1f;
            var h = Math.Min(imgHeight, TargetScreenSize.Height);

            var canvasWidth = w;
            var canvasHeight = (int)(canvasWidth / GetCanvasScreenRatio());

            var x = (canvasWidth - w) / 2;
            var y = (canvasHeight - h) / 2;

            // src
            var srcW = (int)(w * ratio);
            var srcH = (int)(h * ratio);
            var srcX = (imgWidth - srcW) / 2;
            var srcY = 0;

            CalcResult result;
            result.Canvas = new Size(canvasWidth, canvasHeight);
            result.DstRect = new Rectangle(x, y, w, h);
            result.SrcRect = new Rectangle(srcX, srcY, srcW, srcH);
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

        public CalcResult CalcTrimLS(int imgWidth, int imgHeight, int splitIdx)
        {   // 横長画像向け。横を切り取る
            var hRatio = (float)TargetScreenSize.Height / (float)imgHeight;
            var ratio = Math.Min(1.0f, hRatio);

            var h = Math.Min((int)(imgHeight * ratio), TargetScreenSize.Height);
            var scrnAsp = GetCanvasScreenRatio();
            var canvasWidth = (int)(h * scrnAsp);
            var canvasHeight = Math.Min(TargetScreenSize.Height, h);
            var w = canvasWidth;

            // dst
            var x = (canvasWidth - w) / 2;
            var y = (canvasHeight - h) / 2;
            var dstRect = new Rectangle(x, y, w, h);

            // src
            var srcW = (int)(w * ratio);
            var srcH = (int)(h * ratio);
            var srcX = 0;
            var srcY = 0;
            switch (splitIdx)
            {
                case 1://right
                    srcX = imgWidth - srcW;
                    break;
                case 3://left
                    break;
                case 2://center
                default:
                    srcX = (imgWidth - srcW) / 2;
                    break;
            }
            var srcRect = new Rectangle(srcX, srcY, srcW, srcH);

            CalcResult result;
            result.Canvas = new Size(canvasWidth, canvasHeight);
            result.DstRect = dstRect;
            result.SrcRect = srcRect;
            result.Ratio = ratio;
            return result;
        }

        public CalcResult CalcTrimPL(int imgWidth, int imgHeight, int splitIdx)
        {   // 縦長。縦を切り取る
            var wRatio = (float)TargetScreenSize.Width / (float)imgWidth;
            var ratio = Math.Min(1.0f, wRatio);

            var w = Math.Min((int)(imgWidth * ratio), TargetScreenSize.Width);
            var scrnAsp = GetCanvasScreenRatio();
            var canvasHeight = (int)(w / scrnAsp);
            var canvasWidth =  Math.Min(TargetScreenSize.Width, w);
            var h = canvasHeight;

            // dst
            var x = (canvasWidth - w) / 2;
            var y = (canvasHeight - h) / 2;
            var dstRect = new Rectangle(x, y, w, h);

            // src
            //var srcW = (int)(w * ratio);
            //var srcH = (int)(h * ratio);
            var srcW = (int)(w / ratio);
            var srcH = (int)(h / ratio);
            var srcX = 0;
            var srcY = 0;
            switch (splitIdx)
            {
                case 1://top
                    srcY = imgHeight - srcH;
                    break;
                case 3://bottom
                    break;
                case 2://center
                default:
                    srcY = Math.Abs((imgHeight - srcH) / 2);
                    break;
            }
            var srcRect = new Rectangle(srcX, srcY, srcW, srcH);

            CalcResult result;
            result.Canvas = new Size(canvasWidth, canvasHeight);
            result.DstRect = dstRect;
            result.SrcRect = srcRect;
            result.Ratio = ratio;
            return result;
        }
    }
}
