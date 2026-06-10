using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using static iText.IO.Util.IntHashtable;

namespace PdfParserLib
{
    public static class PdfTextExtractor2
    {
        public class TextLine
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

        public static List<TextLine> BuildLinesForDirection(
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

            // Cluster into lines
            double medianHeight = projected
                .Select(p => p.Height)
                .OrderBy(h => h)
                .ElementAt(projected.Count / 2);

            // tighter than before
            double lineTolerance = medianHeight * 1.5;

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

        private static List<List<T>> ClusterBy<T>(
            List<T> items,
            Func<T, double> selector,
            double tolerance)
        {
            List<T> sorted = items.OrderBy(selector).ToList();
            var clusters = new List<List<T>>();

            foreach (var item in sorted)
            {
                double value = selector(item);

                var cluster = clusters.FirstOrDefault(c =>
                    Math.Abs(selector(c[0]) - value) < tolerance);

                if (cluster == null)
                {
                    cluster = new List<T>();
                    clusters.Add(cluster);
                }

                cluster.Add(item);
            }

            return clusters;
        }


        public static string BuildLineTextWithBlocks(
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
            double wordThreshold = medianWidth * 2.3;
            double blockThreshold = medianWidth * 2.5;


            var sb = new StringBuilder();
            sb.Append(items[0].Word.Text);


            for (int i = 1; i < items.Count; i++)
            {
                var prev = items[i - 1];
                var curr = items[i];

                double gap = Math.Abs(curr.X - prev.X - prev.Width);

                if (gap > blockThreshold)
                {
                    // LOCK separator (strong separation)
                    sb.Append("::::"); // or "\t" or " | "
                }
                else if (gap > wordThreshold)
                {
                    // WORD separator
                    sb.Append(" ");
                }

                sb.Append(curr.Word.Text);
            }

            return sb.ToString();
        }


        #region Utility

        private static double GetProjectedX(PdfRectangle rect, TextOrientation dir) =>
            dir switch
            {
                TextOrientation.Horizontal => rect.Left,
                TextOrientation.Rotate180 => -rect.Right,
                TextOrientation.Rotate90 => rect.Bottom,
                TextOrientation.Rotate270 => -rect.Top,
                _ => rect.Left
            };

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


        private static double GetProjectedWidth(PdfRectangle rect, TextOrientation dir) =>
            dir switch
            {
                TextOrientation.Horizontal or TextOrientation.Rotate180 => rect.Width,
                _ => rect.Height

            };


        private static double GetProjectedHeight(PdfRectangle rect, TextOrientation dir) =>
            dir switch
            {
                TextOrientation.Horizontal or TextOrientation.Rotate180 => rect.Height,
                _ => rect.Width
            };

        #endregion

    }
}
