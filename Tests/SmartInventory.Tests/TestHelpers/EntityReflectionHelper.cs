using Domain.Common;

namespace SmartInventory.Tests.TestHelpers
{
    internal static class EntityReflectionHelper
    {
        public static void SetId(BaseEntity entity, int id)
        {
            typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(entity, id);
        }

        public static void SetProperty<T>(T target, string propertyName, object? value)
        {
            var property = typeof(T).GetProperty(propertyName)
                ?? throw new InvalidOperationException($"Property '{propertyName}' not found on type '{typeof(T).Name}'.");
            property.SetValue(target, value);
        }
    }
}
