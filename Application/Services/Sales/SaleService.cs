using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Sales;
using Application.DTOs.Sales.Sale;
using Application.DTOs.Sales.SaleDetail;
using Domain.Entities.Customers;
using Domain.Entities.Invoices;
using Domain.Entities.Sales;
using Domain.ValueObject.Customers.Customer;
using Domain.ValueObject.Sales.Sale;
using Domain.ValueObject.Sales.SaleDetail;
using Domain.Entities.Products;

namespace Application.Services.Sales
{
    public class SaleService : ISaleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SaleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<SaleDto>> GetAllAsync()
        {
            var sales = await _unitOfWork.Sales.GetAllWithDetailsAsync();
            return sales.Select(ToDto).ToList();
        }

        public async Task<SaleDto?> GetByIdAsync(int id)
        {
            var sale = await _unitOfWork.Sales.GetByIdWithDetailsAsync(id);
            return sale is null ? null : ToDto(sale);
        }

        public async Task<IReadOnlyList<SaleDto>> GetMineAsync(int authenticatedUserId)
        {
            var customerId = await ResolveLinkedCustomerIdAsync(authenticatedUserId);
            if (customerId is null)
                return Array.Empty<SaleDto>();

            var sales = await _unitOfWork.Sales.GetByCustomerIdWithDetailsAsync(customerId.Value);
            return sales.Select(ToDto).ToList();
        }

        public async Task<SaleResultDto> CreateAsync(CreateSaleRequest request, int? authenticatedUserId = null)
        {
            if (request.Items is null || request.Items.Count == 0)
                return new SaleResultDto { Success = false, Message = "La venta debe tener al menos un producto." };

            PaymentMethod paymentMethod;
            try
            {
                paymentMethod = ResolvePaymentMethod(request);
            }
            catch (ArgumentException ex)
            {
                return new SaleResultDto { Success = false, Message = ex.Message };
            }

            var isManual = !string.Equals(request.Origin?.Trim(), "Chatbot", StringComparison.OrdinalIgnoreCase);
            if (isManual)
            {
                if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
                    return new SaleResultDto { Success = false, Message = "La dirección de entrega es obligatoria." };
                if (string.IsNullOrWhiteSpace(request.ContactPhone))
                    return new SaleResultDto { Success = false, Message = "El teléfono de contacto es obligatorio." };
                if (string.IsNullOrWhiteSpace(request.ContactDocument))
                    return new SaleResultDto { Success = false, Message = "El documento de identidad es obligatorio." };
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var customerId = await ResolveCustomerIdAsync(request.CustomerId, authenticatedUserId);
                await SyncCustomerContactAsync(customerId, request.ContactPhone, request.ContactDocument);

                var saleOriginId = await GetLookupIdByNameAsync<SaleOrigin>(request.Origin ?? "Manual");
                var saleStatusId = await GetLookupIdByNameAsync<Domain.Entities.Sales.SaleStatus>("Completada");

                foreach (var item in request.Items)
                {
                    var product = await _unitOfWork.Products.GetByIdWithInventoryAsync(item.ProductId);
                    if (product is null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return new SaleResultDto { Success = false, Message = $"El producto {item.ProductId} no existe." };
                    }

                    if (product.Inventory is null || !product.Inventory.HasEnoughStock(item.Quantity))
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return new SaleResultDto { Success = false, Message = $"Stock insuficiente para '{product.Name.Value}'." };
                    }
                }

                var sale = new Sale(
                    customerId,
                    saleOriginId,
                    saleStatusId,
                    paymentMethod,
                    deliveryAddress: request.DeliveryAddress,
                    deliveryLat: request.DeliveryLat,
                    deliveryLng: request.DeliveryLng,
                    contactPhone: request.ContactPhone,
                    contactDocument: request.ContactDocument);
                await _unitOfWork.Sales.AddAsync(sale);
                await _unitOfWork.SaveChangesAsync();

                var movementTypeId = await GetLookupIdByNameAsync<MovementType>("Salida");

                foreach (var item in request.Items)
                {
                    var product = await _unitOfWork.Products.GetByIdWithInventoryAsync(item.ProductId);
                    var detail = new SaleDetail(
                        sale.Id,
                        item.ProductId,
                        new SaleDetailQuantity(item.Quantity),
                        new Domain.ValueObject.Sales.SaleDetail.UnitPrice(product!.Price.Value)
                    );
                    await _unitOfWork.Repository<SaleDetail>().AddAsync(detail);

                    product.Inventory!.DecreaseStock(item.Quantity);
                    _unitOfWork.Inventory.Update(product.Inventory);

                    var movement = Domain.Entities.Inventories.InventoryMovement.CreateExit(
                        product.Inventory.Id, movementTypeId, item.Quantity, "Venta registrada"
                    );
                    await _unitOfWork.Repository<Domain.Entities.Inventories.InventoryMovement>().AddAsync(movement);
                }

                await _unitOfWork.SaveChangesAsync();

                var sequence = await _unitOfWork.Invoices.GetNextSequenceAsync();
                var invoice = Invoice.GenerateFor(sale, sequence);
                await _unitOfWork.Invoices.AddAsync(invoice);

                await _unitOfWork.CommitTransactionAsync();

                var fullSale = await _unitOfWork.Sales.GetByIdWithDetailsAsync(sale.Id);
                var total = fullSale?.GetTotal() ?? 0;

                return new SaleResultDto
                {
                    Success = true,
                    SaleId = sale.Id,
                    InvoiceNumber = invoice.InvoiceNumber.Value,
                    Total = total,
                    Message = "Venta registrada exitosamente.",
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                var detail = ex.InnerException?.Message ?? ex.Message;
                return new SaleResultDto { Success = false, Message = $"Error al registrar la venta: {detail}" };
            }
        }

