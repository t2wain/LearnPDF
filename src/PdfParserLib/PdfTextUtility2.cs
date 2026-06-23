using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;


namespace PdfParserLib
{
    public static class PdfTextUtility2
    {
        public static IReadOnlyList<TextBlock> BuildTextBlockFromWord(IEnumerable<Word> words)
        {
            var pageSegmenter = new DocstrumBoundingBoxes();

            var hw = words
                .Where(w => w.TextOrientation == TextOrientation.Horizontal)
                .ToList();
            var hb = pageSegmenter.GetBlocks(hw);

            IEnumerable<Word> vw = words
                .Where(w => w.TextOrientation != TextOrientation.Horizontal)
                .ToList();

            var letters = vw.SelectMany(w => w.Letters).ToList();
            IEnumerable<Word> vw2 = RebuildWords(letters);
            var vb = pageSegmenter.GetBlocks(vw2);

            return hb.Concat(vb).ToList();
        }

        static IEnumerable<Word> RebuildWords(IReadOnlyList<Letter> letters)
        {
            var wordExtractor = new NearestNeighbourWordExtractor(
                //new NearestNeighbourWordExtractor.NearestNeighbourWordExtractorOptions
                //{
                //    MaximumDistance = (l1, l2) => 2,   // 👈 controls letter-to-letter grouping
                //}
            );

            var words = wordExtractor.GetWords(letters);

            return words;
        }

        public static List<TextBlock> SelectTextBlock(IEnumerable<TextBlock> txtBlocks,
           double? fromX, double? toX, double? fromY, double? toY)
        {
            var minX = fromX ?? double.MinValue;
            var minY = fromY ?? double.MinValue;
            var maxX = toX ?? double.MaxValue;
            var maxY = toY ?? double.MaxValue;
            var rect = new PdfRectangle(minX, minY, maxX, maxY);
            return SelectTextBlock(txtBlocks, rect);
        }

        public static List<TextBlock> SelectTextBlock(IEnumerable<TextBlock> txtBlocks, PdfRectangle rectangle)
        {
            var q = txtBlocks
                .Where(tb => RectUtility.Intersects(tb.BoundingBox, rectangle));

            var blocks = q.ToList();
            return blocks;
        }

        public static List<(Page Page, IEnumerable<Word> Words)> GetPdfWordFromFile(string fileName)
        {
            using PdfDocument doc = PdfUtility.GetPdfDocument(fileName);
            return doc.GetPages().Select(p => (Page : p, Words : p.GetWords())).ToList();
        }

        public static List<string> GetWordText(IEnumerable<TextBlock> txtBlocks) =>
            txtBlocks
                .SelectMany(b => b.TextLines.SelectMany(l => l.Words))
                .Select(GetWordText)
                .ToList();

        public static string GetWordText(Word word)
        {
            string t = word.TextOrientation switch
            {
                TextOrientation.Rotate270 => string.Join("", word.Text.Reverse()),
                _ => word.Text,
            };
            return t;
        }

        public static string GetLineText(TextLine txtLine)
        {
            string t = txtLine.TextOrientation switch
            {
                TextOrientation.Rotate270 => string.Join(" ", 
                    txtLine.Words
                        .Select(w => GetWordText(w))
                ),
                _ => txtLine.Text,
            };
            return t;
        }

    }
}
