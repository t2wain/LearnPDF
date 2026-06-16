using PdfParserLib;
using PdfParserLib.Entity;
using System.Text.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

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
        public void ExtractEquipmentTag()
        {
            string fileName = _ctx.FileNames[1];
            IEnumerable<string> tags = PdfDrawingUtility.ExtractTagFromFile(
                fileName, _ctx.Config.TagPatterns);
        }

        [Fact]
        public void ExtractEquipmentTag2()
        {
            IEnumerable<PdfDrawingUtility.TagInDocument> docs = 
                PdfDrawingUtility.ExtractTagFromFile(_ctx.FileNames, _ctx.Config.TagPatterns);
            var tags = docs
                .SelectMany(d => d.Tags)
                .Distinct()
                .OrderBy(t => t)
                .ToList();
        }

        [Fact]
        public void FindDwgGrid()
        {
            string fileName = _ctx.FileNames[1];
            IEnumerable<PdfTextUtility.TextLine> lines = PdfTextUtility.ExtractText(fileName);

            var grid = PdfDrawingUtility.GetGridLabel(lines);
            
            lines = lines.Where(l => l.Direction == TextOrientation.Horizontal).ToList();
            var block = PdfDrawingUtility.GetTextBlock(lines, grid, "K", null, null, "9");
            var block2 = PdfDrawingUtility.GetTextBlock(lines, 2025, 2200, 100, 200);
            var block3 = PdfDrawingUtility.GetTextBlock(lines, 2025, 2200, 40, 65);
        }

        [Fact]
        public void FindDwgInfo()
        {
            string fileName = _ctx.FileNames[4];
            IEnumerable<PdfTextUtility.TextLine> lines = PdfTextUtility.ExtractText(fileName);
            lines = lines.Where(l => l.Direction == TextOrientation.Horizontal).ToList();
            var titles = PdfDrawingUtility.GetTitleBlock(lines);
            var info = PdfDrawingUtility.GetDrawingNo(lines);
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