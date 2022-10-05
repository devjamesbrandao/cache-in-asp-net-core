
using Cache.Core.Models;

namespace Cache.Core.Interfaces
{
    public interface ICustomerRepository
    {
        Task<List<Customer>> GetAllCustomersAsync();
    }
}