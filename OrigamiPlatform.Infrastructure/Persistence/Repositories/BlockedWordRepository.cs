using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories
{
    public class BlockedWordRepository : IBlockedWordRepository
    {
        private readonly AppDbContext _context;

        public BlockedWordRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<string>> GetAllBlockedWordsAsync()
        {
            // Lấy danh sách cột Word trong bảng BlockedWords
            return await _context.BlockedWords
                .Select(b => b.Word)
                .ToListAsync();
        }
    }
}