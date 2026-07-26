using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.Subscriptions;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Subscriptions;

public class GetAllTransactionsHandler
{
    private readonly ITransactionRepository _transactions;

    public GetAllTransactionsHandler(ITransactionRepository transactions)
        => _transactions = transactions;

    public async Task<PagedResult<AdminTransactionDto>> HandleAsync(
        GetAllTransactionsQuery query,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, totalCount) = await _transactions.GetPagedAsync(query.Status, page, pageSize, ct);

        var dtos = items.Select(t => t.ToAdminDto()).ToList();

        return new PagedResult<AdminTransactionDto>(
            dtos,
            totalCount,
            page,
            pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }
}
