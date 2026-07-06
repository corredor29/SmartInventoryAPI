using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.DTOs.Products.Category
{
    public sealed class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}