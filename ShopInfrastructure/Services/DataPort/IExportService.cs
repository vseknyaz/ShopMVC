using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ShopInfrastructure.Services.DataPort
{
    public interface IExportService<TEntity> where TEntity : class
    {
        Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default);
    }
}