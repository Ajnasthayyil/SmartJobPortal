using System.Threading;
using System.Threading.Tasks;
using SmartJobPortal.Application.DTOs.Resume;

namespace SmartJobPortal.Application.Interfaces
{
    public interface IAffindaParsingService
    {
        Task<AffindaResponseDto?> ParseAsync(string filePath, CancellationToken ct = default);
    }
}
