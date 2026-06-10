using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;

namespace PdfParserLib
{
    public static class PdfTextExtractor
    {
        #region Domain

        public class TextBlock
        {
            public string Text { get; set; } = null!;
            public List<Letter> Letters { get; set; } = null!;
            public PdfRectangle BoundingBox { get; set; }

            public double CenterX => (BoundingBox.Left + BoundingBox.Right) / 2.0;
        }

        public class TextLine
        {
            public List<TextBlock> Blocks { get; set; } = new();

            public List<Letter> Letters { get; set; } = null!;
            public PdfRectangle BoundingBox { get; set; }
            public TextOrientation Direction { get; set; }

            // Optional: reconstructed text with spacing
            public string Text =>
                Blocks != null && Blocks.Count > 0
                    ? string.Join("  |  ", Blocks.Select(b => b.Text))
                    : string.Empty;
        }

        public class TextColumn
        {
            public List<TextBlock> Blocks { get; set; } = new();
            public double MeanX { get; set; }
        }

        #endregion

        public static List<TextLine> ExtractLinesRobust(Page page)
        {
            var letters = page.Letters;

            var groups = letters.GroupBy(l => l.TextOrientation);

            var result = new List<TextLine>();

            foreach (var group in groups)
            {
                var direction = group.Key;
                var lines = BuildLinesForDirection(group.ToList(), direction);
                result.AddRange(lines);
            }

            return result;
        }

        private static List<TextLine> BuildLinesForDirection(
            List<Letter> letters, TextOrientation direction)
        {
            var projected = letters
                .Select(l => new
                {
                    Letter = l,
                    X = GetProjectedX(l, direction),
                    Y = GetProjectedCenterY(l, direction),
                    Width = GetProjectedWidth(l, direction),
                    Height = GetProjectedHeight(l, direction)
                })
                .ToList();

            // Cluster into lines
            double medianHeight = projected
                .Select(p => p.Height)
                .OrderBy(h => h)
                .ElementAt(projected.Count / 2);

            // tighter than before
            double lineTolerance = medianHeight * 0.5;

            //bool SameLine(a, b) =>
            //    Math.Abs(a.CenterY - b.CenterY) < tolerance &&
            //    Overlap(a, b) > 0.3;


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

                var lettersInLine = sorted.Select(p => p.Letter).ToList();

                string text = BuildLineTextWithBlocks(lettersInLine, direction);

                result.Add(new TextLine
                {
                    //Text = text,
                    Letters = lettersInLine,
                    BoundingBox = BoundingBox(lettersInLine),
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

        #region Utility

        private static double GetProjectedX(Letter l, TextOrientation dir)
        {
            var r = l.BoundingBox;

            return dir switch
            {
                TextOrientation.Horizontal => r.Left,
                TextOrientation.Rotate180 => -r.Right,
                TextOrientation.Rotate90 => r.Bottom,
                TextOrientation.Rotate270 => -r.Top,
                _ => r.Left
            };
        }

        private static double GetProjectedWidth(Letter l, TextOrientation dir)
        {
            var r = l.BoundingBox;

            return (dir == TextOrientation.Horizontal || dir == TextOrientation.Rotate180)
                ? r.Width
                : r.Height;
        }

        private static double GetProjectedHeight(Letter l, TextOrientation dir)
        {
            var r = l.BoundingBox;

            return (dir == TextOrientation.Horizontal || dir == TextOrientation.Rotate180)
                ? r.Height
                : r.Width;
        }

        private static PdfRectangle BoundingBox(List<Letter> letters)
        {
            double left = letters.Min(l => l.BoundingBox.Left);
            double right = letters.Max(l => l.BoundingBox.Right);
            double top = letters.Max(l => l.BoundingBox.Top);
            double bottom = letters.Min(l => l.BoundingBox.Bottom);

            return new PdfRectangle(left, bottom, right, top);
        }

        public static double GetProjectedCenterY(Letter l, TextOrientation dir)
        {
            var r = l.BoundingBox;

            double centerY = (r.Top + r.Bottom) / 2.0;

            return dir switch
            {
                TextOrientation.Horizontal => centerY,
                TextOrientation.Rotate180 => -centerY,
                TextOrientation.Rotate90 => r.Left + r.Width / 2.0,
                TextOrientation.Rotate270 => -(r.Left + r.Width / 2.0),
                _ => centerY
            };
        }

        private static double GetColumnTolerance(TextBlock block)
        {
            double width = block.BoundingBox.Width;

            return Math.Max(width * 1.5, 10);
        }

        #endregion

        public static string BuildLineTextWithBlocks(
            List<Letter> letters,
            TextOrientation direction)
        {
            if (letters.Count == 0) return string.Empty;

            var items = letters.Select(l => new
            {
                Letter = l,
                X = GetProjectedX(l, direction),
                Width = GetProjectedWidth(l, direction)
            }).ToList();

            // Median width (robust baseline)
            var widths = items.Select(i => i.Width).OrderBy(w => w).ToList();
            double medianWidth = widths[widths.Count / 2];

            // Thresholds (tunable)
            //double wordThreshold = medianWidth * 0.6;
            //double blockThreshold = medianWidth * 2.5;
            double wordThreshold = medianWidth * 0.5;
            double blockThreshold = medianWidth * 2.0;


            var sb = new StringBuilder();
            sb.Append(items[0].Letter.Value);


            for (int i = 1; i < items.Count; i++)
            {
                var prev = items[i - 1];
                var curr = items[i];

                double gap = curr.X - prev.X - prev.Width;

                if (gap > blockThreshold)
                {
                    // LOCK separator (strong separation)
                    sb.Append("  |  "); // or "\t" or " | "
                }
                else if (gap > wordThreshold)
                {
                    // WORD separator
                    sb.Append(" ");
                }

                sb.Append(curr.Letter.Value);
            }

            return sb.ToString();
        }


        public static List<TextColumn> BuildColumns(List<TextLine> lines)
        {
            var allBlocks = lines.SelectMany(l => l.Blocks).ToList();

            var columns = new List<TextColumn>();

            foreach (var block in allBlocks)
            {
                var col = columns.FirstOrDefault(c =>
                    Math.Abs(c.MeanX - block.CenterX) < GetColumnTolerance(block));

                if (col == null)
                {
                    col = new TextColumn
                    {
                        MeanX = block.CenterX
                    };
                    columns.Add(col);
                }

                col.Blocks.Add(block);

                // Update column center (running average)
                col.MeanX = col.Blocks.Average(b => b.CenterX);
            }

            // Sort columns left → right
            return columns.OrderBy(c => c.MeanX).ToList();
        }

        public static List<List<TextBlock>> AlignLinesToColumns(
            List<TextLine> lines,
            List<TextColumn> columns)
        {
            var result = new List<List<TextBlock>>();

            foreach (var line in lines)
            {
                var aligned = new List<TextBlock>();

                foreach (var col in columns)
                {
                    var block = line.Blocks.FirstOrDefault(b =>
                        Math.Abs(b.CenterX - col.MeanX) < GetColumnTolerance(b));

                    aligned.Add(block); // may be null
                }

                result.Add(aligned);
            }

            return result;
        }
    }
}
