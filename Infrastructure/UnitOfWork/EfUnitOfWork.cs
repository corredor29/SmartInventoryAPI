using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Application.Contracts.Repositories;
using Domain.Common;
using Infrastructure.Persistence;

namespace Infrastructure.UnitOfWork
{
    public class EfUnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;
        private readonly Dictionary<Type, object> _genericRepositories = new();

        public IProductRepository Products { get; }
        public IInventoryRepository Inventory { get; }
        public IInventoryMovementRepository InventoryMovements { get; }
        public ISaleRepository Sales { get; }
        public IInvoiceRepository Invoices { get; }
        public ICustomerRepository Customers { get; }
        public IUserRepository Users { get; }
        public IChatSessionRepository ChatSessions { get; }
        public IChatEscalationRepository ChatEscalations { get; }

        public EfUnitOfWork(AppDbContext context)
        {
            _context = context;

            Products = new Repositories.ProductRepository(_context);
            Inventory = new Repositories.InventoryRepository(_context);
            InventoryMovements = new Repositories.InventoryMovementRepository(_context);
            Sales = new Repositories.SaleRepository(_context);
            Invoices = new Repositories.InvoiceRepository(_context);
            Customers = new Repositories.CustomerRepository(_context);
            Users = new Repositories.UserRepository(_context);
            ChatSessions = new Repositories.ChatSessionRepository(_context);
            ChatEscalations = new Repositories.ChatEscalationRepository(_context);
        }

        public IRepository<T> Repository<T>() where T : BaseEntity
        {
            var type = typeof(T);

            if (!_genericRepositories.ContainsKey(type))
            {
                var repositoryInstance = new Repositories.Repository<T>(_context);
                _genericRepositories[type] = repositoryInstance;
            }

            return (IRepository<T>)_genericRepositories[type];
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                if (_transaction != null)
                    await _transaction.CommitAsync();
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}