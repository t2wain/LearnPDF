using Microsoft.Extensions.Options;
using PdfParserLib.Config;

namespace PdfParserLib
{
    public class PdfDrawing
    {
        #region Entity

        public class DocInfo
        {
            public List<string> Title { get; set; } = [];
            public string ProjectNo { get; set; } = "";
            public string DrawingNo { get; set; } = "";
            public string RevNo { get; set; } = "";
            public List<string> Tags { get; set; } = [];
            public List<Revision> Revisions { get; set; } = [];
            public int PageNumber { get; set; }
            public string PageSize { get; set; } = "";
            public double Width { get; set; }
            public double Height { get; set; }
        }

        public class Revision
        {
            public string Issue { get; set; } = "";
            public string Rev { get; set; } = "";
            public string IssueDate { get; set; } = "";
            public string Desc { get; set; } = "";
            public string IssuedBy { get; set; } = "";
            public string CheckedBy { get; set; } = "";
            public string ApprovedBy { get; set; } = "";
        }

        #endregion

        AppConfig _appConfig;

        public PdfDrawing(IOptions<AppConfig> appConfig)
        {
            this._appConfig = appConfig.Value;
        }

        public List<DocInfo> ExtractDataFromPdf()
        {
            List<DocInfo> docs = _appConfig.PDFFiles
                .Select(f => ExtractDocInfo(f, _appConfig.TagPatterns))
                .ToList();
            return docs;
        }

        #region Static

        public static DocInfo ExtractDocInfo(string fileName, IEnumerable<string> tagPatterns)
        {
            var lines = PdfTextUtility.ExtractText(fileName);
            var title = GetTitleBlock(lines);
            var drawingNo = GetDrawingNo(lines)!;
            var tags = PdfDrawingUtility.ExtractTag(lines, tagPatterns);
            return new()
            {
                Title = title,
                ProjectNo = drawingNo[1],
                DrawingNo = drawingNo[2],
                RevNo = drawingNo[3],
                Tags = tags,
                Revisions = GetRevHistory(lines)
            };
        }

        public static List<string> GetTitleBlock(IEnumerable<PdfTextUtility.TextLine> lines)
        {
            var block = PdfTextUtility.GetTextBlock(lines, 2025, 2200, 100, 200);
            return block.Select(b => b.Text).ToList();
        }

        public static List<string> GetDrawingNo(IEnumerable<PdfTextUtility.TextLine> lines)
        {
            var block = PdfTextUtility.GetTextBlock(lines, 2025, 2200, 40, 65);
            List<string> result = [];
            if (block.FirstOrDefault() is PdfTextUtility.TextBlock b)
            {
                result = b.Text.Split(" ").ToList();
            }
            return result;
        }

        public static List<Revision> GetRevHistory(IEnumerable<PdfTextUtility.TextLine> lines)
        {
            var blocks = PdfTextUtility.GetTextBlock(lines, 2025, 2400, 445, 550);
            var revLines = blocks
                .GroupBy(b => b.LineNo)
                .Select(g => new
                {
                    LineNo = g.Key,
                    Words = g
                        .SelectMany(b => b.Words)
                        .OrderBy(w => w.X)
                        .ToList()
                })
                .ToList();

            var revs = revLines
                .Select(rl => new Revision
                {
                    Issue = rl.Words[0].Text,
                    Rev = rl.Words[1].Text,
                    IssueDate = rl.Words[2].Text,
                    Desc = string.Join(" ", rl.Words[3..^3].Select(w => w.Text)),
                    IssuedBy = rl.Words[^3].Text,
                    CheckedBy = rl.Words[^2].Text,
                    ApprovedBy = rl.Words[^1].Text
                })
                .ToList();

            return revs;
        }

        #endregion

    }
}
