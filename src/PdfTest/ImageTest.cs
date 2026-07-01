using OpenCvSharp;
using PdfApp;

namespace PdfTest
{
    public class ImageTest : IClassFixture<Context>
    {
        Context _ctx;

        public ImageTest(Context ctx)
        {
            this._ctx = ctx;
        }

        [Fact]
        public void DetectCircle()
        {
            var fpdf = _ctx.FileNames[0];
            var fimg = _ctx.Config.ImageFiles[0];
            MainUtility.ShowMarkups(fpdf, fimg);
        }
    }
}
