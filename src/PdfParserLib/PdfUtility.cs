using PdfParserLib.Entity;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Actions;
using UglyToad.PdfPig.Annotations;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Graphics;
using UglyToad.PdfPig.Graphics.Colors;
using UglyToad.PdfPig.Graphics.Core;
using UglyToad.PdfPig.Graphics.Operations;
using UglyToad.PdfPig.Rendering.Skia;
using UglyToad.PdfPig.Tokens;

namespace PdfParserLib
{
    public static class PdfUtility
    {
        public static PdfDocument GetPdfDocument(string pdfFilePath)
        {
            if (string.IsNullOrWhiteSpace(pdfFilePath))
            {
                throw new ArgumentException("PDF file path must be provided.", nameof(pdfFilePath));
            }

            if (!File.Exists(pdfFilePath))
            {
                throw new FileNotFoundException("PDF file was not found.", pdfFilePath);
            }

            return PdfDocument.Open(pdfFilePath);
        }

        #region ExtractAllWords

        public static IReadOnlyList<string> ExtractAllWords(string pdfFilePath)
        {
            using PdfDocument document = GetPdfDocument(pdfFilePath);
            return ExtractAllWords(document);
        }

        public static IReadOnlyList<string> ExtractAllWords(PdfDocument document) =>
            document
                .GetPages()
                .SelectMany(ExtractAllWords)
                .ToList();

        public static IReadOnlyList<string> ExtractAllWords(Page page) =>
            page.GetWords()
                .Select(word => word.Text)
                .ToList();

        #endregion

        #region ExtractAllWordsWithCoordinates

        public static IReadOnlyList<PdfWordData> ExtractAllWordsWithCoordinates(string pdfFilePath)
        {
            using PdfDocument document = GetPdfDocument(pdfFilePath);
            return ExtractAllWordsWithCoordinates(document);
        }

        public static IReadOnlyList<PdfWordData> ExtractAllWordsWithCoordinates(PdfDocument document) =>
            document
                .GetPages()
                .SelectMany(ExtractAllWordsWithCoordinates)
                .ToList();

        public static IReadOnlyList<PdfWordData> ExtractAllWordsWithCoordinates(Page page) =>
            ExploreWords(page.GetWords(), page.Height, new() { SaveData = true });

        #endregion

        #region ConvertPdfToPngImages

        public static void ConvertPdfToPngImages(string pdfFilePath, string destFolderPath, 
            float scale = 1.0F, int quality = 100)
        {
            if (string.IsNullOrWhiteSpace(pdfFilePath))
                throw new ArgumentException("PDF file path must be provided.", nameof(pdfFilePath));

            using PdfDocument document = GetPdfDocument(pdfFilePath);
            document.AddSkiaPageFactory();

            var fi = new FileInfo(pdfFilePath);
            string fileName = Path.Combine(destFolderPath, fi.Name.Replace(fi.Extension, ""));
            int pageCount = document.NumberOfPages;
            for (int p = 1; p <= pageCount; p++)
            {
                string imgFileName = pageCount == 1 ? $"{fileName}.png" : $"{fileName}_{p}.png";
                using (FileStream fs = new(imgFileName, FileMode.Create))
                using (MemoryStream ms = document.GetPageAsPng(p, scale, quality))
                {
                    ms.WriteTo(fs);
                }
            }
        }

        #endregion

        #region Explore

        public record PdfExtractOptions
        {
            public bool SaveData { get; set; }
            public bool SavePngImage { get; set; }
            public bool SavePdfPath { get; set; }
            public bool SaveSvgCommand { get; set; }
        }

