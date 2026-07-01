using System.Text;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Graphics;
using SP = UglyToad.PdfPig.Core.PdfSubpath;

namespace PdfParserLib
{
    public static class PdfPathUtility
    {
        public record PathCommands(
            PdfPath Path, 
            SP SubPath, 
            IReadOnlyList<SP.IPathCommand> Commands, 
            PdfRectangle Bound,
            string ShapeType)
        {
            public (float CenterX, float CenterY, float Radius) CalcCircle()
            {
                if (ShapeType != "Circle")
                    throw new InvalidOperationException("ShapeType must be 'Circle' to calculate circle properties.");

                var b = Bound;
                float x = (float)(b.Left + b.Right) / 2;
                float y = (float)(b.Top + b.Bottom) / 2;
                float r = (float)b.Width / 2;
                return (x, y, r);
            }
        }

        public static List<PathCommands> GetCommands(IEnumerable<PdfPath> paths) =>
                paths.SelectMany(p => p.Select(sp => new PathCommands(p, sp, sp.Commands, new(), ""))).ToList();


        /// <summary>
        /// Return PdfSubPath that represents a circle or rectangle shape based on the number of IPathCommand.
        /// </summary>
        public static List<PathCommands> GetShapes(IEnumerable<PdfPath> paths)
        {
            var pc = GetCommands(paths);
            var circles = GetCircles(pc, 2, 25);
            var rectangles = GetRectangles(pc, 5, 200);
            return circles.Concat(rectangles).ToList();
        }

        /// <summary>
        /// Return PdfSubPath that represents a circle shape based on the number of IPathCommand.
        /// </summary>
        public static List<PathCommands> GetCircles(IEnumerable<PathCommands> pathCommands, double minRadius, double maxRadius) =>
            pathCommands
                .Where(i => i.SubPath.IsClosed())
                .Where(i => i.Commands.Count() == 5)
                .Where(i => i.Commands.All(cmd => cmd is SP.CubicBezierCurve || cmd is SP.Move))
                .Select(i => i with { ShapeType = "Circle", Bound = i.SubPath.GetBoundingRectangle() ?? new() })
                .Where(i =>
                {
                    var isRound = Math.Abs(i.Bound.Width - i.Bound.Height) < 1.0;
                    var radius = i.Bound.Width / 2;
                    return radius > minRadius && radius < maxRadius && isRound;
                })
                .ToList();

        /// <summary>
        /// Return PdfSubPath that represents a rectangle shape based on the number of IPathCommand.
        /// </summary>
        public static List<PathCommands> GetRectangles(IEnumerable<PathCommands> pathCommands, double minLength, double maxLength) =>
            pathCommands
                .Where(i => i.SubPath.IsClosed())
                .Where(i => i.Commands.Count() == 5)
                .Where(i => i.Commands.All(cmd => cmd is SP.Line || cmd is SP.Move))
                .Select(i => i with { ShapeType = "Rectangle", Bound = i.SubPath.GetBoundingRectangle() ?? new() })
                .Where(i => i.Bound.Width > minLength && i.Bound.Width < maxLength 
                    && i.Bound.Height > minLength && i.Bound.Height < maxLength)
                .ToList();

        public static List<string> GetSvgFromPathCommand(
            IEnumerable<SP.IPathCommand> segments, double? height = null)
        {
            StringBuilder b = new();
            var lst = new List<string>();
            foreach (SP.IPathCommand cmd in segments)
            {
                cmd.WriteSvg(b, height ?? 10);
                var svg = b.ToString();
                lst.Add(svg);
                b.Clear();
            }
            return lst;
        }
    }
}
