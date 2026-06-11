using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace PdfParserLib
{
    public class PdfDrawing
    {
        AppConfig _appConfig;

        public PdfDrawing(IOptions<AppConfig> appConfig)
        {
            this._appConfig = appConfig.Value;
        }

        #region Tags

        public List<string> DetectEquipmentTag(string fileName)
        {
            List<string> eqTags = new();
            List<string> matchWords = new();
            using PdfDocument doc = PdfUtility.GetPdfDocument(fileName);
            var words = doc
                .GetPages()
                .SelectMany(PdfTextUtility.ExtractText);

            foreach (var pat in _appConfig.TagPatterns)
            {
                Regex r = new(pat, RegexOptions.IgnoreCase);
                var matchTags = MatchEquipmentTag(r, words);
                eqTags.AddRange(matchTags.Select(i => i.Tag));
                matchWords.AddRange(matchTags.Select(i => i.Word));
                words = words.Where(w => !matchWords.Contains(w)).ToList();
            }

            eqTags = eqTags
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            return eqTags;
        }

        private IEnumerable<(string Word, string Tag)> MatchEquipmentTag(
            Regex regex, IEnumerable<string> words) =>
                words
                    .Where(w => regex.IsMatch(w))
                    .SelectMany(w => regex.Matches(w))
                    .Select(m => (m.Groups[0].Value, m.Groups[1].Value))
                    .Distinct()
                    .ToList();

        #endregion
    }
}
