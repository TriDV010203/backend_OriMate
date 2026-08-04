using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.WeeklyChallenge;
using OrigamiPlatform.Domain.Exceptions;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Infrastructure.Persistence;

namespace OrigamiPlatform.Infrastructure.Services;

public class WeeklyChallengeService : IWeeklyChallengeService
{
    private readonly AppDbContext _context;

    public WeeklyChallengeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<WeeklyChallengeDto>> GetAdminChallengesAsync(int page, int pageSize)
    {
        var query = _context.WeeklyChallenges
            .Include(c => c.Tutorial)
            .OrderByDescending(c => c.StartDate);

        var totalItems = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new WeeklyChallengeDto
            {
                Id = c.Id,
                Title = c.Title,
                Theme = c.Theme,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                TutorialId = c.TutorialId,
                CreatedByUserId = c.CreatedByUserId,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                TutorialTitle = c.Tutorial.Title,
                TutorialSlug = c.Tutorial.Slug,
                TutorialDifficulty = c.Tutorial.Difficulty.ToString(),
                TutorialAuthorName = c.Tutorial.Author != null && c.Tutorial.Author.Profile != null ? c.Tutorial.Author.Profile.DisplayName : null,
                SubmissionCount = _context.WeeklyChallengeSubmissions.Count(s => s.WeeklyChallengeId == c.Id)
            })
            .ToListAsync();

