using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Customers;
using Application.DTOs.Customers.Customer;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/customers")]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IUnitOfWork _unitOfWork;

        public CustomerController(ICustomerService customerService, IUnitOfWork unitOfWork)
        {
            _customerService = customerService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Perfil del cliente vinculado al usuario autenticado (prefills del chat).
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var customerId = await ResolveCustomerIdAsync();
            if (customerId is null)
                return NotFound(new { message = "No hay un cliente vinculado a tu cuenta." });

            var customer = await _customerService.GetByIdAsync(customerId.Value);
            return customer is null ? NotFound() : Ok(customer);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador,Asesor")]
        public async Task<IActionResult> GetAll()
        {
            var customers = await _customerService.GetAllAsync();
            return Ok(customers);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Administrador,Asesor")]
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await _customerService.GetByIdAsync(id);
            return customer is null ? NotFound() : Ok(customer);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador,Asesor")]
        public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
        {
            var customer = await _customerService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = customer.CustomerId }, customer);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador,Asesor")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerRequest request)
        {
            var customer = await _customerService.UpdateAsync(id, request);
            return customer is null ? NotFound() : Ok(customer);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Administrador,Asesor")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _customerService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }

        private async Task<int?> ResolveCustomerIdAsync()
        {
            var customerClaim =
                User.FindFirstValue("customerId") ??
                User.FindFirstValue("customer_id");
            if (int.TryParse(customerClaim, out var fromClaim) && fromClaim > 0)
                return fromClaim;

            var raw =
                User.FindFirstValue("nameid") ??
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub");

            if (int.TryParse(raw, out var userId) && userId > 0)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user?.CustomerId is int linked)
                    return linked;
            }

            return null;
        }
    }
}
