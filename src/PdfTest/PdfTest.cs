using PdfParserLib;
using PdfParserLib.Entity;
using System.Text.Json;
using UglyToad.PdfPig;

namespace PdfTest
{
    public class PdfTest : IClassFixture<Context>
    {
        Context _ctx;

        public PdfTest(Context ctx)
        {
            this._ctx = ctx;
        }

        [Fact]
        public void GetWords()
        {
            string fileName = _ctx.FileNames[0];
            var lst = PdfTextUtility.GetPdfWordFromFile(fileName);
            foreach (var (page, words) in lst)
            {
                var blocks = PdfTextUtility.BuildTextBlockFromWord(words);
                var lines = blocks.Select(b => new
                {
                    Lines = b.TextLines
                        .Select(PdfTextUtility.GetLineText)
                        .ToList(),
                    Bound = b.BoundingBox
                }
                ).ToList();

                var lines2 = lines
                    .Where(l => l.Lines.Count == 2)
                    .ToList();
            }
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
