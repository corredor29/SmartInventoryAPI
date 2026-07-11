using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Chats.ChatEscalation
{
    public record ResolvedAt
    {
        public DateTime Value { get; }
        public ResolvedAt(DateTime value)
        {
            // Normalizar a UTC: Npgsql puede devolver Unspecified/Local y romper comparaciones.
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };
            Value = utc;
        }

        public static ResolvedAt Now() => new(DateTime.UtcNow);
    }
}