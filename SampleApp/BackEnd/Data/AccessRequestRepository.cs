using Microsoft.EntityFrameworkCore;
using BackEnd.Models;

namespace BackEnd.Data
{
    public class AccessRequestRepository : IAccessRequestRepository
    {
        private readonly ApplicationDbContext _context;

        public AccessRequestRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AccessRequest>> GetAllAsync()
        {
            return await _context.AccessRequests.ToListAsync();
        }

        public async Task<AccessRequest?> GetByIdAsync(int id)
        {
            return await _context.AccessRequests.FindAsync(id);
        }

        public async Task<AccessRequest> CreateAsync(AccessRequest request)
        {
            _context.AccessRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task UpdateAsync(AccessRequest request)
        {
            _context.Entry(request).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var request = await GetByIdAsync(id);
            if (request != null)
            {
                _context.AccessRequests.Remove(request);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<AccessRequest>> GetByUserIdAsync(string userId)
        {
            return await _context.AccessRequests
                .Where(r => r.UserId == userId)
                .ToListAsync();
        }
    }
}