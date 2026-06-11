namespace OrigamiPlatform.Application.DTOs.Community;

public class CreatePostRequest
{
    public string Content { get; set; } = string.Empty;
    public Guid? LinkedTutorialId { get; set; }
    public List<string> MediaUrls { get; set; } = new();
}