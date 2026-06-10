using OpenCvSharp;
using System.Drawing;
using Tesseract;
using T = Tesseract;

namespace PdfParserLib
{
    public static class OcrUtility
    {

        public static TesseractEngine Create(string tessDataPath, string language = "eng")
        {
            TesseractEngine engine =
                new TesseractEngine(
                    tessDataPath,
                    language,
                    EngineMode.Default);

            engine.DefaultPageSegMode = PageSegMode.Auto;

            return engine;
        }

        public static IReadOnlyList<PdfDrawing.TextElement> ExtractOcrText(
            string fileName,
            TesseractEngine engine,
            float scale = 1) => 
                ExtractOcrText(File.ReadAllBytes(fileName), engine, scale);


        public static IReadOnlyList<PdfDrawing.TextElement> ExtractOcrText(
            byte[] imageBytes,
            TesseractEngine engine,
            float scale = 1)
        {
            using Mat mat = Mat.FromImageData(imageBytes);
            using Mat gray = mat.CvtColor(ColorConversionCodes.BGR2GRAY);
            using Mat thresh = gray.AdaptiveThreshold(
                255,
                AdaptiveThresholdTypes.GaussianC,
                ThresholdTypes.Binary,
                11,
                2);

            byte[] ocrBytes = thresh.ToBytes(".png");
            using Pix pix = Pix.LoadFromMemory(ocrBytes);
            using Page page = engine.Process(pix);

            ResultIterator iterator = page.GetIterator();
            iterator.Begin();

            List<PdfDrawing.TextElement> results = new();

            do
            {
                string text = iterator.GetText(PageIteratorLevel.Word);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                iterator.TryGetBoundingBox(PageIteratorLevel.Word, out T.Rect rect);

                RectangleF bounds = RectUtility.ToRectangle(rect);

                results.Add(new(text.Trim(), bounds));
            }
            while (iterator.Next(PageIteratorLevel.Word));

            return results;
        }

    }
}
