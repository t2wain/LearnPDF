using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;

namespace PdfParserLib
{
    public static class PdfTextUtility
    {
        public static double LineToleranceAdj { get; set; } = 1.5;
        public static double WordWidthToleranceAdj { get; set; } = 2.3;
        public static double WordBlockWidthToleranceAdj { get; set; } = 2.5;
        public static string BlockDelimiter { get; set; } = "::::";

        private class TextLine
        {
            public string Text { get; set; } = null!;
            public List<Word> Words { get; set; } = [];
            public PdfRectangle BoundingBox { get; set; }
            public TextOrientation Direction { get; set; }
        }

        public static List<string> ExtractText(Page page)
        {
            List<string> res = new();

            IEnumerable<Word> allWord = page.GetWords();

            var groups = allWord.GroupBy(l => l.TextOrientation);

            foreach (var group in groups)
            {
                var direction = group.Key;
                var words = group.ToList();
                if (direction == TextOrientation.Horizontal)
                {
                    res.AddRange(words.Select(w => w.Text));
                }
                else
                {
                    var lines = BuildLinesForDirection(words, direction);
                    res.AddRange(lines.Select(l => l.Text));
                }
            }
            return res;
        }

        /// <summary>
        /// Align embedded words into line based on X coordinate
        /// </summary>
        private static List<TextLine> BuildLinesForDirection(
            List<Word> words, TextOrientation direction)
        {
            var projected = words
                .Select(w => new
                {
                    Word = w,
                    X = GetProjectedX(w.BoundingBox, direction),
                    Y = GetProjectedY(w.BoundingBox, direction),
                    Width = GetProjectedWidth(w.BoundingBox, direction),
                    Height = GetProjectedHeight(w.BoundingBox, direction)
                })
                .ToList();

            double medianHeight = projected
                .Select(p => p.Height)
                .OrderBy(h => h)
                .ElementAt(projected.Count / 2);

            // tighter than before
            double lineTolerance = medianHeight * LineToleranceAdj;

            // Group a list of words into lines of text based
            // on the Y coordinate within a certain Y range tolerance
            var lines = ClusterBy(projected, p => p.Y, lineTolerance);

            var result = new List<TextLine>();

            foreach (var line in lines)
            {
                // Sort according to reading direction
                var sorted = line.OrderBy(p => p.X).ToList();

                // Handle reversed directions (180°, 270°)
                if (direction == TextOrientation.Rotate180 ||
                    direction == TextOrientation.Rotate270)
                {
                    sorted.Reverse();
                }

                var wordsInLine = sorted.Select(p => p.Word).ToList();

                string text = BuildLineTextWithBlocks(wordsInLine, direction);

                result.Add(new TextLine
                {
                    Text = text,
                    Words = wordsInLine,
                    //BoundingBox = BoundingBox(lettersInLine),
                    Direction = direction
                });
            }

            return result;
        }

        /// <summary>
        /// Group a list of items based on differences between 
        /// their value that is <= tolerance
        /// </summary>
        /// <typeparam name="T">Item</typeparam>
        /// <param name="items">List of items</param>
        /// <param name="selector">return a value for grouping</param>
        /// <param name="tolerance">The difference between their values <= tolerance</param>
        /// <returns></returns>
        private static List<List<T>> ClusterBy<T>(
            List<T> items,
            Func<T, double> selector,
            double tolerance)
        {
            // sorted words by Y coordinate
            List<T> sorted = items.OrderBy(selector).ToList();

            var clusters = new List<List<T>>();

            foreach (var item in sorted)
            {
                // return Y coordinate
                double value = selector(item);

                // find the first line that the word
                // might belong to based on Y coordnate
                // within a Y range tolerance
                var cluster = clusters.FirstOrDefault(c =>
                    Math.Abs(selector(c[0]) - value) < tolerance);

                if (cluster == null)
                {
                    // create new line
                    cluster = new List<T>();
                    clusters.Add(cluster);
                }

                // found exsiting line
                cluster.Add(item);
            }

            return clusters;
        }

        /// <summary>
        /// Add separator for block of text within line.
        /// A line of text might consist of block of text 
        /// separated by wide space.
        /// </summary>
        /// <param name="words">Line of words</param>
        /// <param name="direction">Text orientation</param>
        /// <returns>Concatenated words of the line with delimeter 
        /// separate each words and each block of words</returns>
        private static string BuildLineTextWithBlocks(
            List<Word> words,
            TextOrientation direction)
        {
            var items = words.Select(w => new
            {
                Word = w,
                X = GetProjectedX(w.BoundingBox, direction),
                Width = GetProjectedWidth(w.BoundingBox, direction)
            }).ToList();

            // Median width (robust baseline)
            var widths = items.Select(i => i.Width).OrderBy(w => w).ToList();
            double medianWidth = widths[widths.Count / 2];

            // Thresholds (tunable)
            double wordThreshold = medianWidth * WordWidthToleranceAdj;
            double blockThreshold = medianWidth * WordBlockWidthToleranceAdj;

            var sb = new StringBuilder();
            sb.Append(items[0].Word.Text);

            for (int i = 1; i < items.Count; i++)
            {
                var prev = items[i - 1]; // previous word
                var curr = items[i]; // current word

                // calculate the space between the words
                double gap = Math.Abs(curr.X - prev.X - prev.Width);

                if (gap > blockThreshold)
                {
                    // add delimiter between blocks of text
                    sb.Append(BlockDelimiter);
                }
                else if (gap > wordThreshold)
                {
                    // add delimiter between words
                    sb.Append(" ");
                }

                sb.Append(curr.Word.Text);
            }

            return sb.ToString();
        }

        #region Utility

        /// <summary>
        /// Get X coordinate
        /// </summary>
        private static double GetProjectedX(PdfRectangle rect, TextOrientation dir) =>
            dir switch
            {
                TextOrientation.Horizontal => rect.Left,
                TextOrientation.Rotate180 => -rect.Right,
                TextOrientation.Rotate90 => rect.Bottom,
                TextOrientation.Rotate270 => -rect.Top,
                _ => rect.Left
            };

        /// <summary>
        /// Get Y coordinate
        /// </summary>
        public static double GetProjectedY(PdfRectangle rect, TextOrientation dir)
        {
            double centerY = (rect.Top + rect.Bottom) / 2.0;

            return dir switch
            {
                TextOrientation.Horizontal => centerY,
                TextOrientation.Rotate180 => -centerY,
                TextOrientation.Rotate90 => rect.Left + rect.Width / 2.0,
                TextOrientation.Rotate270 => -(rect.Left + rect.Width / 2.0),
                _ => centerY
            };
        }

        /// <summary>
        /// Get width
        /// </summary>
        private static double GetProjectedWidth(PdfRectangle rect, TextOrientation dir) =>
            dir switch
            {
                TextOrientation.Horizontal or TextOrientation.Rotate180 => rect.Width,
                _ => rect.Height

            };

        /// <summary>
        /// Get height
        /// </summary>
        private static double GetProjectedHeight(PdfRectangle rect, TextOrientation dir) =>
            dir switch
            {
                TextOrientation.Horizontal or TextOrientation.Rotate180 => rect.Height,
                _ => rect.Width
            };

        #endregion

    }
}
