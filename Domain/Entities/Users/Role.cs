using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.ValueObject.Users.Role;

namespace Domain.Entities.Users
{
    public sealed class Role : BaseEntity
    {
        public RoleName Name { get; private set; } = null!;

        private Role() { }

        public Role(RoleName name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public void Update(RoleName name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}