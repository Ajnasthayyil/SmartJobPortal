using System.IO;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace SmartJobPortal.Infrastructure.Services
{
    public interface INativeTextExtractor
    {
        Task<string> ExtractTextAsync(string filePath);
    }

    public class NativeTextExtractor : INativeTextExtractor
    {
        public Task<string> ExtractTextAsync(string filePath)
        {
            if (!File.Exists(filePath))
                return Task.FromResult(string.Empty);

            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".pdf")
            {
                using var stream = File.OpenRead(filePath);
                using var doc = PdfDocument.Open(stream);
                var sb = new StringBuilder();
                foreach (var page in doc.GetPages()) sb.AppendLine(page.Text);
                return Task.FromResult(sb.ToString());
            }
            else if (ext == ".docx")
            {
                using var stream = File.OpenRead(filePath);
                using var wordDoc = WordprocessingDocument.Open(stream, false);
                return Task.FromResult(wordDoc.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty);
            }

            return Task.FromResult(string.Empty);
        }
    }
}
