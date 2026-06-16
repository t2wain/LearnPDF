using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace PdfParserLib
{
    public static class PdfDrawingUtility
    {
        public record TagInDocument
        {
            public string FileName { get; set; } = null!;
            public List<string> Tags { get; set; } = [];
        }

        #region Tags

        public static List<TagInDocument> ExtractTagFromFile(
            IEnumerable<string> fileNames, IEnumerable<string> patterns) =>
                fileNames.Select(f => new TagInDocument()
                {
                    FileName = f,
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

    }
}
