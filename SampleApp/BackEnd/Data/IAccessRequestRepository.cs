using BackEnd.Models;

namespace BackEnd.Data
{
    public interface IAccessRequestRepository
    {
        Task<IEnumerable<AccessRequest>> GetAllAsync();
        Task<AccessRequest?> GetByIdAsync(int id);
        Task<AccessRequest> CreateAsync(AccessRequest request);
        Task UpdateAsync(AccessRequest request);
        Task DeleteAsync(int id);
        Task<IEnumerable<AccessRequest>> GetByUserIdAsync(string userId);
    }
}