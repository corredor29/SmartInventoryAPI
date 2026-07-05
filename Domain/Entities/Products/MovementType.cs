using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.ValueObjects.Products.Category;
using Domain.ValueObjects.Products.MovementType;

namespace Domain.Entities.Products
{
    public sealed  class MovementType : BaseEntity
    {
        public MovementTypeName Name { get; private set; } = null!;

        public ICollection<InventoryMovement> InventoryMovements { get; private set; } = new List<InventoryMovement>();

        private MovementType() { }

        public MovementType(MovementTypeName name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public void Update(MovementTypeName name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}