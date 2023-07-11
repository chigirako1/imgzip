using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MyZipper.src
{
    internal class Splitter
    {
        private Config _config;

        public Splitter(Config config)
        {
            _config = config;
        }

        public void Split(PicInfoList piclist)
        {
            foreach (var p in piclist.PicInfos)
            {
                SplitImage(p);
            }
        }

        private void SplitImage(PicInfo p)
        {
            var img = p.GetImage();
            if (img.Width < img.Height)
            {
                Log.W(string.Format("skip file:\"{0}\"{1}x{2}", p.Path, img.Width, img.Height));
                return;
            }
            var canvasWidth = img.Width / _config.SplitLR;
            var canvasHeight = img.Height;
            var bmpCanvas = new Bitmap(canvasWidth, canvasHeight);
            Graphics g = Graphics.FromImage(bmpCanvas);
            Rectangle dstRect = new Rectangle(0, 0, canvasWidth, canvasHeight);

            for (int i = 0; i < _config.SplitLR;  i++)
            {
                Rectangle srcRect = new Rectangle(canvasWidth * i, 0, canvasWidth, canvasHeight);
                g.DrawImage(img, dstRect, srcRect, GraphicsUnit.Pixel);

                var srcDirname = Path.GetDirectoryName(p.Path);
                var dstDirname = srcDirname + "new";
                var srcFilename = Path.GetFileNameWithoutExtension(p.Path);
                int renban;
                if (true)
                {
                    // r -> l
                    renban = _config.SplitLR - i;
                }
                else
                {
                    // l -> r
                    renban = i + 1;
                }
                var dstFilename = string.Format("{0}-{1}.jpg", srcFilename, renban);
                if (!File.Exists(dstDirname))
                {
                    Directory.CreateDirectory(dstDirname);
                }
                var dstPath = Path.Combine(dstDirname, dstFilename);

                bmpCanvas.Save(dstPath, System.Drawing.Imaging.ImageFormat.Jpeg);
            }

            bmpCanvas.Dispose();
        }
    }
}
