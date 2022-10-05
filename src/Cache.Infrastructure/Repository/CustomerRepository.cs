using Cache.Core.Interfaces;
using Cache.Core.Models;
using Cache.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cache.Infrastructure.Repository
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly ApplicationDbContext _context;

        public CustomerRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _context.Customers.AsNoTracking().ToListAsync();
        }
    }
}