using PdfParserLib.Config;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;

namespace PdfParserLib.Dwg
{
    public class DocParser : IDocParser
    {
        public string Name { get; set; } = null!;

        virtual public List<string> GetTags(IEnumerable<TextBlock> txtBlocks, IEnumerable<TagPattern> patterns)
        {
            var words = PdfTextUtility.GetWordText(txtBlocks);
            var tags = PdfTextUtility.MatchWords(words, patterns.SelectMany(p => p.RegExs));
            return tags;
        }
        virtual public List<string> GetTitles(IEnumerable<TextBlock> txtBlocks, PdfRectangle bound)
        {
            var block = PdfTextUtility.SelectTextBlock(txtBlocks, bound);
            return block.Select(b => b.Text).ToList();
        }

        virtual public DocInfo GetDrawingNo(IEnumerable<TextBlock> txtBlocks, PdfRectangle bound)
        {
            var block = PdfTextUtility.SelectTextBlock(txtBlocks, bound);
            var drawingNo = block
                .OrderBy(b => b.BoundingBox.Left)
                .Select(b => b.Text)
                .ToList();

            var doc = new DocInfo();
            if (drawingNo.Count > 0)
            {
                doc.ProjectNo = drawingNo[1];
                doc.DrawingNo = drawingNo[2];
                doc.RevNo = drawingNo[3];
            }
            return doc;
        }

        virtual public List<DocRevision> GetRevHistory(IEnumerable<TextBlock> txtBlocks, PdfRectangle bound)
        {
            var blocks = PdfTextUtility.SelectTextBlock(txtBlocks, bound);
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
    }

}
