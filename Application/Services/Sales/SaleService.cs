using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Sales;
using Application.DTOs.Sales.Sale;
using Domain.Entities.Customers;
using Domain.Entities.Invoices;
using Domain.Entities.Sales;
using Domain.ValueObject.Customers.Customer;
using Domain.ValueObject.Sales.SaleDetail;
using Application.DTOs.Sales.SaleDetail;
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
            var sales = await _unitOfWork.Sales.GetAllAsync();
            var result = new List<SaleDto>();

            foreach (var sale in sales)
            {
                var full = await _unitOfWork.Sales.GetByIdWithDetailsAsync(sale.Id);
                if (full != null) result.Add(ToDto(full));
            }

            return result;
        }

        public async Task<SaleDto?> GetByIdAsync(int id)
        {
            var sale = await _unitOfWork.Sales.GetByIdWithDetailsAsync(id);
            return sale is null ? null : ToDto(sale);
        }

        public async Task<SaleResultDto> CreateAsync(CreateSaleRequest request)
        {
            if (request.Items is null || request.Items.Count == 0)
                return new SaleResultDto { Success = false, Message = "La venta debe tener al menos un producto." };

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // 1. Resolver el cliente (crea uno anónimo si el chatbot no identificó ninguno)
                var customerId = await ResolveCustomerIdAsync(request.CustomerId);

                // 2. Resolver los catálogos por nombre (Origin, Status inicial)
                var saleOriginId = await GetLookupIdByNameAsync<SaleOrigin>(request.Origin);
                var saleStatusId = await GetLookupIdByNameAsync<Domain.Entities.Sales.SaleStatus>("Completada");

                // 3. Validar stock de TODOS los items antes de tocar nada
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

                // 4. Crear la venta
                var sale = new Sale(customerId, saleOriginId, saleStatusId);
                await _unitOfWork.Sales.AddAsync(sale);
                await _unitOfWork.SaveChangesAsync(); // necesitamos sale.Id generado

                // 5. Crear cada detalle + descontar inventario + registrar movimiento
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

                // 6. Generar la factura
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
                return new SaleResultDto { Success = false, Message = $"Error al registrar la venta: {ex.Message}" };
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

        private async Task<int> ResolveCustomerIdAsync(int? customerId)
        {
            if (customerId.HasValue)
                return customerId.Value;

            // Cliente anónimo (compras vía chatbot sin identificación)
            var anonymous = new Customer(new CustomerName("Cliente Anónimo"));
            await _unitOfWork.Customers.AddAsync(anonymous);
            await _unitOfWork.SaveChangesAsync();
            return anonymous.Id;
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
            SaleDate = sale.SaleDate.Value,
            Total = sale.GetTotal(),
            Details = sale.Details.Select(d => new SaleDetailDto
            {
                SaleDetailId = d.Id,
                ProductId = d.ProductId,
                ProductName = d.Product?.Name.Value ?? string.Empty,
                Quantity = d.Quantity.Value,
                UnitPrice = d.UnitPrice.Value,
                Subtotal = d.GetSubtotal(),
            }).ToList(),
        };
    }
}