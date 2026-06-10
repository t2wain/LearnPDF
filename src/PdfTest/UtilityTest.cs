using PdfParserLib;
using PdfParserLib.Entity;
using System.Text.Json;
using System.Text.RegularExpressions;
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
            IEnumerable<string> words = PdfUtility.ExtractAllWords(fileName).Distinct().ToList();
        }

        [Fact]
        public void ExtractLineOfText()
        {
            string fileName = _ctx.FileNames[0];
            using PdfDocument doc = PdfUtility.GetPdfDocument(fileName);
            foreach (var p in doc.GetPages())
            {
                PdfTextExtractor2.ExtractText(p);
            }
        }

        [Fact]
        public void ExtractWordsWithLocation()
        {
            string fileName = _ctx.FileNames[0];

            var words = PdfUtility
                .ExtractAllWordsWithCoordinates(fileName)
                .ToList();

            var vert = words
                .Where(w => w.TextOrientation != "Horizontal")
                .ToList();

            var horiz = words
                .Where(w => w.TextOrientation == "Horizontal")
                .ToList();

        }

        [Fact]
        public void ExtractEquipmentTag()
        {
            string fileName = _ctx.FileNames[0];
            IEnumerable<string> words = PdfUtility.ExtractAllWords(fileName).Distinct().ToList();
            string p = _ctx.Config.TagPatterns[0];
            Regex r = new(_ctx.Config.TagPatterns[0], RegexOptions.IgnoreCase);
            words = words.Where(w => r.IsMatch(w)).ToList();
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

        [Fact]
        public void SaveSvgToHtml()
        {
            string fileName = _ctx.FileNames[0];
            using PdfDocument doc = PdfUtility.GetPdfDocument(fileName);

            PdfUtility.PdfExtractOptions options = new()
            {
                SaveData = true,
                SavePdfPath = true,
                SaveSvgCommand = true,
            };
            PdfDocData o = PdfUtility.ExploreDocument(doc, options);
            PdfPageData p = o.Pages.First();

            var paths = p.Paths.Where(p => p.SubPaths.Count > 2).ToList();
            var maxWidth = paths.Select(p => p.Bound!.Value.Width).Max();
            var maxHeight = paths.Select(p => p.Bound!.Value.Height).Max();

            //var svgs = ImageUtility.CreateSvgElement(p.Paths.Where(p => p.SubPaths.Count > 3), (int)p.Width, (int)p.Height);
            //ImageUtility.SaveSvgAsHtmlToFile(svgs, Path.Combine(_ctx.Config.ImageFolder, "svg.html"));
        }
    }
}