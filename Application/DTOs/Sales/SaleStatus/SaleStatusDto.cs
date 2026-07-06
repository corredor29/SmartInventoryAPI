using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.DTOs.Sales.SaleStatus
{
    public sealed class SaleStatusDto
    {
         public int SaleStatusId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}