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
    /// <summary>
    /// Servicio de ventas que contiene la lógica de negocio para la gestión de pedidos.
    /// Maneja la creación de ventas con validación de stock, generación de facturas
    /// y gestión del ciclo de vida de las ventas.
    /// </summary>
    public class SaleService : ISaleService
    {
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Constructor que inyecta las dependencias necesarias mediante inyección de dependencias.
        /// </summary>
        /// <param name="unitOfWork">Unit of Work para acceso a datos y transacciones.</param>
        public SaleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Obtiene todas las ventas del sistema con sus detalles (cliente, productos, factura).
        /// </summary>
        /// <returns>Lista de DTOs con la información de todas las ventas.</returns>
        public async Task<IReadOnlyList<SaleDto>> GetAllAsync()
        {
            // Obtiene todas las ventas incluyendo sus relaciones (cliente, origen, estado, detalles, factura)
            var sales = await _unitOfWork.Sales.GetAllWithDetailsAsync();
            // Convierte cada entidad Sale a SaleDto usando el método ToDto
            return sales.Select(ToDto).ToList();
        }

        /// <summary>
        /// Obtiene una venta específica por su ID incluyendo sus detalles.
        /// </summary>
        /// <param name="id">ID de la venta a buscar.</param>
        /// <returns>DTO con los detalles de la venta si existe, null si no existe.</returns>
        public async Task<SaleDto?> GetByIdAsync(int id)
        {
            // Obtiene la venta por ID incluyendo sus relaciones
            var sale = await _unitOfWork.Sales.GetByIdWithDetailsAsync(id);
            return sale is null ? null : ToDto(sale);
        }

        /// <summary>
        /// Obtiene las ventas del usuario autenticado filtradas por el Customer vinculado.
        /// </summary>
        /// <param name="authenticatedUserId">ID del usuario autenticado.</param>
        /// <returns>Lista de DTOs con las ventas del usuario, o lista vacía si no tiene Customer vinculado.</returns>
        public async Task<IReadOnlyList<SaleDto>> GetMineAsync(int authenticatedUserId)
        {
            // Resuelve el Customer vinculado al usuario autenticado
            var customerId = await ResolveLinkedCustomerIdAsync(authenticatedUserId);
            if (customerId is null)
                return Array.Empty<SaleDto>(); // Si no tiene Customer vinculado, retorna lista vacía

            // Obtiene las ventas filtradas por el Customer ID
            var sales = await _unitOfWork.Sales.GetByCustomerIdWithDetailsAsync(customerId.Value);
            return sales.Select(ToDto).ToList();
        }

        /// <summary>
        /// Crea una nueva venta con validación de stock y generación de factura.
        /// Usa transacción para asegurar atomicidad de todas las operaciones.
        /// </summary>
        /// <param name="request">DTO con los datos de la venta (productos, cantidades, customer, etc.).</param>
        /// <param name="authenticatedUserId">ID del usuario autenticado (opcional, para ventas con login).</param>
        /// <returns>DTO con el resultado de la operación (éxito/fracaso, ID de venta, número de factura).</returns>
        /// <remarks>
        /// Este método realiza las siguientes operaciones en transacción:
        /// 1. Valida que haya al menos un producto en la venta
        /// 2. Resuelve el método de pago (Efectivo por defecto para chatbot)
        /// 3. Valida campos obligatorios para ventas manuales (dirección, teléfono, documento)
        /// 4. Resuelve el Customer (vinculado al usuario autenticado o anónimo)
        /// 5. Sincroniza datos de contacto del Customer (teléfono, documento)
        /// 6. Valida stock disponible de cada producto
        /// 7. Crea la venta con estado "Completada"
        /// 8. Crea los detalles de venta (líneas de pedido)
        /// 9. Disminuye el stock de cada producto
        /// 10. Registra movimientos de salida en inventario
        /// 11. Genera la factura con número secuencial
        /// Si falla cualquier paso, hace rollback de toda la transacción.
        /// </remarks>
        public async Task<SaleResultDto> CreateAsync(CreateSaleRequest request, int? authenticatedUserId = null)
        {
            // ==============================================================================
            // VALIDACIÓN INICIAL
            // ==============================================================================
            // Valida que la venta tenga al menos un producto
            if (request.Items is null || request.Items.Count == 0)
                return new SaleResultDto { Success = false, Message = "La venta debe tener al menos un producto." };

            // ==============================================================================
            // RESOLUCIÓN DEL MÉTODO DE PAGO
            // ==============================================================================
            PaymentMethod paymentMethod;
            try
            {
                paymentMethod = ResolvePaymentMethod(request);
            }
            catch (ArgumentException ex)
            {
                return new SaleResultDto { Success = false, Message = ex.Message };
            }

            // ==============================================================================
            // VALIDACIÓN DE CAMPOS OBLIGATORIOS PARA VENTAS MANUALES
            // ==============================================================================
            // Determina si la venta es manual (no chatbot)
            var isManual = !string.Equals(request.Origin?.Trim(), "Chatbot", StringComparison.OrdinalIgnoreCase);
            if (isManual)
            {
                // Para ventas manuales, se requieren datos de entrega y contacto
                if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
                    return new SaleResultDto { Success = false, Message = "La dirección de entrega es obligatoria." };
                if (string.IsNullOrWhiteSpace(request.ContactPhone))
                    return new SaleResultDto { Success = false, Message = "El teléfono de contacto es obligatorio." };
                if (string.IsNullOrWhiteSpace(request.ContactDocument))
                    return new SaleResultDto { Success = false, Message = "El documento de identidad es obligatorio." };
            }

            // ==============================================================================
            // INICIO DE TRANSACCIÓN
            // ==============================================================================
            // Inicia una transacción de base de datos para asegurar atomicidad
            // Si falla cualquier operación, se hace rollback de todo
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // ==============================================================================
                // RESOLUCIÓN DEL CUSTOMER
                // ==============================================================================
                // Resuelve el Customer:
                // - Si hay usuario autenticado, usa el Customer vinculado
                // - Si no hay usuario autenticado, usa el CustomerId del request
                // - Si no hay CustomerId, crea un Customer anónimo
                var customerId = await ResolveCustomerIdAsync(request.CustomerId, authenticatedUserId);
                // Sincroniza los datos de contacto (teléfono, documento) del Customer
                await SyncCustomerContactAsync(customerId, request.ContactPhone, request.ContactDocument);

                // ==============================================================================
                // RESOLUCIÓN DE CATÁLOGOS (ORIGEN Y ESTADO)
                // ==============================================================================
                // Resuelve el ID del origen de venta (Manual o Chatbot)
                var saleOriginId = await GetLookupIdByNameAsync<SaleOrigin>(request.Origin ?? "Manual");
                // Resuelve el ID del estado de venta (Completada por defecto)
                var saleStatusId = await GetLookupIdByNameAsync<Domain.Entities.Sales.SaleStatus>("Completada");

                // ==============================================================================
                // VALIDACIÓN DE STOCK DE PRODUCTOS
                // ==============================================================================
                // Valida que todos los productos existan y tengan suficiente stock
                foreach (var item in request.Items)
                {
                    var product = await _unitOfWork.Products.GetByIdWithInventoryAsync(item.ProductId);
                    if (product is null)
                    {
                        // Producto no existe: rollback y retorna error
                        await _unitOfWork.RollbackTransactionAsync();
                        return new SaleResultDto { Success = false, Message = $"El producto {item.ProductId} no existe." };
                    }

                    // Valida que el producto tenga suficiente stock
                    if (product.Inventory is null || !product.Inventory.HasEnoughStock(item.Quantity))
                    {
                        // Stock insuficiente: rollback y retorna error
                        await _unitOfWork.RollbackTransactionAsync();
                        return new SaleResultDto { Success = false, Message = $"Stock insuficiente para '{product.Name.Value}'." };
                    }
                }

                // ==============================================================================
                // CREACIÓN DE LA VENTA
                // ==============================================================================
                // Crea la entidad Sale con todos los datos
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
                await _unitOfWork.SaveChangesAsync(); // Guarda para obtener el ID de la venta

                // ==============================================================================
                // RESOLUCIÓN DEL TIPO DE MOVIMIENTO DE INVENTARIO
                // ==============================================================================
                var movementTypeId = await GetLookupIdByNameAsync<MovementType>("Salida");

                // ==============================================================================
                // CREACIÓN DE DETALLES DE VENTA Y ACTUALIZACIÓN DE INVENTARIO
                // ==============================================================================
                foreach (var item in request.Items)
                {
                    // Obtiene el producto con su inventario
                    var product = await _unitOfWork.Products.GetByIdWithInventoryAsync(item.ProductId);
                    // Crea el detalle de venta (línea de pedido)
                    var detail = new SaleDetail(
                        sale.Id,
                        item.ProductId,
                        new SaleDetailQuantity(item.Quantity),
                        new Domain.ValueObject.Sales.SaleDetail.UnitPrice(product!.Price.Value)
                    );
                    await _unitOfWork.Repository<SaleDetail>().AddAsync(detail);

                    // Disminuye el stock del producto
                    product.Inventory!.DecreaseStock(item.Quantity);
                    _unitOfWork.Inventory.Update(product.Inventory);

                    // Registra el movimiento de salida en inventario
                    var movement = Domain.Entities.Inventories.InventoryMovement.CreateExit(
                        product.Inventory.Id, movementTypeId, item.Quantity, "Venta registrada"
                    );
                    await _unitOfWork.Repository<Domain.Entities.Inventories.InventoryMovement>().AddAsync(movement);
                }

                await _unitOfWork.SaveChangesAsync(); // Guarda detalles y movimientos de inventario

                // ==============================================================================
                // GENERACIÓN DE FACTURA
                // ==============================================================================
                // Obtiene el siguiente número de factura secuencial
                var sequence = await _unitOfWork.Invoices.GetNextSequenceAsync();
                // Genera la factura asociada a la venta
                var invoice = Invoice.GenerateFor(sale, sequence);
                await _unitOfWork.Invoices.AddAsync(invoice);

                // ==============================================================================
                // COMMIT DE TRANSACCIÓN
                // ==============================================================================
                // Confirma todas las operaciones en la base de datos
                await _unitOfWork.CommitTransactionAsync();

                // ==============================================================================
                // CÁLCULO DEL TOTAL Y RETORNO DE RESULTADO
                // ==============================================================================
                // Recarga la venta completa para calcular el total
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
                // ==============================================================================
                // ROLLBACK EN CASO DE ERROR
                // ==============================================================================
                // Si ocurre cualquier error, hace rollback de toda la transacción
                await _unitOfWork.RollbackTransactionAsync();
                var detail = ex.InnerException?.Message ?? ex.Message;
                return new SaleResultDto { Success = false, Message = $"Error al registrar la venta: {detail}" };
            }
        }

        /// <summary>
        /// Cambia el estado de una venta existente.
        /// </summary>
        /// <param name="id">ID de la venta a actualizar.</param>
        /// <param name="saleStatusId">ID del nuevo estado de venta.</param>
        /// <returns>DTO con la venta actualizada si existe, null si no existe.</returns>
        public async Task<SaleDto?> ChangeStatusAsync(int id, int saleStatusId)
        {
            // Busca la venta por ID
            var sale = await _unitOfWork.Sales.GetByIdAsync(id);
            if (sale is null) return null;

            // Cambia el estado usando el método de dominio ChangeStatus
            sale.ChangeStatus(saleStatusId);
            _unitOfWork.Sales.Update(sale);
            await _unitOfWork.SaveChangesAsync();

            // Retorna la venta actualizada con sus detalles
            return await GetByIdAsync(id);
        }

        /// <summary>
        /// Método privado que resuelve el método de pago para una venta.
        /// Para ventas de chatbot, usa Efectivo por defecto.
        /// Para ventas manuales, requiere que se especifique el método.
        /// </summary>
        /// <param name="request">DTO con los datos de la venta.</param>
        /// <returns>ValueObject PaymentMethod con el método de pago resuelto.</returns>
        /// <exception cref="ArgumentException">
        /// Se lanza si el método de pago es inválido o no se especificó para venta manual.
        /// </exception>
        private static PaymentMethod ResolvePaymentMethod(CreateSaleRequest request)
        {
            var origin = request.Origin?.Trim() ?? "Manual";
            var isChatbot = string.Equals(origin, "Chatbot", StringComparison.OrdinalIgnoreCase);

            // Si no se especificó método de pago
            if (string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                // Para chatbot, usa Efectivo por defecto
                if (isChatbot)
                    return PaymentMethod.Efectivo;

                // Para ventas manuales, es obligatorio especificar el método
                throw new ArgumentException("El método de pago es obligatorio (Efectivo o Tarjeta).");
            }

            // Crea el ValueObject de método de pago con el valor especificado
            return PaymentMethod.Create(request.PaymentMethod);
        }

        /// <summary>
        /// Método privado que resuelve el Customer para una venta.
        /// Prioriza el Customer vinculado al usuario autenticado si existe.
        /// </summary>
        /// <param name="customerId">ID del Customer del request (opcional).</param>
        /// <param name="authenticatedUserId">ID del usuario autenticado (opcional).</param>
        /// <returns>ID del Customer resuelto.</returns>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si el usuario autenticado no existe.
        /// </exception>
        private async Task<int> ResolveCustomerIdAsync(int? customerId, int? authenticatedUserId)
        {
            // ==============================================================================
            // USUARIO AUTENTICADO
            // ==============================================================================
            // Si hay usuario autenticado, siempre usa el Customer vinculado a su cuenta
            // Ignora el customerId del request para evitar fraude
            if (authenticatedUserId.HasValue)
            {
                var linked = await ResolveLinkedCustomerIdAsync(authenticatedUserId.Value);
                if (linked.HasValue)
                    return linked.Value;

                throw new InvalidOperationException("El usuario autenticado no existe.");
            }

            // ==============================================================================
            // CUSTOMER ESPECIFICADO
            // ==============================================================================
            // Si no hay usuario autenticado pero se especificó un CustomerId, úsalo
            if (customerId.HasValue)
                return customerId.Value;

            // ==============================================================================
            // CUSTOMER ANÓNIMO
            // ==============================================================================
            // Si no hay usuario autenticado ni CustomerId, crea un Customer anónimo
            var anonymous = new Customer(new CustomerName("Cliente Anónimo"));
            await _unitOfWork.Customers.AddAsync(anonymous);
            await _unitOfWork.SaveChangesAsync();
            return anonymous.Id;
        }

        /// <summary>
        /// Método privado que resuelve el Customer vinculado a un usuario autenticado.
        /// Prioriza el Customer existente con el mismo email para evitar pedidos huérfanos.
        /// </summary>
        /// <param name="authenticatedUserId">ID del usuario autenticado.</param>
        /// <returns>ID del Customer vinculado, o null si el usuario no existe.</returns>
        /// <remarks>
        /// Este método es importante para mantener la integridad referencial:
        /// - Busca si ya existe un Customer con el mismo email que el usuario
        /// - Si existe y está vinculado a otro Customer, corrige el vínculo
        /// - Si no existe un Customer con ese email, crea uno nuevo
        /// - Esto evita que los pedidos queden huérfanos sin Customer asociado
        /// </remarks>
        private async Task<int?> ResolveLinkedCustomerIdAsync(int authenticatedUserId)
        {
            // Busca el usuario por ID
            var user = await _unitOfWork.Users.GetByIdAsync(authenticatedUserId);
            if (user is null)
                return null;

            // Busca si ya existe un Customer con el mismo email que el usuario
            var existingByEmail = await _unitOfWork.Customers.GetByEmailAsync(user.Email.Value);
            if (existingByEmail is not null)
            {
                // Si el usuario está vinculado a un Customer diferente, corrige el vínculo
                if (user.CustomerId != existingByEmail.Id)
                {
                    user.LinkCustomer(existingByEmail.Id);
                    _unitOfWork.Users.Update(user);
                    await _unitOfWork.SaveChangesAsync();
                }

                return existingByEmail.Id;
            }

            // Si no existe un Customer con ese email, usa el Customer vinculado al usuario
            if (user.CustomerId.HasValue)
                return user.CustomerId.Value;

            // Si no tiene Customer vinculado, crea uno nuevo
            var customer = new Customer(
                new CustomerName(user.Name.Value),
                new CustomerEmail(user.Email.Value));
            await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            // Vincula el usuario al Customer creado
            user.LinkCustomer(customer.Id);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return customer.Id;
        }

        /// <summary>
        /// Método privado que sincroniza los datos de contacto del Customer.
        /// Actualiza teléfono y documento si se proporcionan en la venta.
        /// </summary>
        /// <param name="customerId">ID del Customer a actualizar.</param>
        /// <param name="phone">Teléfono de contacto (opcional).</param>
        /// <param name="document">Documento de identidad (opcional).</param>
        private async Task SyncCustomerContactAsync(int customerId, string? phone, string? document)
        {
            // Si no se proporcionaron datos de contacto, no hace nada
            if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(document))
                return;

            // Busca el Customer por ID
            var customer = await _unitOfWork.Customers.GetByIdAsync(customerId);
            if (customer is null)
                return;

            // Determina los nuevos valores (usa los existentes si no se proporcionaron nuevos)
            var nextPhone = !string.IsNullOrWhiteSpace(phone)
                ? new Phone(phone)
                : customer.PhoneNumber;
            var nextDocument = !string.IsNullOrWhiteSpace(document)
                ? new DocumentNumber(document)
                : customer.DocumentNumber;

            // Actualiza el Customer con los nuevos datos de contacto
            customer.Update(customer.Name, customer.Email, nextPhone, nextDocument);
            _unitOfWork.Customers.Update(customer);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Método privado genérico que resuelve el ID de una entidad de catálogo por su nombre.
        /// Usa reflexión para buscar en entidades que tienen una propiedad Name de tipo ValueObject.
        /// </summary>
        /// <typeparam name="T">Tipo de entidad de catálogo (SaleOrigin, SaleStatus, MovementType, etc.).</typeparam>
        /// <param name="name">Nombre de la entidad a buscar.</param>
        /// <returns>ID de la entidad encontrada.</returns>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si no existe la entidad con el nombre especificado.
        /// </exception>
        private async Task<int> GetLookupIdByNameAsync<T>(string name) where T : Domain.Common.BaseEntity
        {
            // Obtiene todas las entidades del tipo especificado
            var items = await _unitOfWork.Repository<T>().GetAllAsync();

            // Usa reflexión para acceder a la propiedad Name (que es un ValueObject)
            var nameProperty = typeof(T).GetProperty("Name");
            var match = items.FirstOrDefault(i =>
            {
                // Obtiene el ValueObject Name de la entidad
                var nameValueObject = nameProperty?.GetValue(i);
                // Obtiene la propiedad Value del ValueObject
                var valueProperty = nameValueObject?.GetType().GetProperty("Value");
                // Obtiene el valor string del ValueObject
                var value = valueProperty?.GetValue(nameValueObject) as string;
                // Compara el valor con el nombre buscado (case-insensitive)
                return string.Equals(value, name, StringComparison.OrdinalIgnoreCase);
            });

            if (match is null)
                throw new InvalidOperationException($"No se encontró '{name}' en el catálogo {typeof(T).Name}.");

            return match.Id;
        }

        /// <summary>
        /// Método estático que convierte una entidad Sale a SaleDto.
        /// Mapea las propiedades de la entidad a las propiedades del DTO.
        /// </summary>
        /// <param name="sale">Entidad Sale a convertir.</param>
        /// <returns>DTO SaleDto con los datos de la venta.</returns>
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
            // Mapea los detalles de venta (líneas de pedido)
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