        public static PdfDocData ExploreDocument(PdfDocument document, PdfExtractOptions? options = null)
        {
            PdfExtractOptions opts = options ?? new();
            PdfDocument d = document;

            AdvancedPdfDocumentAccess a = d.Advanced;

            DocumentInformation i = d.Information;
            DictionaryToken? dd = i.DocumentInformationDictionary;

            var lstPage = new List<PdfPageData>();
            PdfDocData o = new()
            {
                Author = i.Author,
                CreationDate = i.CreationDate,
                Creator = i.Creator,
                Keywords = i.Keywords,
                ModifiedDate = i.ModifiedDate,
                Producer = i.Producer,
                Subject = i.Subject,
                Title = i.Title,
                IsEncrypted = d.IsEncrypted,
                NumberOfPages = d.NumberOfPages,
                Version = d.Version,
                Pages = lstPage
            };

            Structure s = d.Structure;
            Catalog l = s.Catalog;
            DictionaryToken cd = l.CatalogDictionary;

            Page pg = d.GetPage(1);

            foreach (var p in d.GetPages())
            {
                PdfPageData pi = ExplorePage(p, options);
                if (opts.SaveData)
                    lstPage.Add(pi);
            }

            return o;
        }

        public static PdfPageData ExplorePage(Page page, PdfExtractOptions? options = null)
        {
            PdfExtractOptions opts = options ?? new();
            Page p = page;

            CropBox c = p.CropBox;
            PdfRectangle c1 = c.Bounds;
            ExplorePdfRectangle(c1);

            DictionaryToken d = p.Dictionary;
            IReadOnlyList<Letter> l = p.Letters;

            MediaBox b = p.MediaBox;
            PdfRectangle b1 = b.Bounds;

            MediaBox b2 = MediaBox.A0;
            b2 = MediaBox.Legal;
            b2 = MediaBox.Letter;

            PageRotationDegrees r = p.Rotation;
            double v = r.Radians;
            
            PageSize s = p.Size;
            s = PageSize.Tabloid;
            s = PageSize.Letter;
            s = PageSize.A0;

            var lstAnn = new List<PdfAnnoData>();
            var lstWord = new List<PdfWordData>();
            var lstImage = new List<PdfImageData>();
            var lstPath = new List<PdfPathData>();
            PdfPageData o = new()
            {
                PageNumber = p.Number,
                PageSize = p.Size,
                Height = p.Height,
                Width = p.Width,
                RotationDeg = p.Rotation.Value,
                IsFlip = r.SwapsAxis,
                NumberOfImages = p.NumberOfImages,
                NumberOfPaths = p.Paths.Count,
                Text = p.Text,
                BottomLeftX = b1.BottomLeft.X,
                BottomLeftY = b1.BottomLeft.Y,
                TopRightX = b1.TopRight.X,
                TopRightY = b1.TopRight.Y,

                Paths = lstPath,
                Annotations = lstAnn,
                Words = lstWord,
                Images = lstImage,
            };

            IReadOnlyList<IGraphicsStateOperation> ops = p.Operations;
            IReadOnlyList<PdfPath> paths = p.Paths;
            List<PdfPathData> lst = ExplorePdfPath(paths, page.Height, options);
            if (opts.SaveData && opts.SavePdfPath)
                lstPath.AddRange(lst);

            IEnumerable<Annotation> a = p.GetAnnotations();
            var lstAnnData = ExploreAnnotations(a, options);
            if (opts.SaveData)
                lstAnn.AddRange(lstAnnData);

            IReadOnlyList<Hyperlink> lk = p.GetHyperlinks();

            IEnumerable<IPdfImage> img = p.GetImages();
            var lstImageData = ExplorePdfImage(img, options);
            if (opts.SaveData)
                lstImage.AddRange(lstImageData);

            IReadOnlyList<MarkedContentElement> m = p.GetMarkedContents();

            IReadOnlyDictionary<string, IReadOnlyList<OptionalContentGroupElement>> op = p.GetOptionalContents();

            IEnumerable<Word> words = p.GetWords();
            List<PdfWordData> lstWordData = ExploreWords(words, p.Height, options);
            if (opts.SaveData)
                lstWord.AddRange(lstWordData);

            return o;
        }

