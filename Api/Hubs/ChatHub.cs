using Microsoft.AspNetCore.SignalR;
using Application.Contracts.Services.Chats;
using Application.DTOs.Chats.ChatMessage;

namespace Api.Hubs
{
    public class ChatHub : Hub
    {
        private const string AdvisorsGroup = "Advisors";
        private readonly IChatMessageService _chatMessageService;

        public ChatHub(IChatMessageService chatMessageService)
        {
            _chatMessageService = chatMessageService;
        }
        public async Task JoinAsAdvisor()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdvisorsGroup);
        }


        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetSessionGroup(sessionId));
        }

        public async Task LeaveSession(string sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetSessionGroup(sessionId));
        }

        public async Task SendMessageToSession(string sessionId, string content, int senderTypeId)
        {
            if (!int.TryParse(sessionId, out var chatSessionId))
                return;

            var message = await _chatMessageService.CreateAsync(new CreateChatMessageRequest
            {
                ChatSessionId = chatSessionId,
                SenderTypeId = senderTypeId,
                Content = content,
            });

            await Clients.Group(GetSessionGroup(sessionId)).SendAsync("ReceiveMessage", message);
        }

        private static string GetSessionGroup(string sessionId) => $"session-{sessionId}";
    }
}