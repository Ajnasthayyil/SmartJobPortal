namespace SmartJobPortal.Application.Common.Utilities;

public interface ISemanticMatcher
{
    bool IsMatch(string candidateSkill, string jobSkill, out string reason);
}