        public static List<PdfWordData> ExploreWords(IEnumerable<Word> words, 
            double? pageHeight = null, PdfExtractOptions? options = null)
        {
            PdfExtractOptions opts = options ?? new();
            var lst = new List<PdfWordData>();
            foreach (Word w in words)
            {
                PdfRectangle b = w.BoundingBox;
                IReadOnlyList<Letter> l = w.Letters;

                PdfWordData o = new()
                {
                    Text = w.Text,
                    FontName = w.FontName,
                    BottomLeftX = b.BottomLeft.X,
                    BottomLeftY = b.BottomLeft.Y,
                    TopRightX = b.TopRight.X,
                    TopRightY = b.TopRight.Y,
                    TextOrientation = w.TextOrientation.ToString(),
                    Rotation = b.Rotation,
                };
                if (pageHeight.HasValue)
                    o.Bound = RectUtility.ToRectangle(b, pageHeight.Value);
                if (opts.SaveData)
                    lst.Add(o);
            }

            TextOrientation o2 = TextOrientation.Rotate180;
            o2 = TextOrientation.Rotate90;
            o2 = TextOrientation.Rotate270;
            o2 = TextOrientation.Horizontal;
            o2 = TextOrientation.Other;

            return lst;
        }

        public static List<PdfPathData> ExplorePdfPath(IEnumerable<PdfPath> paths, 
            double? pageHeight, PdfExtractOptions? options = null)
        {
            PdfExtractOptions opts = options ?? new();
            var lst = new List<PdfPathData>();
            foreach (PdfPath p in paths)
            {
                IColor? c = p.FillColor;
                FillingRule r = p.FillingRule;
                bool v = p.IsClipping;
                v = p.IsFilled;
                v = p.IsStroked;
                LineCapStyle s = p.LineCapStyle;
                LineDashPattern? lp = p.LineDashPattern;
                LineJoinStyle lj = p.LineJoinStyle;
                double w = p.LineWidth;
                IColor? c2 = p.StrokeColor;
                PdfRectangle? b = p.GetBoundingRectangle();

                List<PdfSubpath> subPaths = p;
                var lstSubPath = new List<PdfSubPathData>();
                PdfPathData o = new()
                {
                    BottomLeftX = b?.BottomLeft.X,
                    BottomLeftY = b?.BottomLeft.Y,
                    TopRightX = b?.TopRight.X,
                    TopRightY = b?.TopRight.Y,
                    Rotation = b?.Rotation,
                    SubPaths = lstSubPath,
                };
                if (pageHeight.HasValue && b.HasValue)
                    o.Bound = RectUtility.ToRectangle(b.Value, pageHeight.Value);

                List<PdfSubPathData> d = ExplorePdfSubPath(subPaths, options);

                if (opts.SaveData && opts.SavePdfPath)
                {
                    lstSubPath.AddRange(d);
                    lst.Add(o);
                }
            }
            return lst;
        }

        public static List<PdfSubPathData> ExplorePdfSubPath(
            IReadOnlyList<PdfSubpath> subPaths, PdfExtractOptions? options = null)
        {
            PdfExtractOptions opts = options ?? new();
            int cnt = subPaths.Count;
            PdfRectangle? r2 = PdfSubpath.GetBoundingRectangle(subPaths);

            var lst = new List<PdfSubPathData>();
            foreach (PdfSubpath sp in subPaths)
            {
                r2 = sp.GetBoundingRectangle();
                PdfPoint pt = sp.GetCentroid();
                //r2 = sp.GetDrawnRectangle();

                IReadOnlyList<PdfSubpath.IPathCommand> cmds = sp.Commands;
                List<string> lc = ExplorePdfSubPathCommand(cmds, options);

                var lstSvg = new List<string>();
                PdfSubPathData o = new()
                {
                    IsClosed = sp.IsClosed(),
                    IsClockwise = sp.IsClockwise,
                    IsCounterClockwise = sp.IsCounterClockwise,
                    IsDrawnAsRectangle = sp.IsDrawnAsRectangle,
                    BottomLeftX = r2?.BottomLeft.X,
                    BottomLeftY = r2?.BottomLeft.Y,
                    TopRightX = r2?.TopRight.X,
                    TopRightY = r2?.TopRight.Y,
                    SVGs = lstSvg,
                };

                if (opts.SaveData && opts.SavePdfPath)
                {
                    if (opts.SaveSvgCommand)
                        lstSvg.AddRange(lc);
                    lst.Add(o);
                }


            }
            return lst;
        }

