using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Customers;
using Application.DTOs.Customers.Customer;
using Domain.Entities.Customers;
using Domain.ValueObject.Customers.Customer;

namespace Application.Services.Customers
{
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CustomerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<CustomerDto>> GetAllAsync()
        {
            var customers = await _unitOfWork.Customers.GetAllAsync();
            return customers.Select(ToDto).ToList();
        }

        public async Task<CustomerDto?> GetByIdAsync(int id)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(id);
            return customer is null ? null : ToDto(customer);
        }

        public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request)
        {
            var customer = new Customer(
                name: new CustomerName(request.Name),
                email: request.Email is null ? null : new CustomerEmail(request.Email),
                phoneNumber: request.Phone is null ? null : new Phone(request.Phone),
                documentNumber: request.DocumentNumber is null ? null : new DocumentNumber(request.DocumentNumber)
            );

            await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(customer);
        }

        public async Task<CustomerDto?> UpdateAsync(int id, UpdateCustomerRequest request)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(id);
            if (customer is null) return null;

            customer.Update(
                name: new CustomerName(request.Name),
                email: request.Email is null ? null : new CustomerEmail(request.Email),
                phoneNumber: request.Phone is null ? null : new Phone(request.Phone),
                documentNumber: request.DocumentNumber is null ? null : new DocumentNumber(request.DocumentNumber)
            );

            _unitOfWork.Customers.Update(customer);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(customer);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(id);
            if (customer is null) return false;

            _unitOfWork.Customers.Remove(customer);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static CustomerDto ToDto(Customer customer) => new()
        {
            CustomerId = customer.Id,
            Name = customer.Name.Value,
            Email = customer.Email?.Value,
            Phone = customer.PhoneNumber?.Value,
            DocumentNumber = customer.DocumentNumber?.Value,
        };
    }
}
