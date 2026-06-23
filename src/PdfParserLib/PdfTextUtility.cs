using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;

namespace PdfParserLib
{
    public static class PdfTextUtility
    {
        #region Parameters

        public static double LineToleranceAdj { get; set; } = 1.5;
        public static double WordWidthToleranceAdj { get; set; } = 2.3; // 2.3;
        public static double WordBlockWidthToleranceAdj { get; set; } = 2.5;
        public static string BlockDelimiter { get; set; } = "::::";

        #endregion

        #region Entity

        public record PdfTextBlock
        {
            public string Text { get; set; } = null!;
            public List<ProjectedWord> Words { get; set; } = [];
            public PdfRectangle BoundingBox { get; set; }
            public int LineNo { get; set; }
            public double X => BoundingBox.Left;
            public double Y => BoundingBox.Bottom;
        }

        public record PdfTextLine
        {
            public string Text { get; set; } = null!;
            public int Idx { get; set; }
            public List<ProjectedWord> Words { get; set; } = [];
            public TextOrientation Direction { get; set; }
            public List<PdfTextBlock> Blocks { get; set; } = [];
        }

        public record ProjectedWord
        {
            public Word? Word { get; set; }
            public int Idx { get; set; }
            public string Text { get; set; } = null!;
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public TextOrientation Direction { get; set; }
        }

        public record PdfTextColumn
        {
            public List<PdfTextBlock> Blocks { get; set; } = new();
            public double MeanX { get; set; }
        }

        #endregion

        public static List<PdfTextLine> ExtractText(string fileName)
        {
            IEnumerable<Word> allWord = GetPdfWordFromFile(fileName);
            var groups = allWord.GroupBy(l => l.TextOrientation);
            var lines = groups
                .SelectMany(g => BuildLinesForDirection(g.ToList(), g.Key))
                .ToList();
            return lines;
        }

        public static List<string> GetWordFromLine(IEnumerable<PdfTextLine> textLines)
        {
            IEnumerable<string> w1 = textLines
                .Where(l => l.Direction == TextOrientation.Horizontal)
                .SelectMany(l => l.Words)
                .Select(w => w.Text);

            IEnumerable<string> w2 = textLines
                .Where(l => l.Direction != TextOrientation.Horizontal)
                .SelectMany(l => l.Blocks)
                .Select(b => b.Text);

            return w1.Concat(w2).ToList();
        }

        /// <summary>
        /// Group the blocks of all the lines
        /// into columns based on the alignment of X coordinate.
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static List<PdfTextColumn> BuildColumns(IEnumerable<PdfTextLine> lines)
        {
            var allBlocks = lines
                .SelectMany(l =>
                    l.Blocks.Select((b, i) => b with { LineNo = i })
                )
                .ToList();

            var columns = new List<PdfTextColumn>();

            foreach (var block in allBlocks)
            {
                var col = columns.FirstOrDefault(c =>
                    Math.Abs(c.MeanX - block.X) < 20);

                if (col == null)
                {
                    col = new PdfTextColumn
                    {
                        MeanX = block.X
                    };
                    columns.Add(col);
                }

                col.Blocks.Add(block);

                // Update column center (running average)
                col.MeanX = col.Blocks.Average(b => b.X);
            }

            foreach (var col in columns)
            {
                col.Blocks = col.Blocks
                    .OrderBy(b => b.Y)
                    .ToList();
            }

            return columns;
        }

        public static List<PdfTextBlock> GetTextBlock(IEnumerable<PdfTextLine> lines,
           double? fromX, double? toX, double? fromY, double? toY)
        {
            var minX = fromX ?? double.MinValue;
            var minY = fromY ?? double.MinValue;
            var maxX = toX ?? double.MaxValue;
            var maxY = toY ?? double.MaxValue;
            var rect = new PdfRectangle(minX, minY, maxX, maxY);
            return GetTextBlock(lines, rect);
        }

