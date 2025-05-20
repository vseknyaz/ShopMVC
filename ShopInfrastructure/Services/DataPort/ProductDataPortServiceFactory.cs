// File: ShopInfrastructure/Services/DataPort/ProductDataPortServiceFactory.cs
using System;
using ShopDomain.Model;

namespace ShopInfrastructure.Services.DataPort
{
    public class ProductDataPortServiceFactory : IDataPortServiceFactory<Product>
    {
        private readonly DbsportsContext _ctx;
        public ProductDataPortServiceFactory(DbsportsContext ctx) => _ctx = ctx;

        public IImportService<Product> GetImportService(string contentType)
            => contentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
               ? new ProductImportService(_ctx)
               : throw new NotSupportedException(contentType);

        public IExportService<Product> GetExportService(string contentType)
            => contentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
               ? new ProductExportService(_ctx)
               : throw new NotSupportedException(contentType);
    }
}
