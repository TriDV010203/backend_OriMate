using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.WeeklyChallenge;

namespace OrigamiPlatform.Application.Interfaces;

public interface IWeeklyChallengeService
{
    // Admin
    Task<PagedResult<WeeklyChallengeDto>> GetAdminChallengesAsync(int page, int pageSize);
    Task<WeeklyChallengeDto> CreateChallengeAsync(CreateWeeklyChallengeDto dto, Guid adminId);
    Task<WeeklyChallengeDto> UpdateChallengeAsync(Guid challengeId, UpdateWeeklyChallengeDto dto);
    Task DeleteChallengeAsync(Guid challengeId);

    // User
    Task<WeeklyChallengeDto?> GetCurrentChallengeAsync(Guid? currentUserId);
    Task<PagedResult<WeeklyChallengeSubmissionDto>> GetSubmissionsAsync(Guid challengeId, int page, int pageSize, Guid? currentUserId);
    
    // Actions
    Task<WeeklyChallengeSubmissionDto> SubmitAsync(Guid challengeId, SubmitWeeklyChallengeDto dto, Guid userId);
    Task ToggleSubmissionLikeAsync(Guid submissionId, Guid userId);
}
