using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Customers.Customer;

namespace Application.Contracts.Services.Customers
{
    public interface ICustomerService
    {
        Task<IReadOnlyList<CustomerDto>> GetAllAsync();
        Task<CustomerDto?> GetByIdAsync(int id);
        Task<CustomerDto> CreateAsync(CreateCustomerRequest request);
        Task<CustomerDto?> UpdateAsync(int id, UpdateCustomerRequest request);
        Task<bool> DeleteAsync(int id);
    }
}