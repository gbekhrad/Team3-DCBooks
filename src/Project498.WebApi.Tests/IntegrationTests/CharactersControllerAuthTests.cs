using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project498.WebApi.Data;

namespace Project498.WebApi.Tests.IntegrationTests;

public class CharactersControllerAuthTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CharactersControllerAuthTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Remove ALL existing DB configs
                var descriptors = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<ComicsDbContext>))
                    .ToList();

                foreach (var d in descriptors)
                {
                    services.Remove(d);
                }

                // Add InMemory DB (unique per test run)
                services.AddDbContext<ComicsDbContext>(options =>
                {
                    options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                });
            });
        });
    }
    
    [Fact]
    public async Task GetCharacters_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/characters");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}