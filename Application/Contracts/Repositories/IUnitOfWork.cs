using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Common;

namespace Application.Contracts.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IProductRepository Products { get; }
        IInventoryRepository Inventory { get; }
        ISaleRepository Sales { get; }
        IInvoiceRepository Invoices { get; }
        ICustomerRepository Customers { get; }
        IUserRepository Users { get; }
        IChatSessionRepository ChatSessions { get; }
        IChatEscalationRepository ChatEscalations { get; }
        IRepository<T> Repository<T>() where T : BaseEntity;

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}