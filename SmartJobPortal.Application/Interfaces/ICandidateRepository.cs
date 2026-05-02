using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Interfaces;

public interface ICandidateRepository
{
    Task<Candidate?> GetByUserIdAsync(int userId);
    Task<int> UpsertAsync(Candidate candidate);
    Task<List<CandidateSkill>> GetSkillsAsync(int candidateId);
    Task ReplaceSkillsAsync(int candidateId, List<CandidateSkill> skills);
    Task<int?> GetSkillIdByNameAsync(string skillName);
    Task<int> CreateSkillAsync(string skillName, string category = "General");
    
    // NEW Structured Data Methods
    Task AddEducationAsync(List<CandidateEducation> education);
    Task AddExperienceAsync(List<CandidateExperience> experience);
    Task ClearEducationAndExperienceAsync(int candidateId);
    Task<List<CandidateEducation>> GetEducationAsync(int candidateId);
    Task<List<CandidateExperience>> GetExperienceAsync(int candidateId);
    Task<(List<CandidateSkill> skills, List<CandidateEducation> education, List<CandidateExperience> experience)> GetFullProfileDataAsync(int candidateId);
}