using PdfParserLib;
using UglyToad.PdfPig.Content;

namespace PdfTest
{
    public class PdfTextTest : IClassFixture<Context>
    {
        Context _ctx;

        public PdfTextTest(Context ctx)
        {
            this._ctx = ctx;
        }

        [Fact]
        public void GetWords()
        {
            string fileName = _ctx.FileNames[0];
            List<Word> words = PdfTextUtility.GetPdfWordFromFile(fileName);

            var hw = words.Where(w => w.TextOrientation == TextOrientation.Horizontal).ToList();
            var phw = PdfTextUtility.GetProjectedWords(hw, TextOrientation.Horizontal);

            var vw = words.Where(w => w.TextOrientation == TextOrientation.Rotate270).ToList();
            var pvw = PdfTextUtility.GetProjectedWords(vw, TextOrientation.Rotate270)
                .OrderBy(w => Math.Round(w.Y,0))
                //.ThenBy(w => w.X)
                .ToList();
        }

        [Fact]
        public void ExtractLineOfText()
        {
            string fileName = _ctx.FileNames[0];
            List<PdfTextUtility.TextLine> lines = PdfTextUtility.ExtractText(fileName);

            var h = lines.Where(l => l.Direction == TextOrientation.Horizontal).ToList();
            var hl = h.Select(l => l.Text).ToList();

            var v = lines.Where(l => l.Direction == TextOrientation.Rotate270).ToList();
            var vl = v.Select(l => l.Text).ToList();
            var vtb = v.SelectMany(l => l.Blocks.Select(b => b.Text)).ToList();
        }

        [Fact]
        public void BuildTextBlock()
        {
            string fileName = _ctx.FileNames[0];
            List<PdfTextUtility.TextLine> lines = PdfTextUtility.ExtractText(fileName);
            var h = lines.Where(l => l.Direction == TextOrientation.Horizontal).ToList();
            IEnumerable<PdfTextUtility.TextColumn> cols = PdfTextUtility.BuildColumns(h);
            var cols2 = cols.OrderByDescending(tc => tc.Blocks.Count).ToList();
        }

    }
}
