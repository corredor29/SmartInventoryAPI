using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Chats;
using Application.DTOs.Chats.EscalationStatus;
using Domain.Entities.Chats;
using Domain.ValueObject.Chats.EscalationStatus;

namespace Application.Services.Chats
{
    public class EscalationStatusService : IEscalationStatusService
    {
        private readonly IUnitOfWork _unitOfWork;

        public EscalationStatusService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<EscalationStatusDto>> GetAllAsync()
        {
            var statuses = await _unitOfWork.Repository<EscalationStatus>().GetAllAsync();
            return statuses.Select(ToDto).ToList();
        }

        public async Task<EscalationStatusDto?> GetByIdAsync(int id)
        {
            var status = await _unitOfWork.Repository<EscalationStatus>().GetByIdAsync(id);
            return status is null ? null : ToDto(status);
        }

        public async Task<EscalationStatusDto> CreateAsync(CreateEscalationStatusRequest request)
        {
            var status = new EscalationStatus(new EscalationStatusName(request.Name));

            await _unitOfWork.Repository<EscalationStatus>().AddAsync(status);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(status);
        }

        public async Task<EscalationStatusDto?> UpdateAsync(int id, UpdateEscalationStatusRequest request)
        {
            var status = await _unitOfWork.Repository<EscalationStatus>().GetByIdAsync(id);
            if (status is null) return null;

            status.Update(new EscalationStatusName(request.Name));
            _unitOfWork.Repository<EscalationStatus>().Update(status);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(status);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var status = await _unitOfWork.Repository<EscalationStatus>().GetByIdAsync(id);
            if (status is null) return false;

            _unitOfWork.Repository<EscalationStatus>().Remove(status);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static EscalationStatusDto ToDto(EscalationStatus status) => new()
        {
            EscalationStatusId = status.Id,
            Name = status.Name.Value,
        };
    }
}