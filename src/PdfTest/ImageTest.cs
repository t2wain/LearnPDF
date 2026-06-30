using ImageParserLib;

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
            var f = _ctx.Config.ImageFiles[0];
            OpenCvUtility.DetectCircles(f);
        }
    }
}
