using OpenCvSharp;
using PdfParserLib;
using PdfParserLib.Config;
using PdfParserLib.Dwg;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;

namespace PdfTest
{
    public class DwgTest : IClassFixture<Context>
    {
        Context _ctx;
        DwgConfig _dwgCfg;
        IDictionary<string, DwgRegion> _regions;
        IDocParser _docParser;

        public DwgTest(Context ctx)
        {
            this._ctx = ctx;
            _dwgCfg = _ctx.GetDwgConfig();
            _regions = _dwgCfg.Regions.ToDictionary(r => r.Name!);
            _docParser = _ctx.GetDocParser();
        }

        [Fact]
        public void GetDoc()
        {
            var dwg = _ctx.GetDrawing();
            var docs = dwg.ParseDoc();
        }

        [Fact]
        public void GetDwgNo()
        {
            var b = GetBound("DwgNo");
            var blocks = GetTextBlocks();
            var di = _docParser.GetDrawingNo(blocks, b);
        }

        [Fact]
        public void GetTitleBlock()
        {
            var b = GetBound("Title");
            var blocks = GetTextBlocks();
            var titles = _docParser.GetTitles(blocks, b);
        }

        [Fact]
        public void GetRevHist()
        {
            var b = GetBound("RevHist");
            var blocks = GetTextBlocks();
            var revHist = _docParser.GetRevHistory(blocks, b);
        }

        [Fact]
        public void GetTag()
        {
            var words = PdfTextUtility.GetPdfWordFromFile(_dwgCfg.DwgFiles[0]).First().Words;
            var blocks = PdfTextUtility.BuildTextBlockFromWord(words);
            var tags = _docParser.GetTags(blocks, _dwgCfg.TagPatterns);
        }

        PdfRectangle GetBound(string regionName)
        {
            var r = _regions[regionName];
            return RectUtility.BuildRectangle(r);
        }

        IEnumerable<TextBlock> GetTextBlocks() =>
            _ctx.GetBlocks(_dwgCfg.DwgFiles[0]);
    }
}
