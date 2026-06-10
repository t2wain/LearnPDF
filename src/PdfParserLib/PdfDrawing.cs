using System.Drawing;
using System.Text.RegularExpressions;
using D = System.Drawing;
using P = UglyToad.PdfPig.Content;

namespace PdfParserLib
{

    public static class PdfDrawing
    {

        #region Domain Models


        /// <summary>
        /// Text element extracted from a PDF page (vector or OCR).
        /// </summary>
        public sealed record TextElement(
            string Text,
            RectangleF Bounds);

        /// <summary>
        /// Represents a detected graphical symbol on a PDF page.
        /// </summary>
        public sealed record SymbolElement(
            RectangleF Bounds);

        /// <summary>
        /// Represents a validated equipment tag associated with a symbol.
        /// </summary>
        public sealed record EquipmentTag(
            string Tag,
            RectangleF TextBounds,
            RectangleF SymbolBounds);

        #endregion

        /// <summary>
        /// Extracts graphical symbol bounding boxes from vector paths.
        /// Responsibility: symbol geometry detection only.
        /// </summary>
        public static IReadOnlyList<SymbolElement> ExtractSymbols(P.Page page) =>
            page
                .Paths
                .Where(p => p.Count >= 3)
                .Select(p => p.GetBoundingRectangle())
                .Where(r => r != null)
                .Select(r => new SymbolElement(RectUtility.ToRectangle(r!.Value, page.Height)))
                .ToList();
        
        public static IReadOnlyList<TextElement> ExtractWords(P.Page page) =>
            PdfUtility
                .ExtractAllWordsWithCoordinates(page)
                .Select(w => new TextElement(w.Text!, w.Bound!.Value))
                .ToList();


        /// <summary>
        /// Extracts text from a rendered PDF page image using OpenCV and Tesseract.
        /// Responsibility: OCR text extraction only.
        /// </summary>
        //public static IReadOnlyList<TextElement> ExtractOcrText(
        //    PdfDocument document,
        //    int pageNumber,
        //    TesseractEngine engine,
        //    float scale = 1)
        //{
        //    document.AddSkiaPageFactory();
        //    using SKBitmap bitmap = document.GetPageAsSKBitmap(pageNumber, scale);
        //    using SKImage image = SKImage.FromBitmap(bitmap);
        //    using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

        //    byte[] imageBytes = data.ToArray();
        //    using Mat mat = Mat.FromImageData(imageBytes);
        //    using Mat gray = mat.CvtColor(ColorConversionCodes.BGR2GRAY);
        //    using Mat thresh = gray.AdaptiveThreshold(
        //        255,
        //        AdaptiveThresholdTypes.GaussianC,
        //        ThresholdTypes.Binary,
        //        11,
        //        2);

        //    byte[] ocrBytes = thresh.ToBytes(".png");
        //    using Pix pix = Pix.LoadFromMemory(ocrBytes);
        //    using T.Page page = engine.Process(pix);

        //    ResultIterator iterator = page.GetIterator();
        //    iterator.Begin();

        //    List<TextElement> results = new List<TextElement>();

        //    do
        //    {
        //        string text = iterator.GetText(PageIteratorLevel.Word);
        //        if (string.IsNullOrWhiteSpace(text))
        //        {
        //            continue;
        //        }

        //        iterator.TryGetBoundingBox(PageIteratorLevel.Word, out T.Rect rect);
        //        results.Add(new(text.Trim(), RectUtility.ToRectangle(rect)));
        //    }
        //    while (iterator.Next(PageIteratorLevel.Word));

        //    return results;
        //}


        /// <summary>
        /// Extracts candidate equipment tag strings from text elements using a regular expression.
        /// Responsibility: text pattern detection only.
        /// Filters text elements whose text matches the provided equipment tag regex.
        /// </summary>
        public static IReadOnlyList<TextElement> DetectEquipmentTag(
            IEnumerable<TextElement> textElements,
            Regex tagRegex)
        {
            List<TextElement> matches = textElements
                .Where(t => !string.IsNullOrWhiteSpace(t.Text))
                .Where(t => tagRegex.IsMatch(t.Text.Trim()))
                .Select(t => new TextElement(t.Text.Trim(), t.Bounds))
                .ToList();

            return matches;
        }


        // <summary>
        /// Computes spatial relationships between text and symbols.
        /// Responsibility: geometry-based association only.
        /// Finds the nearest symbol to a given text element within a maximum distance.
        /// </summary>
        public static SymbolElement? SymbolFindNearest(
            TextElement text,
            IEnumerable<SymbolElement> symbols,
            double maxDistance)
        {
            PointF pt = RectUtility.GetCenter(text.Bounds);

            IEnumerable<(SymbolElement Symbol, double Distance)> distances =
                symbols.Select(symbol =>
                {
                    D.PointF ps = RectUtility.GetCenter(symbol.Bounds);
                    double dx = pt.X - ps.X;
                    double dy = pt.Y - ps.Y;
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    return (symbol, distance);
                });

            SymbolElement? nearest = distances
                .Where(x => x.Distance <= maxDistance)
                .OrderBy(x => x.Distance)
                .Select(x => x.Symbol)
                .FirstOrDefault();

            return nearest;
        }


        /// <summary>
        /// Extracts equipment tags from a PDF page by combining vector text,
        /// OCR fallback text, regex detection, and symbol association.
        /// Responsibility: orchestration only.
        /// </summary>
        public static IReadOnlyList<EquipmentTag> ExtractEquipmentTag(
            P.Page page,
            IEnumerable<TextElement> ocrTextElements,
            Regex equipmentTagRegex,
            double maxAssociationDistance)
            {
                var vectorText = ExtractWords(page);
                var allText = vectorText.Concat(ocrTextElements).ToList();
                var symbols = ExtractSymbols(page);

                var tagTexts = DetectEquipmentTag(allText, equipmentTagRegex);

                return tagTexts
                    .Select(t =>
                    {
                        var symbol = SymbolFindNearest(
                            t, symbols, maxAssociationDistance);

                        return symbol == null
                            ? null
                            : new EquipmentTag(
                                t.Text,
                                t.Bounds,
                                symbol.Bounds);
                    })
                    .Where(x => x != null)
                    .Cast<EquipmentTag>()
                    .ToList();
            }
        }
}
