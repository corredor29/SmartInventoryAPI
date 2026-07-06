using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Chats;
using Application.DTOs.Chats.ChatSessionStatus;
using Domain.Entities.Chats;
using Domain.ValueObject.Chats.ChatSessionStatus;

namespace Application.Services.Chats
{
    public class ChatSessionStatusService : IChatSessionStatusService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChatSessionStatusService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<ChatSessionStatusDto>> GetAllAsync()
        {
            var statuses = await _unitOfWork.Repository<ChatSessionStatus>().GetAllAsync();
            return statuses.Select(ToDto).ToList();
        }

        public async Task<ChatSessionStatusDto?> GetByIdAsync(int id)
        {
            var status = await _unitOfWork.Repository<ChatSessionStatus>().GetByIdAsync(id);
            return status is null ? null : ToDto(status);
        }

        public async Task<ChatSessionStatusDto> CreateAsync(CreateChatSessionStatusRequest request)
        {
            var status = new ChatSessionStatus(ChatSessionStatusName.Create(request.Name));

            await _unitOfWork.Repository<ChatSessionStatus>().AddAsync(status);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(status);
        }

        public async Task<ChatSessionStatusDto?> UpdateAsync(int id, UpdateChatSessionStatusRequest request)
        {
            var status = await _unitOfWork.Repository<ChatSessionStatus>().GetByIdAsync(id);
            if (status is null) return null;

            status.UpdateName(ChatSessionStatusName.Create(request.Name));
            _unitOfWork.Repository<ChatSessionStatus>().Update(status);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(status);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var status = await _unitOfWork.Repository<ChatSessionStatus>().GetByIdAsync(id);
            if (status is null) return false;

            _unitOfWork.Repository<ChatSessionStatus>().Remove(status);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static ChatSessionStatusDto ToDto(ChatSessionStatus status) => new()
        {
            ChatSessionStatusId = status.Id,
            Name = status.Name.Value,
        };
    }
}