namespace Application.Interfaces;

public interface ISeederService
{
    Task SeedFeaturesAsync();
    Task SeedAdminAsync();
    Task SeedTestDataAsync(bool clear = false);
}