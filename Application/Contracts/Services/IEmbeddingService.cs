namespace Application.Contracts.Services
{
    public interface IEmbeddingService
    {
        bool IsConfigured { get; }

        /// <summary>
        /// Genera un embedding (text-embedding-3-small, 1536 dims). Null si no hay API key o falla.
        /// </summary>
        Task<float[]?> CreateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    }
}
