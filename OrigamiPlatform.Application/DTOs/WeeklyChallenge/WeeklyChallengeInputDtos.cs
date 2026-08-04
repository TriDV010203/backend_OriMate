using System.ComponentModel.DataAnnotations;

namespace OrigamiPlatform.Application.DTOs.WeeklyChallenge;

public class CreateWeeklyChallengeDto
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string? Theme { get; set; }
    
    [Required]
    public DateOnly StartDate { get; set; }
    
    [Required]
    public DateOnly EndDate { get; set; }
    
    [Required]
    public Guid TutorialId { get; set; }
}

public class UpdateWeeklyChallengeDto
{
    public string? Title { get; set; }
    public string? Theme { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public Guid? TutorialId { get; set; }
}

public class SubmitWeeklyChallengeDto
{
    [Required]
    public string PhotoUrl { get; set; } = string.Empty;
    
    public string? Note { get; set; }
}
