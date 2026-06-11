using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Commands.Likes;

public class ToggleLikeHandler
{
    private readonly ILikeRepository _likes;

    public ToggleLikeHandler(ILikeRepository likes)
        => _likes = likes;

    // Hàm trả về bool: true nếu kết quả cuối cùng là Đã Like, false nếu là Đã Unlike
    public async Task<bool> HandleAsync(ToggleLikeCommand cmd, CancellationToken ct = default)
    {
        // 1. Kiểm tra xem User đã like mục này chưa
        var existingLike = await _likes.GetLikeAsync(cmd.UserId, cmd.TargetId, cmd.TargetType);

        if (existingLike != null)
        {
            // 2. Nếu đã like -> Xóa record (Hành động Unlike - AC-03)
            await _likes.RemoveAsync(existingLike);

            // Trả về false báo hiệu "Chưa like"
            return false;
        }
        else
        {
            // 3. Nếu chưa like -> Tạo record mới (Hành động Like - AC-02)
            var newLike = new Like
            {
                // Bỏ thuộc tính Id ở đây
                UserId = cmd.UserId,
                TargetId = cmd.TargetId,
                TargetType = cmd.TargetType,
                CreatedAt = DateTime.UtcNow
            };

            await _likes.AddAsync(newLike);

            // Trả về true báo hiệu "Đã like"
            return true;
        }
    }
}