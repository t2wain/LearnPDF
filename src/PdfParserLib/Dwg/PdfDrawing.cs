using Microsoft.Extensions.Options;
using PdfParserLib.Config;
using UglyToad.PdfPig.Content;

namespace PdfParserLib.Dwg
{
    public class PdfDrawing : IPdfDrawing
    {
        AppConfig _appConfig;
        IEnumerable<IDocParser> _parsers;

        public PdfDrawing(IOptions<AppConfig> appConfig, IEnumerable<IDocParser> parsers)
        {
            this._appConfig = appConfig.Value;
            this._parsers = parsers;
        }

        public List<DocInfo> ParseDoc() =>
            _appConfig.DwgConfigs.SelectMany(ParseDoc).ToList();

        public List<DocInfo> ParseDoc(DwgConfig config)
        {
            var docParsers = _parsers
                .Where(p => p.Name == config.ParserName)
                .ToList();

            return docParsers
                .SelectMany(p => ParseDoc(config, p))
                .ToList();
        }

        public List<DocInfo> ParseDoc(DwgConfig config, IDocParser parser)
        {
            List<DocInfo> docs = config.DwgFiles
                .SelectMany(f => ExtractDocInfo(f, config, parser))
                .ToList();
            return docs;
        }

        virtual public List<DocInfo> ExtractDocInfo(string fileName, DwgConfig config, IDocParser parser)
        {
            var tagPatterns = config.TagPatterns.SelectMany(p => p.RegExs).ToList();
            var dRegion = config.Regions.ToDictionary(r => r.Name!);

            var res = new List<DocInfo>();
            List<(Page Page, IEnumerable<Word> Words)> pages = PdfTextUtility2.GetPdfWordFromFile(fileName);
            foreach (var (page, words) in pages)
            {
                var blocks = PdfTextUtility2.BuildTextBlockFromWord(words);

                var doc = new DocInfo()
                {
                    PageNumber = page.Number,
                    PageSize = page.Size.ToString(),
                    Height = page.Height,
                    Width = page.Width,
                };

                if (config.TitleRegion is DwgRegion r1)
                {
                    var bound = RectUtility.BuildRectangle(r1);
                    doc.Title = parser.GetTitles(blocks, bound);
                }

                if (config.DwgNoRegion is DwgRegion r2)
                {
                    var bound = RectUtility.BuildRectangle(r2);
                    var drawingNo = parser.GetDrawingNo(blocks, bound)!;
                    doc.ProjectNo = drawingNo[1];
                    doc.DrawingNo = drawingNo[2];
                    doc.RevNo = drawingNo[3];

                }

                if (config.RevHistRegion is DwgRegion r3)
                {
                    var bound = RectUtility.BuildRectangle(r3);
                    doc.Revisions = parser.GetRevHistory(blocks, bound);
                }

                doc.Tags = parser.GetTags(blocks, tagPatterns);

                //var doc = new DocInfo()
                //{
                //    //Title = title,
                //    //ProjectNo = drawingNo[1],
                //    //DrawingNo = drawingNo[2],
                //    //RevNo = drawingNo[3],
                //    Tags = tags,
                //    //Revisions = revHist,
                //    PageNumber = page.Number,
                //    PageSize = page.Size.ToString(),
                //    Height = page.Height,
                //    Width = page.Width,
                //};
                res.Add(doc);
            }
            return res;
        }


    }
}
