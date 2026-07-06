using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.Entities.Customers;
using Domain.Entities.Chats;
using Domain.ValueObject.Users.User;
namespace Domain.Entities.Users
{
    public sealed class User : BaseEntity
    {
        public int      RoleId       { get; private set; }
        public int?     CustomerId   { get; private set; }
        public UserName Name         { get; private set; } = null!;
        public Email    Email        { get; private set; } = null!;
        public PasswordHash PasswordHash { get; private set; } = null!;

        public Role      Role     { get; private set; } = null!;
        public Customer?  Customer { get; private set; }
        public ICollection<ChatEscalation> AssignedEscalations { get; private set; } = new List<ChatEscalation>();

        private User() { }

        public User(int roleId, UserName name, Email email, PasswordHash passwordHash, int? customerId = null)
        {
            RoleId       = roleId > 0 ? roleId : throw new ArgumentException("RoleId must be greater than 0.");
            Name         = name         ?? throw new ArgumentNullException(nameof(name));
            Email        = email        ?? throw new ArgumentNullException(nameof(email));
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
            CustomerId   = customerId;
        }

        public void Update(UserName name, Email email)
        {
            Name  = name  ?? throw new ArgumentNullException(nameof(name));
            Email = email ?? throw new ArgumentNullException(nameof(email));
        }

        public void ChangePassword(PasswordHash passwordHash)
        {
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        }

        public void ChangeRole(int roleId)
        {
            RoleId = roleId > 0 ? roleId : throw new ArgumentException("RoleId must be greater than 0.");
        }

        public void LinkCustomer(int customerId)
        {
            CustomerId = customerId > 0 ? customerId : throw new ArgumentException("CustomerId must be greater than 0.");
        }
    }
}