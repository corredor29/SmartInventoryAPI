using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.DTOs.Chats.ChatSessionStatus
{
    public sealed class ChatSessionStatusDto
    {
        public int ChatSessionStatusId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}