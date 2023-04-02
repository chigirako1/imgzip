using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using MyZipper;
using MyZipper.src;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethodCalc()
        {
            var c = new CoordinateCalculator(new Size(1200, 1920));

            // 等倍
            var result = c.Calculate(1200, 1920);
            Assert.AreEqual(new Size(1200, 1920), result.Canvas);
            Assert.AreEqual(1.0f, result.Ratio);
            Assert.AreEqual(new Rectangle(0, 0, 1200, 1920), result.DstRect);
            Assert.AreEqual(new Rectangle(0, 0, 1200, 1920), result.SrcRect);

            // 2倍
            result = c.Calculate(1200 * 2, 1920 * 2);
            Assert.AreEqual(new Size(1200, 1920), result.Canvas);
            Assert.AreEqual(0.5f, result.Ratio);
            Assert.AreEqual(new Rectangle(0, 0, 1200, 1920), result.DstRect);
            Assert.AreEqual(new Rectangle(0, 0, 2400, 3840), result.SrcRect);

            // 0.5倍
            result = c.Calculate(1200 / 2, 1920 / 2);
            Assert.AreEqual(new Size(600, 960), result.Canvas);
            Assert.AreEqual(1f, result.Ratio);
            Assert.AreEqual(new Rectangle(0, 0, 600, 960), result.DstRect);
            Assert.AreEqual(new Rectangle(0, 0, 600, 960), result.SrcRect);

            // 9:16 ※画面サイズのアスペクト比(10:16)に合わせて余白を追加する
            result = c.Calculate(1080, 1920);
            Assert.AreEqual(new Size(1200, 1920), result.Canvas);
            Assert.AreEqual(1f, result.Ratio);
            Assert.AreEqual(new Rectangle(60, 0, 1080, 1920), result.DstRect);
            Assert.AreEqual(new Rectangle(0, 0, 1080, 1920), result.SrcRect);

            result = c.Calculate(1200, 1800);
            Assert.AreEqual(new Size(1200, 1920), result.Canvas);
            Assert.AreEqual(1f, result.Ratio);
            Assert.AreEqual(new Rectangle(0, 60, 1200, 1800), result.DstRect);
            Assert.AreEqual(new Rectangle(0, 0, 1200, 1800), result.SrcRect);

            result = c.Calculate(1200, 1200);
            Assert.AreEqual(new Size(1200, 1920), result.Canvas);
            Assert.AreEqual(1f, result.Ratio);
            Assert.AreEqual(new Rectangle(0, 360, 1200, 1200), result.DstRect);
            Assert.AreEqual(new Rectangle(0, 0, 1200, 1200), result.SrcRect);

            result = c.Calculate(1000, 1000);
            Assert.AreEqual(new Size(1000, 1600), result.Canvas);
            Assert.AreEqual(1f, result.Ratio);
            Assert.AreEqual(new Rectangle(0, 300, 1000, 1000), result.DstRect);
            Assert.AreEqual(new Rectangle(0, 0, 1000, 1000), result.SrcRect);

            result = c.Calculate(2000, 2000);
            Assert.AreEqual(new Size(1200, 1920), result.Canvas);
            Assert.AreEqual(0.6f, result.Ratio);
            Assert.AreEqual(new Rectangle(0, 360, 1200, 1200), result.DstRect);
            Assert.AreEqual(new Rectangle(0, 0, 2000, 2000), result.SrcRect);

            result = c.Calculate(500, 500);
            Assert.AreEqual(new Size(500, 800), result.Canvas);
            Assert.AreEqual(1f, result.Ratio);
            Assert.AreEqual(new Rectangle(0, 150, 500, 500), result.DstRect);
            Assert.AreEqual(new Rectangle(0, 0, 500, 500), result.SrcRect);
        }

        [TestMethod]
        public void TestMethodFilename()
        {
            var r = Util.GetTitle(@"c:\d\e\f.jpg");

            Assert.AreEqual(@"e\f", r);
        }
    }
}