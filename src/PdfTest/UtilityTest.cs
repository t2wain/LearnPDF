using PdfParserLib;
using PdfParserLib.Entity;
using System.Text.Json;
using UglyToad.PdfPig;

namespace PdfTest
{
    public class UtilityTest : IClassFixture<Context>
    {
        Context _ctx;

        public UtilityTest(Context ctx)
        {
            this._ctx = ctx;
        }

        [Fact]
        public void ExtractWords()
        {
            string fileName = _ctx.FileNames[0];
            var words = PdfUtility.ExtractAllWords(fileName).Distinct().ToList();
        }

        [Fact]
        public void ExtractWordsWithLocation()
        {
            string fileName = _ctx.FileNames[0];

            var words = PdfUtility
                .ExtractAllWordsWithCoordinates(fileName)
                .Where(w => w.Bound.HasValue && w.TextOrientation == "Horizontal")
                .ToList();

            var bounds = words
                .Select(w => w.Bound!.Value)
                .ToList();
        }

        [Fact]
        public void SavePdfToImage()
        {
            string fileName = _ctx.FileNames[0];
            PdfUtility.ConvertPdfToPngImages(fileName, _ctx.ImageFolderPath);
        }

        [Fact]
        public void ExploreDocument()
        {
            string fileName = _ctx.FileNames[0];
            using PdfDocument doc = PdfUtility.GetPdfDocument(fileName);

            PdfUtility.PdfExtractOptions options = new()
            {
                SaveData = true,
                SavePdfPath = true,
                SaveSvgCommand = true,
                SavePngImage = true,
            };
            PdfDocData o = PdfUtility.ExploreDocument(doc, options);
            o.FileName = fileName;
            string json = JsonSerializer.Serialize(o);
        }
    }
}