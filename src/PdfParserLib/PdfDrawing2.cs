using Microsoft.Extensions.Options;
using PdfParserLib.Config;
using System.Text.RegularExpressions;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using static PdfParserLib.PdfDrawing;

namespace PdfParserLib
{
    public record class PdfDrawing2
    {
        AppConfig _appConfig;

        public PdfDrawing2(IOptions<AppConfig> appConfig)
        {
            this._appConfig = appConfig.Value;
        }

        public List<DocInfo> ExtractDataFromPdf()
        {
            List<DocInfo> docs = _appConfig.PDFFiles
                .SelectMany(f => ExtractDocInfo(f, _appConfig.TagPatterns))
                .ToList();
            return docs;
        }

        #region Static

        public static List<DocInfo> ExtractDocInfo(string fileName, IEnumerable<string> tagPatterns)
        {
            var res = new List<DocInfo>();
            List<(Page Page, IEnumerable<Word> Words)> pages = PdfTextUtility2.GetPdfWordFromFile(fileName);
            foreach (var (page, words) in pages)
            {
                var blocks = PdfTextUtility2.BuildTextBlockFromWord(words);
                var title = GetTitleBlock(blocks);
                var drawingNo = GetDrawingNo(blocks)!;
                var tags = GetTags(blocks, tagPatterns);
                var doc = new DocInfo()
                {
                    Title = title,
                    ProjectNo = drawingNo[1],
                    DrawingNo = drawingNo[2],
                    RevNo = drawingNo[3],
                    Tags = tags,
                    Revisions = GetRevHistory(blocks),
                    PageNumber = page.Number,
                    PageSize = page.Size.ToString(),
                    Height = page.Height,
                    Width = page.Width,
                };
                res.Add(doc);
            }
            return res;
        }

        public static List<string> GetTags(IEnumerable<TextBlock> txtBlocks, IEnumerable<string> patterns)
        {
            var words = PdfTextUtility2.GetWordText(txtBlocks);
            var tags = MatchWords(words, patterns);
            return tags;
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

        public static List<string> GetTitleBlock(IEnumerable<TextBlock> txtBlocks)
        {
            var block = PdfTextUtility2.SelectTextBlock(txtBlocks, 2025, 2200, 100, 200);
            return block.Select(b => b.Text).ToList();
        }

        public static List<string> GetDrawingNo(IEnumerable<TextBlock> txtBlocks)
        {
            var block = PdfTextUtility2.SelectTextBlock(txtBlocks, 2025, 2500, 40, 65);
            return block
                .OrderBy(b => b.BoundingBox.Left)
                .Select(b => b.Text)
                .ToList();
        }

        public static List<Revision> GetRevHistory(IEnumerable<TextBlock> txtBlocks)
        {
            var blocks = PdfTextUtility2.SelectTextBlock(txtBlocks, 2025, 2400, 445, 550);
            var revLines = blocks
                .GroupBy(b => (int)Math.Floor(b.BoundingBox.Bottom))
                .Select(g => g
                    .OrderBy(b => b.BoundingBox.Left)
                    .Select(b => b.Text)
                    .ToList()
                )
                .ToList();

            var revs = revLines
                .Select(rl => new Revision
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

        #endregion

    }
}
