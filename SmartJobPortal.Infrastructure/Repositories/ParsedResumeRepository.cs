using Dapper;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;
using SmartJobPortal.Infrastructure.Data;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SmartJobPortal.Infrastructure.Repositories
{
    public class ParsedResumeRepository : IParsedResumeRepository
    {
        private readonly IDbConnectionFactory _dbFactory;

        public ParsedResumeRepository(IDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        private IDbConnection CreateConnection() => _dbFactory.CreateConnection();

        public async Task<long> CreateAsync(ParsedResume resume)
        {
            const string sql = @"
                INSERT INTO ParsedResumes (CandidateId, FileName, FilePath, Status, CreatedAt, UpdatedAt)
                OUTPUT INSERTED.ParsedResumeId
                VALUES (@CandidateId, @FileName, @FilePath, @Status, @CreatedAt, @UpdatedAt)";

            using var connection = CreateConnection();
            var id = await connection.ExecuteScalarAsync<long>(sql, resume);
            resume.ParsedResumeId = id;
            return id;
        }

        public async Task UpdateStatusAsync(long resumeId, string status, string? errorMessage = null)
        {
            const string sql = @"
                UPDATE ParsedResumes 
                SET Status = @Status, 
                    ErrorMessage = @ErrorMessage,
                    UpdatedAt = GETUTCDATE()
                WHERE ParsedResumeId = @Id";
            using var connection = CreateConnection();
            await connection.ExecuteAsync(sql, new { Id = resumeId, Status = status, ErrorMessage = errorMessage });
        }

        public async Task UpdateAsync(ParsedResume resume)
        {
            const string sql = @"
                UPDATE ParsedResumes 
                SET Status = @Status, 
                    ParsedJson = @ParsedJson,
                    ErrorMessage = @ErrorMessage,
                    UpdatedAt = GETUTCDATE()
                WHERE ParsedResumeId = @ParsedResumeId";
            using var connection = CreateConnection();
            await connection.ExecuteAsync(sql, resume);
        }

        public async Task<ParsedResume?> GetByIdAsync(long resumeId)
        {
            const string sql = "SELECT * FROM ParsedResumes WHERE ParsedResumeId = @Id";
            using var connection = CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<ParsedResume>(sql, new { Id = resumeId });
        }

        public async Task<IEnumerable<ParsedResume>> GetPendingAsync(int batchSize = 10)
        {
            const string sql = "SELECT TOP(@BatchSize) * FROM ParsedResumes WHERE Status = 'Pending' ORDER BY CreatedAt ASC";
            using var connection = CreateConnection();
            return await connection.QueryAsync<ParsedResume>(sql, new { BatchSize = batchSize });
        }

        public async Task SaveSkillsAsync(long resumeId, IEnumerable<ResumeSkill> skills)
        {
            const string sql = "INSERT INTO ResumeSkills (ParsedResumeId, SkillName, ProficiencyLevel) VALUES (@ResumeId, @SkillName, @ProficiencyLevel)";
            using var connection = CreateConnection();
            var parameters = skills.Select(s => new { ResumeId = resumeId, SkillName = s.SkillName, ProficiencyLevel = s.ProficiencyLevel });
            await connection.ExecuteAsync(sql, parameters);
        }

        public async Task SaveExperiencesAsync(long resumeId, IEnumerable<ResumeExperience> experiences)
        {
            const string sql = "INSERT INTO ResumeExperiences (ParsedResumeId, Company, Role, Duration, Description) VALUES (@ResumeId, @Company, @Role, @Duration, @Description)";
            using var connection = CreateConnection();
            var parameters = experiences.Select(e => new 
            { 
                ResumeId = resumeId, 
                Company = e.Company, 
                Role = e.Role, 
                Duration = e.Duration,
                Description = e.Description
            });
            await connection.ExecuteAsync(sql, parameters);
        }

        public async Task SaveEducationsAsync(long resumeId, IEnumerable<ResumeEducation> educations)
        {
            const string sql = "INSERT INTO ResumeEducations (ParsedResumeId, Degree, Institution, GraduationYear) VALUES (@ResumeId, @Degree, @Institution, @GraduationYear)";
            using var connection = CreateConnection();
            var parameters = educations.Select(e => new 
            { 
                ResumeId = resumeId, 
                Degree = e.Degree, 
                Institution = e.Institution, 
                GraduationYear = e.GraduationYear
            });
            await connection.ExecuteAsync(sql, parameters);
        }
    }
}