        return new PagedResult<WeeklyChallengeDto>(items, totalItems, page, pageSize, (int)Math.Ceiling(totalItems / (double)pageSize));
    }

    public async Task<WeeklyChallengeDto> CreateChallengeAsync(CreateWeeklyChallengeDto dto, Guid adminId)
    {
        var tutorial = await _context.Tutorials.FindAsync(dto.TutorialId);
        if (tutorial == null) throw new NotFoundException("Tutorial not found");

        var challenge = new WeeklyChallenge
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Theme = dto.Theme,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            TutorialId = dto.TutorialId,
            CreatedByUserId = adminId,
            Status = WeeklyChallengeStatus.Active, // Or Scheduled depending on date
            CreatedAt = DateTime.UtcNow
        };

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)); // GMT+7
        if (dto.StartDate > today)
        {
            challenge.Status = WeeklyChallengeStatus.Scheduled;
        }
        else if (dto.EndDate < today)
        {
            challenge.Status = WeeklyChallengeStatus.Closed;
        }

        _context.WeeklyChallenges.Add(challenge);
        await _context.SaveChangesAsync();

        return await GetChallengeDtoAsync(challenge.Id);
    }

    public async Task<WeeklyChallengeDto> UpdateChallengeAsync(Guid challengeId, UpdateWeeklyChallengeDto dto)
    {
        var challenge = await _context.WeeklyChallenges.FindAsync(challengeId);
        if (challenge == null) throw new NotFoundException("Weekly challenge not found");

        if (dto.Title != null) challenge.Title = dto.Title;
        if (dto.Theme != null) challenge.Theme = dto.Theme;
        if (dto.StartDate.HasValue) challenge.StartDate = dto.StartDate.Value;
        if (dto.EndDate.HasValue) challenge.EndDate = dto.EndDate.Value;
        if (dto.TutorialId.HasValue)
        {
            var tutorial = await _context.Tutorials.FindAsync(dto.TutorialId.Value);
            if (tutorial == null) throw new NotFoundException("Tutorial not found");
            challenge.TutorialId = dto.TutorialId.Value;
        }

        challenge.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetChallengeDtoAsync(challenge.Id);
    }

    public async Task DeleteChallengeAsync(Guid challengeId)
    {
        var challenge = await _context.WeeklyChallenges.FindAsync(challengeId);
        if (challenge == null) throw new NotFoundException("Weekly challenge not found");

        _context.WeeklyChallenges.Remove(challenge);
        await _context.SaveChangesAsync();
    }

    public async Task<WeeklyChallengeDto?> GetCurrentChallengeAsync(Guid? currentUserId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var challenge = await _context.WeeklyChallenges
            .Include(c => c.Tutorial)
            .ThenInclude(t => t.Author)
            .Where(c => c.StartDate <= today && c.EndDate >= today && c.Status != WeeklyChallengeStatus.Closed)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync();

        if (challenge == null) return null;

        var dto = new WeeklyChallengeDto
        {
            Id = challenge.Id,
            Title = challenge.Title,
            Theme = challenge.Theme,
            StartDate = challenge.StartDate,
            EndDate = challenge.EndDate,
            TutorialId = challenge.TutorialId,
            CreatedByUserId = challenge.CreatedByUserId,
            Status = challenge.Status,
            CreatedAt = challenge.CreatedAt,
            UpdatedAt = challenge.UpdatedAt,
            TutorialTitle = challenge.Tutorial.Title,
            TutorialSlug = challenge.Tutorial.Slug,
            TutorialDifficulty = challenge.Tutorial.Difficulty.ToString(),
            TutorialAuthorName = challenge.Tutorial.Author?.Profile?.DisplayName,
            SubmissionCount = await _context.WeeklyChallengeSubmissions.CountAsync(s => s.WeeklyChallengeId == challenge.Id)
        };

        if (currentUserId.HasValue)
        {
            dto.HasSubmittedThisWeek = await _context.WeeklyChallengeSubmissions
                .AnyAsync(s => s.WeeklyChallengeId == challenge.Id && s.UserId == currentUserId.Value);
            
            // Lấy điểm tổng kết tuần hoặc rank (nếu đã tổng kết)
            // Tạm thời mock 500 điểm nếu đã nộp
            dto.MyWeeklyPoints = dto.HasSubmittedThisWeek ? 500 : 0; 
        }

        return dto;
    }

    public async Task<PagedResult<WeeklyChallengeSubmissionDto>> GetSubmissionsAsync(Guid challengeId, int page, int pageSize, Guid? currentUserId)
    {
        var query = _context.WeeklyChallengeSubmissions
            .Include(s => s.User)
            .ThenInclude(u => u.Profile)
            .Where(s => s.WeeklyChallengeId == challengeId);

        query = query.OrderByDescending(s => s.LikeCount)
                     .ThenByDescending(s => s.CreatedAt);

        var totalItems = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new WeeklyChallengeSubmissionDto
            {
                Id = s.Id,
                WeeklyChallengeId = s.WeeklyChallengeId,
                UserId = s.UserId,
                UserDisplayName = s.User.Profile != null ? s.User.Profile.DisplayName : null,
                UserAvatarUrl = s.User.Profile != null ? s.User.Profile.AvatarUrl : null,
                PhotoUrl = s.PhotoUrl,
                Note = s.Note,
                CreatedAt = s.CreatedAt,
                FinalRank = s.FinalRank,
                LikeCount = s.LikeCount,
                IsLikedByCurrentUser = currentUserId.HasValue && _context.Likes.Any(l => l.TargetType == TargetType.WeeklyChallengeSubmission && l.TargetId == s.Id && l.UserId == currentUserId.Value)
            })
            .ToListAsync();

        return new PagedResult<WeeklyChallengeSubmissionDto>(items, totalItems, page, pageSize, (int)Math.Ceiling(totalItems / (double)pageSize));
    }

    public async Task<WeeklyChallengeSubmissionDto> SubmitAsync(Guid challengeId, SubmitWeeklyChallengeDto dto, Guid userId)
    {
        var challenge = await _context.WeeklyChallenges.FindAsync(challengeId);
        if (challenge == null) throw new NotFoundException("Weekly challenge not found");

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        if (today < challenge.StartDate || today > challenge.EndDate || challenge.Status == WeeklyChallengeStatus.Closed)
        {
            throw new BadRequestException("Thử thách tuần này không còn hoạt động.");
        }

        var existing = await _context.WeeklyChallengeSubmissions
            .FirstOrDefaultAsync(s => s.WeeklyChallengeId == challengeId && s.UserId == userId);

        if (existing != null)
        {
            throw new BadRequestException("Bạn đã nộp bài cho thử thách này rồi.");
        }

        var submission = new WeeklyChallengeSubmission
        {
            Id = Guid.NewGuid(),
            WeeklyChallengeId = challengeId,
            UserId = userId,
            PhotoUrl = dto.PhotoUrl,
            Note = dto.Note,
            CreatedAt = DateTime.UtcNow,
            LikeCount = 0
        };

        _context.WeeklyChallengeSubmissions.Add(submission);
        await _context.SaveChangesAsync();

        var user = await _context.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == userId);

        return new WeeklyChallengeSubmissionDto
        {
            Id = submission.Id,
            WeeklyChallengeId = submission.WeeklyChallengeId,
            UserId = submission.UserId,
            UserDisplayName = user?.Profile?.DisplayName,
            UserAvatarUrl = user?.Profile?.AvatarUrl,
            PhotoUrl = submission.PhotoUrl,
            Note = submission.Note,
            CreatedAt = submission.CreatedAt,
            LikeCount = 0,
            IsLikedByCurrentUser = false
        };
    }

    public async Task ToggleSubmissionLikeAsync(Guid submissionId, Guid userId)
    {
        var submission = await _context.WeeklyChallengeSubmissions.FindAsync(submissionId);
        if (submission == null) throw new NotFoundException("Submission not found");

        var existingLike = await _context.Likes.FirstOrDefaultAsync(l => 
            l.TargetType == TargetType.WeeklyChallengeSubmission && 
            l.TargetId == submissionId && 
            l.UserId == userId);

        if (existingLike != null)
        {
            _context.Likes.Remove(existingLike);
            submission.LikeCount = Math.Max(0, submission.LikeCount - 1);
        }
        else
        {
            _context.Likes.Add(new Like
            {
                TargetId = submissionId,
                TargetType = TargetType.WeeklyChallengeSubmission,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
            submission.LikeCount++;
        }

        await _context.SaveChangesAsync();
    }

    private async Task<WeeklyChallengeDto> GetChallengeDtoAsync(Guid id)
    {
        var challenge = await _context.WeeklyChallenges
            .Include(c => c.Tutorial)
            .ThenInclude(t => t.Author)
            .FirstOrDefaultAsync(c => c.Id == id);
            
        if (challenge == null) throw new NotFoundException("Weekly challenge not found");
        
        return new WeeklyChallengeDto
        {
            Id = challenge.Id,
            Title = challenge.Title,
            Theme = challenge.Theme,
            StartDate = challenge.StartDate,
            EndDate = challenge.EndDate,
            TutorialId = challenge.TutorialId,
            CreatedByUserId = challenge.CreatedByUserId,
            Status = challenge.Status,
            CreatedAt = challenge.CreatedAt,
            UpdatedAt = challenge.UpdatedAt,
            TutorialTitle = challenge.Tutorial.Title,
            TutorialSlug = challenge.Tutorial.Slug,
            TutorialDifficulty = challenge.Tutorial.Difficulty.ToString(),
            TutorialAuthorName = challenge.Tutorial.Author?.Profile?.DisplayName,
            SubmissionCount = await _context.WeeklyChallengeSubmissions.CountAsync(s => s.WeeklyChallengeId == challenge.Id)
        };
    }
}
