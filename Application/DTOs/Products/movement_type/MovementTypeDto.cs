using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.DTOs.Products.movement_type
{
    public sealed class MovementTypeDto
    {
        public int MovementTypeId { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}