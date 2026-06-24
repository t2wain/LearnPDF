using PdfParserLib.Config;

namespace PdfParserLib.Dwg
{
    public interface IPdfDrawing
    {
        List<DocInfo> ParseDoc();
        List<DocInfo> ParseDoc(DwgConfig config);
        List<DocInfo> ParseDoc(DwgConfig config, IDocParser parser);
        List<DocInfo> ExtractDocInfo(string fileName, DwgConfig config, IDocParser parser);
    }
}
