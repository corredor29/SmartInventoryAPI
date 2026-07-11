using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Application.Contracts.Repositories;
using Domain.Entities.Products;
using Infrastructure.Persistence;
using Pgvector.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "al", "con", "de", "del", "el", "en", "es", "esta", "este", "hay",
            "la", "las", "lo", "los", "me", "mi", "mis", "no", "o", "para", "por",
            "que", "quiero", "se", "si", "su", "sus", "te", "ti", "tu", "un", "una",
            "unas", "unos", "y", "yo", "busco", "necesito", "gustan", "gusta",
            "tienen", "tiene", "tienes", "disponible", "disponibles", "modelo",
            "modelos", "opcion", "opciones", "algo", "algun", "alguna", "algunos",
        };

        public ProductRepository(AppDbContext context) : base(context) { }

        public async Task<IReadOnlyList<Product>> GetAllWithDetailsAsync()
        {
            return await DbSet
                .Include(p => p.Category)
                .Include(p => p.ProductStatus)
                .Include(p => p.Inventory)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Product>> SearchAsync(string query)
        {
            // Value objects no se traducen bien con ILike en SQL: filtramos en memoria.
            // Ademas tokenizamos para que "me gustan los lenovos" encuentre "lenovo LOQ".
            var tokens = Tokenize(query);
            if (tokens.Count == 0)
                return Array.Empty<Product>();

            var products = await DbSet
                .Include(p => p.Category)
                .Include(p => p.ProductStatus)
                .Include(p => p.Inventory)
                .ToListAsync();

            return products
                .Where(p => MatchesAnyToken(p, tokens))
                .ToList();
        }

        public async Task<IReadOnlyList<Product>> SearchByEmbeddingAsync(float[] embedding, int take = 15)
        {
            if (embedding is null || embedding.Length == 0)
                return Array.Empty<Product>();

            var vector = new Pgvector.Vector(embedding);

            return await DbSet
                .Include(p => p.Category)
                .Include(p => p.ProductStatus)
                .Include(p => p.Inventory)
                .Where(p => p.Embedding != null)
                .OrderBy(p => p.Embedding!.CosineDistance(vector))
                .Take(take)
                .ToListAsync();
        }

        public async Task<Product?> GetByIdWithInventoryAsync(int productId)
        {
            return await DbSet
                .Include(p => p.Category)
                .Include(p => p.ProductStatus)
                .Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.Id == productId);
        }

        private static List<string> Tokenize(string query)
        {
            var raw = Regex.Split(query.Trim().ToLowerInvariant(), @"[^a-z0-9áéíóúüñ]+")
                .Where(t => t.Length >= 2 && !StopWords.Contains(t))
                .ToList();

            var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in raw)
            {
                tokens.Add(t);
                // Plural simple en español/inglés: lenovos → lenovo, laptops → laptop
                if (t.Length > 3 && t.EndsWith('s') && !t.EndsWith("ss"))
                    tokens.Add(t[..^1]);
                if (t.Length > 4 && t.EndsWith("es"))
                    tokens.Add(t[..^2]);
            }

            return tokens.ToList();
        }

        private static bool MatchesAnyToken(Product product, IReadOnlyList<string> tokens)
        {
            var name = product.Name.Value;
            var description = product.Description?.Value ?? string.Empty;
            var category = product.Category?.Name.Value ?? string.Empty;
            var haystack = $"{name} {description} {category}";

            return tokens.Any(token =>
                haystack.Contains(token, StringComparison.OrdinalIgnoreCase));
        }
    }
}
