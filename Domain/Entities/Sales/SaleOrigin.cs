using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.ValueObject.Sales.SaleOrigin;

namespace Domain.Entities.Sales
{
    public sealed class SaleOrigin : BaseEntity
    {
        public SaleOriginName Name { get; private set; } = null!;

        private readonly List<Sale> _sales = new();
        public IReadOnlyCollection<Sale> Sales => _sales.AsReadOnly();

        private SaleOrigin() { }

        public SaleOrigin(SaleOriginName name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public void UpdateName(SaleOriginName name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}