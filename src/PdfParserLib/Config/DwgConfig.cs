namespace PdfParserLib.Config
{
    public record DwgConfig
    {
        public string? Name { get; set; }
        public string? DwgFolder { get; set; }
        public IList<string> DwgFiles { get; set; } = [];
        public IList<DwgRegion> Regions { get; set; } = [];
        public IList<TagPattern> TagPatterns { get; set; } = [];
    }

    public record DwgRegion
    {
        public string? Name { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    public record TagPattern
    {
        public string? Name { get; set; }
        public IList<string> RegExs { get; set; } = [];
    }
}
