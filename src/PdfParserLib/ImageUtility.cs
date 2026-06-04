using SkiaSharp;
using Svg.Skia;
using System.Text;

namespace PdfParserLib
{
    public static class ImageUtility
    {
        public static SKImage CreateSKImage(string filename)
        {
            using var bitmap = SKBitmap.Decode(filename);
            return SKImage.FromBitmap(bitmap);
        }
        public static SKImage CreateSKImage(byte[] data)
        {
            using var bitmap = SKBitmap.Decode(data);
            return SKImage.FromBitmap(bitmap);
        }

        public static void SavePngImageToFile(SKImage image, string filePath)
        {
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(filePath, data.ToArray());
        }

        public static void SavePngImageToFile(byte[] imageBytes, string filePath)
        {
            using var image = CreateSKImage(imageBytes);
            SavePngImageToFile(image, filePath);
        }

        public static void SaveSvgAsPngToFile(IEnumerable<string> svgElements, string filePath)
        {

            string svgContent = $@"
                <svg xmlns='http://www.w3.org/2000/svg' width='200' height='200'>
                    {string.Join("\n", svgElements)}
                </svg>";


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
