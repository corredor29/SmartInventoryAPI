using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.DTOs.Sales.SaleOrigin
{
    public sealed class CreateSaleOriginRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}