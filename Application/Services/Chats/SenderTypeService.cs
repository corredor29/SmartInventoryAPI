using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Chats;
using Application.DTOs.Chats.SenderType;
using Domain.Entities.Chats;
using Domain.ValueObject.Chats.SenderType;

namespace Application.Services.Chats
{
    public class SenderTypeService : ISenderTypeService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SenderTypeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<SenderTypeDto>> GetAllAsync()
        {
            var types = await _unitOfWork.Repository<SenderType>().GetAllAsync();
            return types.Select(ToDto).ToList();
        }

        public async Task<SenderTypeDto?> GetByIdAsync(int id)
        {
            var type = await _unitOfWork.Repository<SenderType>().GetByIdAsync(id);
            return type is null ? null : ToDto(type);
        }

        public async Task<SenderTypeDto> CreateAsync(CreateSenderTypeRequest request)
        {
            var type = new SenderType(new SenderTypeName(request.Name));

            await _unitOfWork.Repository<SenderType>().AddAsync(type);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(type);
        }

        public async Task<SenderTypeDto?> UpdateAsync(int id, UpdateSenderTypeRequest request)
        {
            var type = await _unitOfWork.Repository<SenderType>().GetByIdAsync(id);
            if (type is null) return null;

            type.Update(new SenderTypeName(request.Name));
            _unitOfWork.Repository<SenderType>().Update(type);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(type);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var type = await _unitOfWork.Repository<SenderType>().GetByIdAsync(id);
            if (type is null) return false;

            _unitOfWork.Repository<SenderType>().Remove(type);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static SenderTypeDto ToDto(SenderType type) => new()
        {
            SenderTypeId = type.Id,
            Name = type.Name.Value,
        };
    }
}