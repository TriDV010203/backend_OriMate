namespace OrigamiPlatform.Application.Interfaces
{
    public interface IBlockedWordRepository
    {
        // Lấy danh sách tất cả các từ cấm để check (NAC-01)
        Task<IEnumerable<string>> GetAllBlockedWordsAsync();
    }
}