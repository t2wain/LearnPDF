using ImageParserLib;

namespace PdfTest
{
    public class OcrTest : IClassFixture<Context>
    {
        Context _ctx;

        public OcrTest(Context ctx)
        {
            this._ctx = ctx;
        }

        [Fact]
        public void OcrText()
        {
            using var engine =  OcrUtility.Create(_ctx.Config.OcrTrainedDataFolderPath);
            string imageFile = _ctx.Config.ImageFiles[0];
            var elements = OcrUtility.ExtractOcrText(imageFile, engine);
        }
    }
}
