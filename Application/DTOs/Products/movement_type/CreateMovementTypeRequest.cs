using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.DTOs.Products.movement_type
{
    public sealed class CreateMovementTypeRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}