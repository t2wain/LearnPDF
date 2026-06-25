using Microsoft.Extensions.Options;
using PdfParserLib.Config;
using UglyToad.PdfPig.Content;

namespace PdfParserLib.Dwg
{
    /// <summary>
    /// This class contains implementation for a specific
    /// drawing title blocks. Override this class to provide
    /// a different implementation for a different title block.
    /// </summary>
    public class PdfDrawing : IPdfDrawing
    {
        AppConfig _appConfig;
        IEnumerable<IDocParser> _parsers;

        public PdfDrawing(IOptions<AppConfig> appConfig, IEnumerable<IDocParser> parsers)
        {
            this._appConfig = appConfig.Value;
            this._parsers = parsers;
        }

        /// <summary>
        /// Parse all PDF documents specified in all of DwgConfigs
        /// </summary>
        public List<DocInfo> ParseDoc() =>
            _appConfig.DwgConfigs.SelectMany(ParseDoc).ToList();

        /// <summary>
        /// Parse all PDF documents specified in a DwgConfig
        /// </summary>
        public List<DocInfo> ParseDoc(DwgConfig config)
        {
            // search for the specify parser
            var docParsers = _parsers
                .Where(p => p.Name == config.ParserName)
                .ToList();

            return docParsers
                .SelectMany(p => ParseDoc(config, p))
                .ToList();
        }

        /// <summary>
        /// Parse all PDF documents specified in a DwgConfig
        /// using a specific document parser
        /// </summary>
        public List<DocInfo> ParseDoc(DwgConfig config, IDocParser parser)
        {
            List<DocInfo> docs = config.DwgFiles
                .SelectMany(f => ExtractDocInfo(f, config, parser))
                .ToList();
            return docs;
        }

        /// <summary>
        /// Each project may implement a different drawing title blocks
        /// and equipment tag pattern. Override this method to provide
        /// implementation specific to the project.
        /// </summary>
        virtual public List<DocInfo> ExtractDocInfo(string fileName, DwgConfig config, IDocParser parser)
        {
            var tagPatterns = config.TagPatterns.SelectMany(p => p.RegExs).ToList();

            var res = new List<DocInfo>();
            List<(Page Page, IEnumerable<Word> Words)> pages = PdfTextUtility.GetPdfWordFromFile(fileName);
            foreach (var (page, words) in pages)
            {
                var blocks = PdfTextUtility.BuildTextBlockFromWord(words);

                var doc = new DocInfo()
                {
                    FileName = fileName,
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

                res.Add(doc);
            }
            return res;
        }


    }
}
