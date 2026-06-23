using PdfParserLib;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis;

namespace PdfTest
{
    public class DwgTest : IClassFixture<Context>
    {
        Context _ctx;

        public DwgTest(Context ctx)
        {
            this._ctx = ctx;
        }

        [Fact]
        public void GetDwgNo()
        {
            var blocks = GetTextBlocks();
            var di = PdfDrawing2.GetDrawingNo(blocks);
        }

        [Fact]
        public void GetTitleBlock()
        {
            var blocks = GetTextBlocks();
            var titles = PdfDrawing2.GetTitleBlock(blocks);
        }

        [Fact]
        public void GetRevHist()
        {
            var blocks = GetTextBlocks();
            var revHist = PdfDrawing2.GetRevHistory(blocks);
        }

        IEnumerable<TextBlock> GetTextBlocks() =>
            GetTextBlocks(_ctx.FileNames[0]);

        IEnumerable<TextBlock> GetTextBlocks(string fileName) =>
            PdfTextUtility2.BuildTextBlockFromWord(GetWords(fileName));

        IEnumerable<Word> GetWords(string fileName) =>
            PdfTextUtility2.GetPdfWordFromFile(fileName).First().Words;

    }
}
