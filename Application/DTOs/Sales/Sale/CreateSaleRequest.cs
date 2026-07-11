using System.Collections.Generic;

namespace Application.DTOs.Sales.Sale
{
    public class CreateSaleRequest
    {
        public int? CustomerId { get; set; }
        public string? SessionId { get; set; }
        public List<SaleItemRequest> Items { get; set; } = new();
        public string Origin { get; set; } = "Manual";
        /// <summary>
        /// "Efectivo" o "Tarjeta". Obligatorio para ventas Manual; opcional en Chatbot (default Efectivo).
        /// </summary>
        public string? PaymentMethod { get; set; }

        /// <summary>Dirección de entrega. Obligatoria en ventas Manual.</summary>
        public string? DeliveryAddress { get; set; }
        public decimal? DeliveryLat { get; set; }
        public decimal? DeliveryLng { get; set; }
        /// <summary>Teléfono de contacto. Obligatorio en ventas Manual.</summary>
        public string? ContactPhone { get; set; }
        /// <summary>Documento de identidad. Obligatorio en ventas Manual.</summary>
        public string? ContactDocument { get; set; }
    }

    public class SaleItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
