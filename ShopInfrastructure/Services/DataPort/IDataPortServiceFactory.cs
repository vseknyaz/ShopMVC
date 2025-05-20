namespace ShopInfrastructure.Services.DataPort
{
    public interface IDataPortServiceFactory<TEntity> where TEntity : class
    {
        IImportService<TEntity> GetImportService(string contentType);
        IExportService<TEntity> GetExportService(string contentType);
    }
}