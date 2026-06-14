using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Interfaces;

public interface ICommentRepository
{
    Task AddAsync(Comment comment);
    Task<Comment?> GetByIdAsync(Guid id);
    Task RemoveAsync(Comment comment);
    Task<PagedResult<Comment>> GetCommentsByTargetAsync(Guid targetId, TargetType targetType, int page, int pageSize);
}