        public static List<string> ExplorePdfSubPathCommand(
            IReadOnlyList<PdfSubpath.IPathCommand> commands, PdfExtractOptions? options = null)
        {
            PdfExtractOptions opts = options ?? new();
            StringBuilder b = new();
            var lst = new List<string>();
            foreach (PdfSubpath.IPathCommand cmd in commands)
            {
                PdfRectangle? r2 = cmd.GetBoundingRectangle();
                cmd.WriteSvg(b, 10);
                if (opts.SaveData && opts.SavePdfPath && opts.SaveSvgCommand)
                    lst.Add(b.ToString());
                b.Clear();
            }
            return lst;
        }

        public static List<PdfImageData> ExplorePdfImage(IEnumerable<IPdfImage> images, PdfExtractOptions? options = null)
        {
            PdfExtractOptions opts = options ?? new();
            var lst = new List<PdfImageData>();
            int cnt = images.Count();
            foreach (IPdfImage img in images)
            {
                PdfRectangle b = img.BoundingBox;
                ColorSpaceDetails? cd = img.ColorSpaceDetails;
                DictionaryToken d = img.ImageDictionary;
                bool s = img.TryGetPng(out var png);

                PdfImageData o = new()
                {
                    HeightInSamples = img.HeightInSamples,
                    WidthInSamples = img.WidthInSamples,
                    IsInlineImage = img.IsInlineImage,
                    BitsPerComponent = img.BitsPerComponent,
                    BottomLeftX = b.BottomLeft.X,
                    BottomLeftY = b.BottomLeft.Y,
                    TopRightX = b.TopRight.X,
                    TopRightY = b.TopRight.Y,
                    PNG = opts.SaveData && opts.SavePngImage ? png : null,
                };
                if (opts.SaveData)
                    lst.Add(o);
            }
            return lst;
        }

        public static List<PdfAnnoData> ExploreAnnotations(
            IEnumerable<Annotation> annotations, PdfExtractOptions? options = null)
        {
            PdfExtractOptions opts = options ?? new();
            var lst = new List<PdfAnnoData>();
            int cnt = annotations.Count();
            foreach (Annotation a in annotations)
            {
                PdfAction? ac = a.Action;
                DictionaryToken d = a.AnnotationDictionary;
                AnnotationBorder b = a.Border;
                AnnotationFlags f = a.Flags;
                Annotation? r = a.InReplyTo;
                PdfRectangle rc = a.Rectangle;

                PdfAnnoData o = new()
                {
                    Content = a.Content,
                    ModifiedDate = a.ModifiedDate,
                    Name = a.Name,
                    Type = a.Type,
                    InReplayTo = a.InReplyTo?.Name
                };

                if (opts.SaveData)
                    lst.Add(o);
            }
            return lst;
        }

        public static void ExplorePdfRectangle(PdfRectangle rectangle)
        {
            PdfRectangle r = rectangle;
            double d = r.Area;
            d = r.Bottom;
            PdfPoint p = r.BottomLeft;
            p = r.BottomRight;
            p = r.Centroid;
            d = r.Height;
            d = r.Left;
            d = r.Right;
            d = r.Rotation;
            d = r.Top;
            p = r.TopLeft;
            p = r.TopRight;
            d = r.Width;

            d = p.X;
            d = p.Y;

            p = PdfPoint.Origin;
        }

        #endregion
    }
}
