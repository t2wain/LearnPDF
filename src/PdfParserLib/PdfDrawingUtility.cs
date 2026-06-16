using iText.Signatures.Validation.Events;
using System.Text.RegularExpressions;
using UglyToad.PdfPig.Content;

namespace PdfParserLib
{
    public static class PdfDrawingUtility
    {
        #region Entity

        public record GridLabel
        {
            public Dictionary<string, double> XLabel { get; set; } = [];
            public Dictionary<string, double> YLabel { get; set; } = [];

            public (double X, double Y) GetCoord(string xLabel, string yLabel) =>
                (XLabel.TryGetValue(xLabel, out double x) ? x : double.NaN,
                 YLabel.TryGetValue(yLabel, out double y) ? y : double.NaN);
        }

        public class DocInfo
        {
            public List<string> Title { get; set; } = [];
            public string ProjectNo { get; set; } = "";
            public string DrawingNo { get; set; } = "";
            public string RevNo { get; set; } = "";
            public List<string> Tags { get; set; } = [];
        }

        public static DocInfo ExtractDocInfo(string fileName, IEnumerable<string> tagPatterns)
        {
            var lines = PdfTextUtility.ExtractText(fileName);
            var title = GetTitleBlock(lines);
            var drawingNo = GetDrawingNo(lines)!;
            var tags = ExtractTagFromFile(fileName, tagPatterns);
            return new DocInfo()
            {
                Title = title,
                ProjectNo = drawingNo[1],
                DrawingNo = drawingNo[2],
                RevNo = drawingNo[3],
                Tags = tags
            };
        }

        #endregion

        #region Tags

        public static List<DocInfo> ExtractTagFromFile(
            IEnumerable<string> fileNames, IEnumerable<string> patterns) =>
                fileNames.Select(f => new DocInfo()
                {
                    DrawingNo = f,
                    Tags = ExtractTagFromFile(f, patterns)
                }).ToList();

        public static List<string> ExtractTagFromFile(string fileName, IEnumerable<string> patterns)
        {
            List<string> eqTags = new();
            List<string> matchWords = new();

            IEnumerable<PdfTextUtility.TextLine> lines = PdfTextUtility.ExtractText(fileName);
            var words = PdfTextUtility.GetWordFromLine(lines);

            return MatchWords(words, patterns);
        }

        public static List<string> MatchWords(IEnumerable<string> words, IEnumerable<string> patterns)
        {
            List<string> eqTags = new();
            List<string> matchWords = new();

            foreach (var pat in patterns)
            {
                Regex r = new(pat, RegexOptions.IgnoreCase);
                var matchTags = MatchWords(r, words);
                eqTags.AddRange(matchTags.Select(i => i.Tag));
                matchWords.AddRange(matchTags.Select(i => i.Word));
                words = words.Where(w => !matchWords.Contains(w)).ToList();
            }

            eqTags = eqTags
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            return eqTags;
        }

        public static IEnumerable<(string Word, string Tag)> MatchWords(
            Regex regex, IEnumerable<string> words) =>
                words
                    .Where(w => regex.IsMatch(w))
                    .SelectMany(w => regex.Matches(w))
                    .Select(m => (m.Groups[0].Value, m.Groups[1].Value))
                    .Distinct()
                    .ToList();

        #endregion

        public static GridLabel GetGridLabel(IEnumerable<PdfTextUtility.TextLine> lines)
        {
            var grid = new GridLabel();

            var s = string.Join(PdfTextUtility.BlockDelimiter, ["A", "B", "C"]);
            var hlines = lines.Where(l => l.Direction == TextOrientation.Horizontal).ToList();

            if (hlines.FirstOrDefault(l => Regex.IsMatch(l.Text, s)) is PdfTextUtility.TextLine hgrid)
            {
                foreach (var b in hgrid.Blocks)
                    grid.XLabel.TryAdd(b.Text, b.X);
            }
            ;

            s = $"{PdfTextUtility.BlockDelimiter}[\\d]+$";

            var ycoords = hlines
                .Where(l => Regex.IsMatch(l.Text, s))
                .Where(l => l.Blocks.Any(b => b.BoundingBox.Left > 2300))
                .SelectMany(l => l.Blocks)
                .Where(b => b.BoundingBox.Left > 2300)
                .ToList();

            foreach (var b in ycoords)
                grid.YLabel.TryAdd(b.Text, b.Y);

            return grid;
        }

        public static List<PdfTextUtility.TextBlock> GetTextBlock(
            IEnumerable<PdfTextUtility.TextLine> lines,
            GridLabel grid, string? fromX, string? toX, string? fromY, string? toY)
        {
            double? fx = fromX == null ? null : grid.XLabel[fromX];
            double? tx = toX == null ? null : grid.XLabel[toX];
            double? fy = fromY == null ? null : grid.YLabel[fromY];
            double? ty = toY == null ? null : grid.YLabel[toY];
            return GetTextBlock(lines, fx, tx, fy, ty);
        }

        public static List<PdfTextUtility.TextBlock> GetTextBlock(
            IEnumerable<PdfTextUtility.TextLine> lines,
           double? fromX, double? toX, double? fromY, double? toY)
        {
            var q = lines.SelectMany(l => l.Blocks);
            if (fromX != null)
                q = q.Where(b => b.X >= fromX);
            if (toX != null)
                q = q.Where(b => b.X <= toX);
            if (fromY != null)
                q = q.Where(b => b.Y >= fromY);
            if (toY != null)
                q = q.Where(b => b.Y <= toY);

            var blocks = q.ToList();
            return blocks;
        }

        public static List<string> GetTitleBlock(IEnumerable<PdfTextUtility.TextLine> lines)
        {
            var block = GetTextBlock(lines, 2025, 2200, 100, 200);
            return block.Select(b => b.Text).ToList();
        }

        public static List<string> GetDrawingNo(IEnumerable<PdfTextUtility.TextLine> lines)
        {
            var block = GetTextBlock(lines, 2025, 2200, 40, 65);
            List<string> result = [];
            if (block.FirstOrDefault() is PdfTextUtility.TextBlock b)
            {
                result = b.Text.Split(" ").ToList();
            }
            return result;
        }
    }
}
