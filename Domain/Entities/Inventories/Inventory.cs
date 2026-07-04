using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.Entities.Products;
using Domain.ValueObject.Inventories.Inventory;
namespace Domain.Entities.Inventories
{
    public sealed class Inventory : BaseEntity
    {
        public int           ProductId    { get; private set; }
        public StockQuantity CurrentStock { get; private set; } = null!;

        public Product Product { get; private set; } = null!;
        public ICollection<InventoryMovement> Movements { get; private set; } = new List<InventoryMovement>();

        private Inventory() { }

        public Inventory(int productId, StockQuantity? currentStock = null)
        {
            ProductId    = productId > 0 ? productId : throw new ArgumentException("ProductId must be greater than 0.");
            CurrentStock = currentStock ?? StockQuantity.Zero;
        }

        public void IncreaseStock(int amount)
        {
            CurrentStock = CurrentStock.Increase(amount);
        }

        public void DecreaseStock(int amount)
        {
            CurrentStock = CurrentStock.Decrease(amount);
        }

        public bool HasEnoughStock(int requestedAmount) => CurrentStock.HasEnoughFor(requestedAmount);
    }
}