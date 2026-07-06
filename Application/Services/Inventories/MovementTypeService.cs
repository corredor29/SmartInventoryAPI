using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Inventories;
using Application.DTOs.Inventories.MovementType;
using Domain.Entities.Products;
using Domain.ValueObject.Products.MovementType;

namespace Application.Services.Inventories
{
    public class MovementTypeService : IMovementTypeService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MovementTypeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<MovementTypeDto>> GetAllAsync()
        {
            var types = await _unitOfWork.Repository<MovementType>().GetAllAsync();
            return types.Select(ToDto).ToList();
        }

        public async Task<MovementTypeDto?> GetByIdAsync(int id)
        {
            var type = await _unitOfWork.Repository<MovementType>().GetByIdAsync(id);
            return type is null ? null : ToDto(type);
        }

        public async Task<MovementTypeDto> CreateAsync(CreateMovementTypeRequest request)
        {
            var type = new MovementType(MovementTypeName.Create(request.Name));

            await _unitOfWork.Repository<MovementType>().AddAsync(type);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(type);
        }

        public async Task<MovementTypeDto?> UpdateAsync(int id, UpdateMovementTypeRequest request)
        {
            var type = await _unitOfWork.Repository<MovementType>().GetByIdAsync(id);
            if (type is null) return null;

            type.Update(MovementTypeName.Create(request.Name));
            _unitOfWork.Repository<MovementType>().Update(type);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(type);
        }
        public async Task<bool> DeleteAsync(int id)
        {
            var type = await _unitOfWork.Repository<MovementType>().GetByIdAsync(id);
            if (type is null) return false;

            _unitOfWork.Repository<MovementType>().Remove(type);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static MovementTypeDto ToDto(MovementType type) => new()
        {
            MovementTypeId = type.Id,
            Name = type.Name.Value,
        };
    }
}