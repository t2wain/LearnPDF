using PdfParserLib.Entity;
using SkiaSharp;
using Svg.Skia;
using System.Text;
using System.Xml.Linq;
using UglyToad.PdfPig.Graphics.Colors;

namespace PdfParserLib
{
    public static class ImageUtility
    {
        public static SKImage LoadSKImage(string filename)
        {
            using var bitmap = SKBitmap.Decode(filename);
            return SKImage.FromBitmap(bitmap);
        }

        public static SKImage LoadSKImage(byte[] data)
        {
            using SKBitmap bitmap = SKBitmap.Decode(data);
            return SKImage.FromBitmap(bitmap);
        }

        public static byte[] GetImageBytesAsPng(SKImage image)
        {
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        public static void SavePngImageToFile(SKImage image, string filePath)
        {
            byte[] imageBytes = GetImageBytesAsPng(image);
            File.WriteAllBytes(filePath, imageBytes);
        }

        public static void SaveSvgAsHtmlToFile(string svgContents, string filePath)
        {
            string html = $"""
                <!DOCTYPE html>
                <html lang=""en"">
                <head>
                    <meta charset=""UTF-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <title>Document Title</title>
                </head>
                <body>

                {svgContents}

                </body>
                </html>
                """;

            File.WriteAllText(filePath, html);
        }

        public static string CreateSvgElement(IEnumerable<PdfPathData> pathData, int width, int height)
        {
            StringBuilder b = new();
            Func<PdfPathData, string> f = p => $@"<path d=""{string.Join("", p.SubPaths.SelectMany(sp => sp.SVGs))}"" style=""fill:none;stroke:green;stroke-width:3"" />";
            var paths = string.Join("\n", pathData.Select(f));
            var svg = $@"<svg viewBox=""0 0 {width} {height}"" xmlns=""http://www.w3.org/2000/svg"" width=""200"" height=""200"">{paths }</svg>";
            return svg;
        }

        public static void SaveSvgAsPngToFile(string svgContent, string filePath)
        {
            // Convert string → stream
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svgContent));

            // Load SVG
            var svg = new SKSvg();
            svg.Load(stream);

            // Render to bitmap
            var picture = svg.Picture;
            var rect = picture!.CullRect;

            using var bitmap = new SKBitmap((int)rect.Width, (int)rect.Height);
            using var canvas = new SKCanvas(bitmap);

            canvas.Clear(SKColors.Transparent);
            canvas.DrawPicture(picture);
            canvas.Flush();

            // Save as PNG
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            File.WriteAllBytes(filePath, data.ToArray());
        }
    }
}
