using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Common;
using Domain.Entities.Customers;
using Domain.Entities.Invoices;
using Domain.ValueObject.Sales.Sale;

namespace Domain.Entities.Sales
{
    /// <summary>
    /// Entidad que representa una venta/pedido en el sistema.
    /// Contiene información del cliente, origen, estado, método de pago,
    /// datos de entrega y los detalles de la venta (líneas de pedido).
    /// </summary>
    /// <remarks>
    /// Esta entidad:
    /// - Usa Value Objects para validación de dominio (SaleDate, PaymentMethod)
    /// - Tiene una relación muchos-a-uno con Customer
    /// - Tiene una relación uno-a-uno con Invoice (factura)
    /// - Tiene una relación uno-a-muchos con SaleDetail (líneas de pedido)
    - Es sealed para prevenir herencia y mantener la integridad del dominio
    /// - Calcula el total dinámicamente sumando los subtotales de los detalles
    /// </remarks>
    public sealed class Sale : BaseEntity
    {
        // ==============================================================================
        // PROPIEDADES - DATOS DE LA VENTA
        // ==============================================================================
        /// <summary>ID del cliente que realizó la compra (FK).</summary>
        public int CustomerId { get; private set; }

        /// <summary>ID del origen de la venta (Manual/Chatbot) (FK).</summary>
        public int SaleOriginId { get; private set; }

        /// <summary>ID del estado de la venta (Pendiente/Completada/Cancelada) (FK).</summary>
        public int SaleStatusId { get; private set; }

        /// <summary>Fecha de la venta (Value Object).</summary>
        public SaleDate SaleDate { get; private set; } = null!;

        /// <summary>Método de pago (Efectivo/Tarjeta) (Value Object).</summary>
        public PaymentMethod PaymentMethod { get; private set; } = null!;

        // ==============================================================================
        // PROPIEDADES - DATOS DE ENTREGA Y CONTACTO
        // ==============================================================================
        /// <summary>Dirección de entrega del pedido.</summary>
        public string? DeliveryAddress { get; private set; }

        /// <summary>Latitud de la ubicación de entrega (opcional).</summary>
        public decimal? DeliveryLat { get; private set; }

        /// <summary>Longitud de la ubicación de entrega (opcional).</summary>
        public decimal? DeliveryLng { get; private set; }

        /// <summary>Teléfono de contacto del cliente.</summary>
        public string? ContactPhone { get; private set; }

        /// <summary>Documento de identidad del cliente.</summary>
        public string? ContactDocument { get; private set; }

        // ==============================================================================
        // PROPIEDADES DE NAVEGACIÓN - RELACIONES
        // ==============================================================================
        /// <summary>Cliente que realizó la compra (relación de navegación).</summary>
        public Customer Customer { get; private set; } = null!;

        /// <summary>Origen de la venta (relación de navegación).</summary>
        public SaleOrigin SaleOrigin { get; private set; } = null!;

        /// <summary>Estado de la venta (relación de navegación).</summary>
        public SaleStatus SaleStatus { get; private set; } = null!;

        /// <summary>Factura asociada a la venta (relación uno-a-uno).</summary>
        public Invoice? Invoice { get; private set; }

        // ==============================================================================
        // COLECCIÓN DE DETALLES DE VENTA
        // ==============================================================================
        /// <summary>
        /// Lista interna de detalles de venta (líneas de pedido).
        /// Es privada para mantener la encapsulación.
        /// </summary>
        private readonly List<SaleDetail> _details = new();

        /// <summary>
        /// Colección de solo lectura de detalles de venta.
        /// Expuesta públicamente para lectura, pero no para modificación directa.
        /// </summary>
        public IReadOnlyCollection<SaleDetail> Details => _details.AsReadOnly();

        // ==============================================================================
        // CONSTRUCTOR PRIVADO PARA EF CORE
        // ==============================================================================
        /// <summary>
        /// Constructor privado requerido por Entity Framework Core.
        /// No debe usarse directamente para creación de ventas.
        /// </summary>
        private Sale() { }

