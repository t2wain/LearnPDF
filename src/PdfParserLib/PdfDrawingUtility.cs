using System.Text.RegularExpressions;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;

namespace PdfParserLib
{
    public static class PdfDrawingUtility
    {
        #region Entity

        public record GridLabel
        {
            public Dictionary<string, double> XLabel { get; set; } = [];
            public Dictionary<string, double> YLabel { get; set; } = [];

            public double? GetX(string label) => 
                XLabel.TryGetValue(label, out double x) ? x : null;

            public double? GetY(string label) =>
                YLabel.TryGetValue(label, out double y) ? y : null;

            public (double? X, double? Y) GetCoord(string xLabel, string yLabel) =>
                (GetX(xLabel), GetY(yLabel));
        }

        #endregion

        #region Tags

        public static List<string> ExtractTagFromFile(string fileName, IEnumerable<string> patterns)
        {
            IEnumerable<PdfTextUtility.PdfTextLine> lines = PdfTextUtility.ExtractText(fileName);
            return ExtractTag(lines, patterns);
        }

        public static List<string> ExtractTag(IEnumerable<PdfTextUtility.PdfTextLine> lines, IEnumerable<string> patterns)
        {
            List<string> eqTags = new();
            List<string> matchWords = new();
            var words = PdfTextUtility.GetWordFromLine(lines);
            return MatchWords(words, patterns);
        }

        public static List<string> ExtractTag(IEnumerable<Word> words, IEnumerable<string> patterns) =>
            MatchWords(words.Select(w => w.Text), patterns);

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

        public static GridLabel GetGridLabel(IEnumerable<PdfTextUtility.PdfTextLine> lines)
        {
            var grid = new GridLabel();

            var s = string.Join(PdfTextUtility.BlockDelimiter, ["A", "B", "C"]);
            var hlines = lines.Where(l => l.Direction == TextOrientation.Horizontal).ToList();

            if (hlines.FirstOrDefault(l => Regex.IsMatch(l.Text, s)) is PdfTextUtility.PdfTextLine hgrid)
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

        public static PdfRectangle GetBoundingBox(GridLabel grid, 
            string? fromX, string? toX, string? fromY, string? toY)
        {
            double? minX = fromX == null ? null : grid.GetX(fromX);
            double? maxX = toX == null ? null : grid.GetX(toX);
            double? minY = fromY == null ? null : grid.GetY(fromY);
            double? maxY = toY == null ? null : grid.GetY(toY);
            return new(
                minX ?? 0, 
                minY ?? 0, 
                maxX ?? double.MaxValue, 
                maxY ?? double.MaxValue
            ); 
        }

        public static List<PdfTextUtility.PdfTextBlock> GetTextBlock(
            IEnumerable<PdfTextUtility.PdfTextLine> lines,
            GridLabel grid, string? fromX, string? toX, string? fromY, string? toY)
        {
            double? fx = fromX == null ? null : grid.XLabel[fromX];
            double? tx = toX == null ? null : grid.XLabel[toX];
            double? fy = fromY == null ? null : grid.YLabel[fromY];
            double? ty = toY == null ? null : grid.YLabel[toY];
            return PdfTextUtility.GetTextBlock(lines, fx, tx, fy, ty);
        }
    }
}
