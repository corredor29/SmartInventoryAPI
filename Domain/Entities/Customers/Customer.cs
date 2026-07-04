using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.Entities.Sales;
using Domain.Entities.Chats;
using Domain.ValueObject.Customers.Customer;
namespace Domain.Entities.Customers
{
    public sealed class Customer : BaseEntity
    {
        public CustomerName    Name           { get; private set; } = null!;
        public CustomerEmail?  Email          { get; private set; }
        public Phone?          PhoneNumber    { get; private set; }
        public DocumentNumber? DocumentNumber { get; private set; }

        public ICollection<Sale> Sales { get; private set; } = new List<Sale>();
        public ICollection<ChatSession> ChatSessions { get; private set; } = new List<ChatSession>();

        private Customer() { }

        public Customer(CustomerName name, CustomerEmail? email = null, Phone? phoneNumber = null, DocumentNumber? documentNumber = null)
        {
            Name           = name ?? throw new ArgumentNullException(nameof(name));
            Email          = email;
            PhoneNumber    = phoneNumber;
            DocumentNumber = documentNumber;
        }

        public void Update(CustomerName name, CustomerEmail? email, Phone? phoneNumber, DocumentNumber? documentNumber)
        {
            Name           = name ?? throw new ArgumentNullException(nameof(name));
            Email          = email;
            PhoneNumber    = phoneNumber;
            DocumentNumber = documentNumber;
        }
    }
}