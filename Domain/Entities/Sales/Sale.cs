using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Common;
using Domain.Entities.Customers;
using Domain.Entities.Invoices;
using Domain.ValueObject.Sales.Sale;

namespace Domain.Entities.Sales
{
    public sealed class Sale : BaseEntity
    {
        public int CustomerId { get; private set; }
        public int SaleOriginId { get; private set; }
        public int SaleStatusId { get; private set; }
        public SaleDate SaleDate { get; private set; } = null!;
        public PaymentMethod PaymentMethod { get; private set; } = null!;

        public string? DeliveryAddress { get; private set; }
        public decimal? DeliveryLat { get; private set; }
        public decimal? DeliveryLng { get; private set; }
        public string? ContactPhone { get; private set; }
        public string? ContactDocument { get; private set; }

        public Customer Customer { get; private set; } = null!;
        public SaleOrigin SaleOrigin { get; private set; } = null!;
        public SaleStatus SaleStatus { get; private set; } = null!;
        public Invoice? Invoice { get; private set; }

        private readonly List<SaleDetail> _details = new();
        public IReadOnlyCollection<SaleDetail> Details => _details.AsReadOnly();

        private Sale() { }

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
            CustomerId = customerId > 0 ? customerId : throw new ArgumentException("CustomerId must be greater than 0.");
            SaleOriginId = saleOriginId > 0 ? saleOriginId : throw new ArgumentException("SaleOriginId must be greater than 0.");
            SaleStatusId = saleStatusId > 0 ? saleStatusId : throw new ArgumentException("SaleStatusId must be greater than 0.");
            PaymentMethod = paymentMethod ?? throw new ArgumentNullException(nameof(paymentMethod));
            SaleDate = saleDate ?? SaleDate.Now();
            SetDeliveryInfo(deliveryAddress, deliveryLat, deliveryLng, contactPhone, contactDocument);
        }

        public void SetDeliveryInfo(
            string? deliveryAddress,
            decimal? deliveryLat,
            decimal? deliveryLng,
            string? contactPhone,
            string? contactDocument)
        {
            DeliveryAddress = string.IsNullOrWhiteSpace(deliveryAddress) ? null : deliveryAddress.Trim();
            DeliveryLat = deliveryLat;
            DeliveryLng = deliveryLng;
            ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone.Trim();
            ContactDocument = string.IsNullOrWhiteSpace(contactDocument) ? null : contactDocument.Trim();
        }

        public void AddDetail(SaleDetail detail)
        {
            if (detail is null)
                throw new ArgumentNullException(nameof(detail));
            _details.Add(detail);
        }

        public void ChangeStatus(int saleStatusId)
        {
            SaleStatusId = saleStatusId > 0 ? saleStatusId : throw new ArgumentException("SaleStatusId must be greater than 0.");
        }

        public decimal GetTotal() => _details.Sum(d => d.GetSubtotal());
    }
}
