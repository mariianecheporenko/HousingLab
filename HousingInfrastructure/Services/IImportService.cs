using HousingDomain.Models;

namespace HousingInfrastructure.Services;

public interface IImportService<TEntity>
    where TEntity : Entity
{
    Task ImportFromStreamAsync(Stream stream, CancellationToken cancellationToken);
}