        // ==============================================================================
        // CONSTRUCTOR PÚBLICO - CREACIÓN DE VENTA
        // ==============================================================================
        /// <summary>
        /// Constructor público para crear una nueva venta.
        /// Valida todos los parámetros usando Value Objects.
        /// </summary>
        /// <param name="customerId">ID del cliente (debe ser mayor a 0).</param>
        /// <param name="saleOriginId">ID del origen de venta (debe ser mayor a 0).</param>
        /// <param name="saleStatusId">ID del estado de venta (debe ser mayor a 0).</param>
        /// <param name="paymentMethod">Método de pago (Value Object).</param>
        /// <param name="saleDate">Fecha de la venta (opcional, usa fecha actual si es null).</param>
        /// <param name="deliveryAddress">Dirección de entrega (opcional).</param>
        /// <param name="deliveryLat">Latitud de entrega (opcional).</param>
        /// <param name="deliveryLng">Longitud de entrega (opcional).</param>
        /// <param name="contactPhone">Teléfono de contacto (opcional).</param>
        /// <param name="contactDocument">Documento de identidad (opcional).</param>
        /// <exception cref="ArgumentException">Se lanza si los IDs no son válidos.</exception>
        /// <exception cref="ArgumentNullException">Se lanza si paymentMethod es null.</exception>
        public Sale(
            int customerId,
            int saleOriginId,
            int saleStatusId,
            PaymentMethod paymentMethod,
            SaleDate? saleDate = null,
            string? deliveryAddress = null,
            decimal? deliveryLat = null,
            decimal? deliveryLng = null,
            string? contactPhone = null,
            string? contactDocument = null)
        {
            // Valida que los IDs sean positivos
            CustomerId = customerId > 0 ? customerId : throw new ArgumentException("CustomerId must be greater than 0.");
            SaleOriginId = saleOriginId > 0 ? saleOriginId : throw new ArgumentException("SaleOriginId must be greater than 0.");
            SaleStatusId = saleStatusId > 0 ? saleStatusId : throw new ArgumentException("SaleStatusId must be greater than 0.");
            // Valida que el Value Object no sea null
            PaymentMethod = paymentMethod ?? throw new ArgumentNullException(nameof(paymentMethod));
            // Usa la fecha actual si no se proporcionó
            SaleDate = saleDate ?? SaleDate.Now();
            // Establece la información de entrega
            SetDeliveryInfo(deliveryAddress, deliveryLat, deliveryLng, contactPhone, contactDocument);
        }

        // ==============================================================================
        // MÉTODOS DE DOMINIO
        // ==============================================================================
        /// <summary>
        /// Establece o actualiza la información de entrega y contacto de la venta.
        /// Limpia espacios en blanco de los strings para mantener consistencia.
        /// </summary>
        /// <param name="deliveryAddress">Dirección de entrega.</param>
        /// <param name="deliveryLat">Latitud de entrega.</param>
        /// <param name="deliveryLng">Longitud de entrega.</param>
        /// <param name="contactPhone">Teléfono de contacto.</param>
        /// <param name="contactDocument">Documento de identidad.</param>
        public void SetDeliveryInfo(
            string? deliveryAddress,
            decimal? deliveryLat,
            decimal? deliveryLng,
            string? contactPhone,
            string? contactDocument)
        {
            // Limpia espacios en blanco y asigna null si está vacío
            DeliveryAddress = string.IsNullOrWhiteSpace(deliveryAddress) ? null : deliveryAddress.Trim();
            DeliveryLat = deliveryLat;
            DeliveryLng = deliveryLng;
            ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone.Trim();
            ContactDocument = string.IsNullOrWhiteSpace(contactDocument) ? null : contactDocument.Trim();
        }

        /// <summary>
        /// Agrega un detalle de venta (línea de pedido) a la venta.
        /// </summary>
        /// <param name="detail">Detalle de venta a agregar.</param>
        /// <exception cref="ArgumentNullException">Se lanza si detail es null.</exception>
        public void AddDetail(SaleDetail detail)
        {
            if (detail is null)
                throw new ArgumentNullException(nameof(detail));
            _details.Add(detail);
        }

        /// <summary>
        /// Cambia el estado de la venta (Pendiente/Completada/Cancelada).
        /// </summary>
        /// <param name="saleStatusId">Nuevo ID de estado (debe ser mayor a 0).</param>
        /// <exception cref="ArgumentException">Se lanza si saleStatusId no es válido.</exception>
        public void ChangeStatus(int saleStatusId)
        {
            SaleStatusId = saleStatusId > 0 ? saleStatusId : throw new ArgumentException("SaleStatusId must be greater than 0.");
        }

        /// <summary>
        /// Calcula el total de la venta sumando los subtotales de todos los detalles.
        /// </summary>
        /// <returns>Total de la venta (suma de cantidad * precio unitario de cada detalle).</returns>
        public decimal GetTotal() => _details.Sum(d => d.GetSubtotal());
    }
}
