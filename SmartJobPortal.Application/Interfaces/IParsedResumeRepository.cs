using SmartJobPortal.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartJobPortal.Application.Interfaces
{
    public interface IParsedResumeRepository
    {
        Task<long> CreateAsync(ParsedResume resume);
        Task UpdateStatusAsync(long resumeId, string status, string? errorMessage = null);
        Task UpdateAsync(ParsedResume resume);
        Task<ParsedResume?> GetByIdAsync(long resumeId);
        Task<IEnumerable<ParsedResume>> GetPendingAsync(int batchSize = 10);
        Task SaveSkillsAsync(long resumeId, IEnumerable<ResumeSkill> skills);
        Task SaveExperiencesAsync(long resumeId, IEnumerable<ResumeExperience> experiences);
        Task SaveEducationsAsync(long resumeId, IEnumerable<ResumeEducation> educations);
    }
}
