namespace HousingInfrastructure.Services;

public class ImportValidationException : Exception
{
    public IReadOnlyCollection<string> ValidationErrors { get; }

    public ImportValidationException(IEnumerable<string> validationErrors)
        : base("Помилки в даних імпорту.")
    {
        ValidationErrors = validationErrors.ToList().AsReadOnly();
    }
}