        public async Task<SaleDto?> ChangeStatusAsync(int id, int saleStatusId)
        {
            var sale = await _unitOfWork.Sales.GetByIdAsync(id);
            if (sale is null) return null;

            sale.ChangeStatus(saleStatusId);
            _unitOfWork.Sales.Update(sale);
            await _unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        private static PaymentMethod ResolvePaymentMethod(CreateSaleRequest request)
        {
            var origin = request.Origin?.Trim() ?? "Manual";
            var isChatbot = string.Equals(origin, "Chatbot", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                if (isChatbot)
                    return PaymentMethod.Efectivo;

                throw new ArgumentException("El método de pago es obligatorio (Efectivo o Tarjeta).");
            }

            return PaymentMethod.Create(request.PaymentMethod);
        }

        private async Task<int> ResolveCustomerIdAsync(int? customerId, int? authenticatedUserId)
        {
            // Usuario autenticado: siempre vincular por cuenta (ignora customerId del cliente).
            if (authenticatedUserId.HasValue)
            {
                var linked = await ResolveLinkedCustomerIdAsync(authenticatedUserId.Value);
                if (linked.HasValue)
                    return linked.Value;

                throw new InvalidOperationException("El usuario autenticado no existe.");
            }

            if (customerId.HasValue)
                return customerId.Value;

            var anonymous = new Customer(new CustomerName("Cliente Anónimo"));
            await _unitOfWork.Customers.AddAsync(anonymous);
            await _unitOfWork.SaveChangesAsync();
            return anonymous.Id;
        }

        /// <summary>
        /// Resuelve el Customer del usuario priorizando el que ya existe por email
        /// (evita pedidos huérfanos si el User quedó ligado a otro Customer vacío).
        /// </summary>
        private async Task<int?> ResolveLinkedCustomerIdAsync(int authenticatedUserId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(authenticatedUserId);
            if (user is null)
                return null;

            var existingByEmail = await _unitOfWork.Customers.GetByEmailAsync(user.Email.Value);
            if (existingByEmail is not null)
            {
                if (user.CustomerId != existingByEmail.Id)
                {
                    user.LinkCustomer(existingByEmail.Id);
                    _unitOfWork.Users.Update(user);
                    await _unitOfWork.SaveChangesAsync();
                }

                return existingByEmail.Id;
            }

            if (user.CustomerId.HasValue)
                return user.CustomerId.Value;

            var customer = new Customer(
                new CustomerName(user.Name.Value),
                new CustomerEmail(user.Email.Value));
            await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            user.LinkCustomer(customer.Id);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return customer.Id;
        }

        private async Task SyncCustomerContactAsync(int customerId, string? phone, string? document)
        {
            if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(document))
                return;

            var customer = await _unitOfWork.Customers.GetByIdAsync(customerId);
            if (customer is null)
                return;

            var nextPhone = !string.IsNullOrWhiteSpace(phone)
                ? new Phone(phone)
                : customer.PhoneNumber;
            var nextDocument = !string.IsNullOrWhiteSpace(document)
                ? new DocumentNumber(document)
                : customer.DocumentNumber;

            customer.Update(customer.Name, customer.Email, nextPhone, nextDocument);
            _unitOfWork.Customers.Update(customer);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<int> GetLookupIdByNameAsync<T>(string name) where T : Domain.Common.BaseEntity
        {
            var items = await _unitOfWork.Repository<T>().GetAllAsync();

            var nameProperty = typeof(T).GetProperty("Name");
            var match = items.FirstOrDefault(i =>
            {
                var nameValueObject = nameProperty?.GetValue(i);
                var valueProperty = nameValueObject?.GetType().GetProperty("Value");
                var value = valueProperty?.GetValue(nameValueObject) as string;
                return string.Equals(value, name, StringComparison.OrdinalIgnoreCase);
            });

            if (match is null)
                throw new InvalidOperationException($"No se encontró '{name}' en el catálogo {typeof(T).Name}.");

            return match.Id;
        }

        private static SaleDto ToDto(Sale sale) => new()
        {
            SaleId = sale.Id,
            CustomerName = sale.Customer?.Name.Value ?? string.Empty,
            OriginName = sale.SaleOrigin?.Name.Value ?? string.Empty,
            StatusName = sale.SaleStatus?.Name.Value ?? string.Empty,
            PaymentMethod = sale.PaymentMethod?.Value ?? string.Empty,
            SaleDate = sale.SaleDate.Value,
            Total = sale.GetTotal(),
            InvoiceNumber = sale.Invoice?.InvoiceNumber.Value,
            DeliveryAddress = sale.DeliveryAddress,
            DeliveryLat = sale.DeliveryLat,
            DeliveryLng = sale.DeliveryLng,
            ContactPhone = sale.ContactPhone,
            ContactDocument = sale.ContactDocument,
            Details = sale.Details.Select(d => new SaleDetailDto
            {
                SaleDetailId = d.Id,
                ProductId = d.ProductId,
                ProductName = d.Product?.Name.Value ?? string.Empty,
                CategoryName = d.Product?.Category?.Name.Value ?? string.Empty,
                ImageUrl = d.Product?.ImageUrl,
                Quantity = d.Quantity.Value,
                UnitPrice = d.UnitPrice.Value,
                Subtotal = d.GetSubtotal(),
            }).ToList(),
        };
    }
}
