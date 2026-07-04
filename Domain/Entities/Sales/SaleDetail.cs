using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.Entities.Products;
using Domain.ValueObject.Sales.SaleDetail;
namespace Domain.Entities.Sales
{
    public sealed class SaleDetail : BaseEntity
    {
        public int                 SaleId    { get; private set; }
        public int                 ProductId { get; private set; }
        public SaleDetailQuantity  Quantity  { get; private set; } = null!;
        public UnitPrice           UnitPrice { get; private set; } = null!;

        public Sale    Sale    { get; private set; } = null!;
        public Product Product { get; private set; } = null!;

        private SaleDetail() { }

        public SaleDetail(int saleId, int productId, SaleDetailQuantity quantity, UnitPrice unitPrice)
        {
            SaleId    = saleId    > 0 ? saleId    : throw new ArgumentException("SaleId must be greater than 0.");
            ProductId = productId > 0 ? productId : throw new ArgumentException("ProductId must be greater than 0.");
            Quantity  = quantity  ?? throw new ArgumentNullException(nameof(quantity));
            UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));
        }

        public decimal GetSubtotal() => Quantity.Value * UnitPrice.Value;
    }
}