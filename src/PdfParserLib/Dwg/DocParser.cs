using System.Text.RegularExpressions;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;

namespace PdfParserLib.Dwg
{
    public class DocParser : IDocParser
    {
        public string Name { get; set; } = null!;

        virtual public List<string> GetTags(IEnumerable<TextBlock> txtBlocks, IEnumerable<string> patterns)
        {
            var words = PdfTextUtility2.GetWordText(txtBlocks);
            var tags = MatchWords(words, patterns);
            return tags;
        }
        virtual public List<string> GetTitles(IEnumerable<TextBlock> txtBlocks, PdfRectangle bound)
        {
            var block = PdfTextUtility2.SelectTextBlock(txtBlocks, bound);
            return block.Select(b => b.Text).ToList();
        }

        virtual public List<string> GetDrawingNo(IEnumerable<TextBlock> txtBlocks, PdfRectangle bound)
        {
            var block = PdfTextUtility2.SelectTextBlock(txtBlocks, bound);
            return block
                .OrderBy(b => b.BoundingBox.Left)
                .Select(b => b.Text)
                .ToList();
        }

        virtual public List<DocRevision> GetRevHistory(IEnumerable<TextBlock> txtBlocks, PdfRectangle bound)
        {
            var blocks = PdfTextUtility2.SelectTextBlock(txtBlocks, bound);
            var revLines = blocks
                .GroupBy(b => (int)Math.Floor(b.BoundingBox.Bottom))
                .Select(g => g
                    .OrderBy(b => b.BoundingBox.Left)
                    .Select(b => b.Text)
                    .ToList()
                )
                .ToList();

            var revs = revLines
                .Select(rl => new DocRevision
                {
                    Issue = rl[0],
                    Rev = rl[1],
                    IssueDate = rl[2],
                    Desc = string.Join(" ", rl[3..^3]),
                    IssuedBy = rl[^3],
                    CheckedBy = rl[^2],
                    ApprovedBy = rl[^1]
                })
                .ToList();

            return revs;
        }

        virtual protected List<string> MatchWords(IEnumerable<string> words, IEnumerable<string> patterns)
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

        private IEnumerable<(string Word, string Tag)> MatchWords(
            Regex regex, IEnumerable<string> words) =>
                words
                    .Where(w => regex.IsMatch(w))
                    .SelectMany(w => regex.Matches(w))
                    .Select(m => (m.Groups[0].Value, m.Groups[1].Value))
                    .Distinct()
                    .ToList();


    }

}
