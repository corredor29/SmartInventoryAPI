using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.DTOs.Products.ProductStatus
{
    public class ProductStatusDto
    {
        public int ProductStatusId { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}