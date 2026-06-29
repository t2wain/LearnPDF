using PdfParserLib.Config;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;

namespace PdfParserLib.Dwg
{
    public class DocParserInstr : IDocParser
    {
        public string Name { get; set; } = "";

        public DocInfo GetDrawingNo(IEnumerable<TextBlock> txtBlocks, PdfRectangle bound) => new();

        public List<DocRevision> GetRevHistory(IEnumerable<TextBlock> txtBlocks, PdfRectangle bound) => [];

        public List<string> GetTags(IEnumerable<TextBlock> txtBlocks, IEnumerable<TagPattern> patterns)
        {
            var tags = new List<string>();

            // instrument tags typically splitted into 2 lines
            var words = txtBlocks
                .Where(b => b.TextLines.Count == 2)
                .Select(b => string.Join("-", b.TextLines.Select(PdfTextUtility.GetLineText)))
                .ToList();

            var pats = patterns
                .Where(p => p.Name == "InstrTag")
                .SelectMany(p => p.RegExs)
                .ToList();

            var instrTags = PdfTextUtility.MatchWords(words, pats);
            tags.AddRange(instrTags);

            // all tags
            words = PdfTextUtility.GetWordText(txtBlocks);
            pats = patterns
                .SelectMany(p => p.RegExs)
                .ToList();

            var eqTags = PdfTextUtility.MatchWords(words, pats);

            tags.AddRange(eqTags);

            tags = tags
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            return tags;
        }

        public List<string> GetTitles(IEnumerable<TextBlock> txtBlocks, PdfRectangle bound) => [];
    }
}
