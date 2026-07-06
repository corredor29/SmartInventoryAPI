using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.ValueObject.Products.Category
{
    public sealed record CategoryName
    {
        public string Value{ get; private set;} = default!;
        private CategoryName(){}
        private CategoryName(string value)
        {
            Value = value;
        }
        public static CategoryName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Category name cannot be empty.");
            if (value.Length > 150)
                throw new ArgumentException("Category name cannot exceed 150 characters.");
            return new CategoryName(value.Trim());
        }
        public override string ToString() => Value;
        
    }
}