using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.ValueObject.Chats.SenderType;
namespace Domain.Entities.Chats
{
    public sealed class SenderType : BaseEntity
    {
        public SenderTypeName Name { get; private set; } = null!; 

        public ICollection<ChatMessage> Messages { get; private set; } = new List<ChatMessage>();

        private SenderType() { }

        public SenderType(SenderTypeName name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public void Update(SenderTypeName name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}