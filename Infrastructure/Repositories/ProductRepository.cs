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
            // Ranking + precision ladder para que "lenovo legion 5" no devuelva otros Lenovos.
            var tokens = Tokenize(query);
            if (tokens.Count == 0)
                return Array.Empty<Product>();

            var products = await DbSet
                .Include(p => p.Category)
                .Include(p => p.ProductStatus)
                .Include(p => p.Inventory)
                .ToListAsync();

            var scored = products
                .Select(p => (Product: p, Score: ScoreMatch(p, tokens, query)))
                .Where(x => x.Score > 0)
                .ToList();

            // 1) Todos los tokens en el nombre → solo esos
            var nameHits = scored
                .Where(x => MatchesAllTokensIn(x.Product.Name.Value, tokens))
                .OrderByDescending(x => x.Score)
                .Select(x => x.Product)
                .ToList();

            if (nameHits.Count > 0)
                return nameHits;

            // 2) Query multi-token: todos en name+description+category
            if (tokens.Count >= 2)
            {
                var broadHits = scored
                    .Where(x =>
                    {
                        var name = x.Product.Name.Value;
                        var description = x.Product.Description?.Value ?? string.Empty;
                        var category = x.Product.Category?.Name.Value ?? string.Empty;
                        return MatchesAllTokensIn($"{name} {description} {category}", tokens);
                    })
                    .OrderByDescending(x => x.Score)
                    .Select(x => x.Product)
                    .ToList();

                if (broadHits.Count > 0)
                    return broadHits;
            }

            // 3) Cualquier token, ordenados por score, max 8
            return scored
                .OrderByDescending(x => x.Score)
                .Take(8)
                .Select(x => x.Product)
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
                .Where(KeepToken)
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

        private static bool KeepToken(string t)
        {
            if (string.IsNullOrEmpty(t) || StopWords.Contains(t))
                return false;

            // Length >= 2, pure digits (e.g. "5"), or short model codes (e.g. "m2")
            if (t.Length >= 2)
                return true;
            if (Regex.IsMatch(t, @"^\d+$"))
                return true;
            if (Regex.IsMatch(t, @"^[a-záéíóúüñ]+\d+$", RegexOptions.IgnoreCase))
                return true;

            return false;
        }

        private static bool MatchesAllTokensIn(string haystack, IReadOnlyList<string> tokens)
        {
            if (string.IsNullOrEmpty(haystack) || tokens.Count == 0)
                return false;

            return tokens.All(token =>
                haystack.Contains(token, StringComparison.OrdinalIgnoreCase));
        }

        private static int ScoreMatch(Product product, IReadOnlyList<string> tokens, string query)
        {
            var name = product.Name.Value;
            var description = product.Description?.Value ?? string.Empty;
            var category = product.Category?.Name.Value ?? string.Empty;
            var descAndCategory = $"{description} {category}";

            var score = 0;
            var normalizedName = CollapseSpaces(name.ToLowerInvariant());
            var normalizedQuery = CollapseSpaces(query.Trim().ToLowerInvariant());

            if (!string.IsNullOrEmpty(normalizedQuery) &&
                normalizedName.Contains(normalizedQuery, StringComparison.Ordinal))
            {
                score += 50;
            }

            foreach (var token in tokens)
            {
                if (name.Contains(token, StringComparison.OrdinalIgnoreCase))
                    score += 20;
                else if (descAndCategory.Contains(token, StringComparison.OrdinalIgnoreCase))
                    score += 5;
            }

            if (MatchesAllTokensIn(name, tokens))
                score += 10;

            return score;
        }

        private static string CollapseSpaces(string value) =>
            Regex.Replace(value, @"\s+", " ").Trim();
    }
}