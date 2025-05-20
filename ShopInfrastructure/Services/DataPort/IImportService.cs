using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ShopInfrastructure.Services.DataPort
{
    public interface IImportService<TEntity> where TEntity : class
    {
        Task ImportFromStreamAsync(Stream stream, CancellationToken cancellationToken = default);
    }
}