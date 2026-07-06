using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.ValueObject.Sales.SaleStatus;

namespace Domain.Entities.Sales
{
    public sealed class SaleStatus : BaseEntity
    {
        public SaleStatusName Name { get; private set; } = null!;

        private readonly List<Sale> _sales = new();
        public IReadOnlyCollection<Sale> Sales => _sales.AsReadOnly();

        private SaleStatus() { }

        public SaleStatus(SaleStatusName name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public void UpdateName(SaleStatusName name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}