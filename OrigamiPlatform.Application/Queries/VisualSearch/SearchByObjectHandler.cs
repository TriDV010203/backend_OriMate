using OrigamiPlatform.Application.DTOs.VisualSearch;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Constants; // Gọi thư viện chứa file AiDictionary
using OrigamiPlatform.Domain.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrigamiPlatform.Application.Queries.VisualSearch;

public class SearchByObjectHandler(
    IImageLabelingService imageLabelingService,
    ITutorialRepository tutorialRepository)
{
    public async Task<ObjectSearchResultDto> HandleAsync(SearchByObjectQuery request, CancellationToken cancellationToken = default)
    {
        // 1. Gửi ảnh cho AI xử lý và lấy ra nhãn
        var rawLabels = await imageLabelingService.DetectLabelsAsync(request.ImageStream);

        if (rawLabels == null || !rawLabels.Any())
            return new ObjectSearchResultDto(Message: "Không nhận diện được vật thể trong ảnh.");

        var searchKeywords = new HashSet<string>();
        foreach (var raw in rawLabels)
        {
            var cleanRaw = raw.Trim().ToLower();
            searchKeywords.Add(cleanRaw);

            // 2. Mở rộng từ khóa (ví dụ có 'dog' thì thêm 'chó'...) dựa vào Dictionary chung
            foreach (var map in AiDictionary.KeywordMapping)
            {
                if (cleanRaw.Contains(map.Key))
                {
                    searchKeywords.Add(map.Value);
                }
            }
        }

        // 3. Lấy tất cả bài hướng dẫn đã xuất bản
        var publishedTutorials = await tutorialRepository.GetAllPublishedWithCategoryAsync(cancellationToken);

        // 4. Tìm kiếm từ khóa xuất hiện ở Tiêu đề hoặc Danh mục (tránh lỗi Null)
        var results = publishedTutorials
            .Where(t => t != null && searchKeywords.Any(keyword =>
                (!string.IsNullOrEmpty(t.Title) && t.Title.ToLower().Contains(keyword)) ||
                (t.Category != null && !string.IsNullOrEmpty(t.Category.Name) && t.Category.Name.ToLower().Contains(keyword))))
            .Select(t => new OrigamiSearchResultItemDto(
                TargetId: t.Id,
                TargetType: TargetType.Tutorial,
                Title: t.Title,
                ImageUrl: t.CoverImageUrl,
                Difficulty: t.Difficulty
            ))
            .ToList();

        // 5. Trả về kết quả phân loại theo độ khó
        return new ObjectSearchResultDto(
            Message: results.Any() ? "Tìm thấy bài hướng dẫn phù hợp." : "Không tìm thấy bài hướng dẫn nào trong hệ thống.",
            DetectedObject: rawLabels.First(),
            BeginnerOptions: results.Where(x => x.Difficulty == TutorialDifficulty.Beginner).Take(3).ToList(),
            IntermediateOptions: results.Where(x => x.Difficulty == TutorialDifficulty.Intermediate).Take(3).ToList(),
            AdvancedOptions: results.Where(x => x.Difficulty == TutorialDifficulty.Advanced).Take(3).ToList()
        );
    }
}