        public static List<PdfTextBlock> GetTextBlock(IEnumerable<PdfTextLine> lines, PdfRectangle rectangle)
        {
            var q = lines
                .SelectMany(l => l.Blocks)
                .Where(tb => RectUtility.Intersects(tb.BoundingBox, rectangle));

            var blocks = q.ToList();
            return blocks;
        }


        #region Core Logic

        public static List<Word> GetPdfWordFromFile(string fileName)
        {
            using PdfDocument doc = PdfUtility.GetPdfDocument(fileName);
            return doc.GetPages().SelectMany(p => p.GetWords()).ToList();
        }

        public static List<ProjectedWord> GetProjectedWords(
            IEnumerable<Word> words, TextOrientation direction) =>
                words
                    .Select((w, i) => new ProjectedWord()
                    {
                        Word = w,
                        Idx = i,
                        Text = w.Text,
                        X = GetProjectedX(w.BoundingBox, direction),
                        Y = GetProjectedY(w.BoundingBox, direction),
                        Width = GetProjectedWidth(w.BoundingBox, direction),
                        Height = GetProjectedHeight(w.BoundingBox, direction),
                        Direction = direction
                    })
                    .ToList();

        /// <summary>
        /// Align embedded words into line based on X coordinate
        /// </summary>
        private static List<PdfTextLine> BuildLinesForDirection(
            IEnumerable<Word> words, TextOrientation direction)
        {
            var projected = GetProjectedWords(words, direction);

            double medianHeight = projected
                .Select(p => p.Height)
                .OrderBy(h => h)
                .ElementAt(projected.Count / 2);

            // tighter than before
            double lineTolerance = medianHeight * LineToleranceAdj;

            // Group a list of words into lines of text based
            // on the Y coordinate within a certain Y range tolerance
            if (direction != TextOrientation.Horizontal)
                projected = projected.OrderBy(p => p.Y).ToList();
            var lines = ClusterBy(projected, p => p.Y, lineTolerance);

            var result = new List<PdfTextLine>();

            int i = 0;
            foreach (var line in lines)
            {
                // Sort according to reading direction
                var sorted = line.OrderBy(p => p.X).ToList();

                PdfTextLine txtLine = direction switch
                {
                    TextOrientation.Horizontal => BuildHorizontalLine(sorted),
                    _ => BuildRotate270Line(sorted)
                };
                txtLine.Idx = ++i;
                foreach (var b in txtLine.Blocks)
                    b.LineNo = i;

                result.Add(txtLine);
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
            IEnumerable<T> items,
            Func<T, double> selector,
            double tolerance)
        {
            var clusters = new List<List<T>>();

            foreach (var item in items)
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
        /// Horizontal words are extracted from left-to-right. 
        /// Therefore, a line of text may actually consist of 
        /// columns of text block.
        /// </summary>
        private static PdfTextLine BuildHorizontalLine(List<ProjectedWord> words)
        {
            // Median width (robust baseline)
            var widths = words.Select(i => i.Width).OrderBy(w => w).ToList();
            double medianWidth = widths[widths.Count / 2];

            // Thresholds (tunable)
            double blockThreshold = medianWidth * WordBlockWidthToleranceAdj;

            var line = new PdfTextLine() { Direction = TextOrientation.Horizontal, Words = words };

            var blk = new PdfTextBlock();
            blk.Words.Add(words[0]);
            line.Blocks.Add(blk);

            for (int i = 1; i < words.Count; i++)
            {
                var prev = words[i - 1]; // previous word
                var curr = words[i]; // current word

                // calculate the space between the words
                double gap = curr.X - prev.X - prev.Width;

                if (gap > blockThreshold)
                {
                    // create new block
                    blk = new PdfTextBlock();
                    line.Blocks.Add(blk);
                }
                blk.Words.Add(curr);
            }

            var ltxt = line.Blocks.Select(b => string.Join(" ", b.Words.Select(w => w.Text)));
            line.Text = string.Join(BlockDelimiter, ltxt);

            foreach (var b in line.Blocks)
            {
                b.Text = string.Join(" ", b.Words.Select(w => w.Text).ToArray());
                b.BoundingBox = GetBoundingBox(b.Words);
            }

            return line;

        }

        /// <summary>
        /// For rotated words (90°, 270°), each extracted word has only 1 letter. 
        /// Therefore, actual words must be build from consecutive letters 
        /// based on Y coordinate of the document. A line of text may actually 
        /// consist of columns of text block.
        /// </summary>
        private static PdfTextLine BuildRotate270Line(List<ProjectedWord> words)
        {
            // Median width (robust baseline)
            var widths = words.Select(i => i.Width).OrderBy(w => w).ToList();
            double medianWidth = widths[widths.Count / 2];

            // Thresholds (tunable)
            double wordThreshold = medianWidth * WordWidthToleranceAdj;
            double blockThreshold = medianWidth * WordBlockWidthToleranceAdj;

            //wordThreshold = 13;

            var line = new PdfTextLine() { Direction = TextOrientation.Rotate270, Words = words };

            var sbLine = new StringBuilder();
            sbLine.Append(words[0].Text);

            var blk = new PdfTextBlock();
            blk.Words.Add(words[0]);
            line.Blocks.Add(blk);

            var sbBlock = new StringBuilder();
            sbBlock.Append(words[0].Text);

            for (int i = 1; i < words.Count; i++)
            {
                var prev = words[i - 1]; // previous word
                var curr = words[i]; // current word

                // calculate the space between the words
                double gap = Math.Abs(curr.X - prev.X - prev.Width);

                if (gap > blockThreshold)
                {
                    // add delimiter between blocks of text
                    sbLine.Append(BlockDelimiter);

                    // udate current block
                    blk.Text = sbBlock.ToString();
                    sbBlock.Clear();

                    // create new block
                    blk = new PdfTextBlock();
                    line.Blocks.Add(blk);
                }
                else if (gap > wordThreshold)
                {
                    // add delimiter between words
                    sbLine.Append(" ");
                    sbBlock.Append(" ");
                }

                sbLine.Append(curr.Text);

                blk.Words.Add(curr);
                sbBlock.Append(curr.Text);
            }


            line.Text = sbLine.ToString();

            foreach (var b in line.Blocks)
            {
                b.Text = string.Join("", b.Words.Select(w => w.Text).ToArray());
                b.BoundingBox = GetBoundingBox(b.Words);
            }

            return line;

        }

        #endregion

        #region Utility

        /// <summary>
        /// Get X coordinate
        /// </summary>
        private static double GetProjectedX(PdfRectangle rect, TextOrientation dir) =>
            dir switch
            {
                TextOrientation.Horizontal => rect.Left,
                TextOrientation.Rotate180 => rect.Right,
                TextOrientation.Rotate90 => rect.Bottom,
                TextOrientation.Rotate270 => rect.Top,
                _ => rect.Left
            };

        /// <summary>
        /// Get Y coordinate
        /// </summary>
        private static double GetProjectedY(PdfRectangle rect, TextOrientation dir)
        {
            double centerY = (rect.Top + rect.Bottom) / 2.0;

            return dir switch
            {
                TextOrientation.Horizontal or TextOrientation.Rotate180 => centerY,
                TextOrientation.Rotate90 or TextOrientation.Rotate270 => rect.Left + rect.Width / 2.0,
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

        private static PdfRectangle GetBoundingBox(IEnumerable<ProjectedWord> words)
        {
            var rects = words.Select(w => w.Word)
                .Where(w => w != null)
                .Select(w => w!.BoundingBox)
                .ToList();

            return RectUtility.GetEnclosingRectangle(rects);
        }

        #endregion

    }
}
