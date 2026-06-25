using PdfParserLib;

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
    }
}
