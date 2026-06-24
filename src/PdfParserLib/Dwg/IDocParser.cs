using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;

namespace PdfParserLib.Dwg
{
    public interface IDocParser
    {
        string Name { get; set; }
        List<string> GetTags(IEnumerable<TextBlock> txtBlocks, IEnumerable<string> patterns);
        List<string> GetTitles(IEnumerable<TextBlock> txtBlocks, PdfRectangle bound);
        List<string> GetDrawingNo(IEnumerable<TextBlock> txtBlocks, PdfRectangle bound);
        List<DocRevision> GetRevHistory(IEnumerable<TextBlock> txtBlocks, PdfRectangle bound);
    }

}
