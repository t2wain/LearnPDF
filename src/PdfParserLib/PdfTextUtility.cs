using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace PdfParserLib
{
    public static class PdfTextUtility
    {
        #region Word

        /// <summary>
        /// Get all words from PDF document
        /// </summary>
        public static List<(Page Page, IEnumerable<Word> Words)> GetPdfWordFromFile(string fileName)
        {
            using PdfDocument doc = PdfUtility.GetPdfDocument(fileName);
            return doc.GetPages()
                .Select(p => (
                    Page : p, 
                    Words : p.GetWords().ToList().AsEnumerable()
                ))
                .ToList();
        }

        /// <summary>
        /// Group letters into words
        /// </summary>
        public static IEnumerable<Word> RebuildWords(IReadOnlyList<Letter> letters)
        {
            var wordExtractor = new NearestNeighbourWordExtractor(
                //new NearestNeighbourWordExtractor.NearestNeighbourWordExtractorOptions
                //{
                //    MaximumDistance = (l1, l2) => 2,   // controls letter-to-letter grouping
                //}
            );

            var words = wordExtractor.GetWords(letters);

            return words;
        }

        #endregion

        #region TextBlock

        public static IEnumerable<TextBlock> GetTextBlock(string fileName)
        {
            var words = GetPdfWordFromFile(fileName)
                .SelectMany(i => i.Words)
                .ToList();
            return BuildTextBlockFromWord(words);
        }

        /// <summary>
        /// Group words into text blocks using DocstrumBoundingBoxes 
        /// and algorithm NearestNeighbourWordExtractor algorithms
        /// </summary>
        public static List<TextBlock> BuildTextBlockFromWord(IEnumerable<Word> words)
        {
            var pageSegmenter = new DocstrumBoundingBoxes();

            // build blocks from horizontal words
            var hw = words
                .Where(w => w.TextOrientation == TextOrientation.Horizontal)
                .ToList();
            var hb = pageSegmenter.GetBlocks(hw);

            // build blocks from vertical words
            IEnumerable<Word> vw = words
                .Where(w => w.TextOrientation != TextOrientation.Horizontal)
                .ToList();

            var letters = vw.SelectMany(w => w.Letters).ToList();
            IEnumerable<Word> vw2 = RebuildWords(letters);
            var vb = pageSegmenter.GetBlocks(vw2);

            // combine the blocks
            return hb.Concat(vb).ToList();
        }

        #endregion

        #region SelectTextBox

        /// <summary>
        /// Select text blocks that intersect the specified boundary
        /// </summary>
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

        /// <summary>
        /// Select text blocks that intersect the specified boundary
        /// </summary>
        public static List<TextBlock> SelectTextBlock(IEnumerable<TextBlock> txtBlocks, PdfRectangle rectangle)
        {
            var q = txtBlocks
                .Where(tb => RectUtility.Intersects(tb.BoundingBox, rectangle));

            var blocks = q.ToList();
            return blocks;
        }

        #endregion

        #region GetWordText

        /// <summary>
        /// Convenience method to get the list of text from words. The 
        /// words built using NearestNeighbourWordExtractor algorithm
        /// are inherently in reverse order.
        /// </summary>
        public static List<string> GetWordText(IEnumerable<TextBlock> txtBlocks) =>
            txtBlocks
                .SelectMany(b => b.TextLines.SelectMany(l => l.Words))
                .Select(GetWordText)
                .ToList();


        /// <summary>
        /// Convenience method to get the text from word. The 
        /// words built using NearestNeighbourWordExtractor algorithm
        /// are inherently in reverse order.
        /// </summary>
        public static string GetWordText(Word word)
        {
            string t = word.TextOrientation switch
            {
                TextOrientation.Rotate270 => string.Join("", word.Text.Reverse()),
                _ => word.Text,
            };
            return t;
        }

        /// <summary>
        /// Convenience method to get the lines of text from words. The 
        /// words built using NearestNeighbourWordExtractor algorithm
        /// are inherently in reverse order.
        /// </summary>
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

        #endregion

    }
}
