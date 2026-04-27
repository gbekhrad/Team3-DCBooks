using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project498.WebApi.Controllers;
using Project498.WebApi.Data;
using Project498.WebApi.Models;

namespace Project498.WebApi.Tests.UnitTests;

public class CharactersControllerTests
{
    private ComicsDbContext CreateFreshDbContext()
    {
        var options = new DbContextOptionsBuilder<ComicsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ComicsDbContext(options);
    }
    
    private async Task<CharactersController> CreateControllerWithSeedData()
    {
        var context = CreateFreshDbContext();
        
        context.Characters.AddRange(
            new Character
            {
                Alias = "Batman",
                Name = "Bruce Wayne"
            },
            new Character
            {
                Alias = "Superman",
                Name = "Clark Kent"
            }
        );
        
        await  context.SaveChangesAsync();
        return new CharactersController(context);
    }
    
    [Fact]
    public async Task GetCharacters_ReturnsAllCharacters_InOrder()
    {
        var controller = await CreateControllerWithSeedData();

        var result = await controller.GetCharacters();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var characters = Assert.IsAssignableFrom<List<Character>>(okResult.Value);

        Assert.Equal(2, characters.Count);
        Assert.Equal(1, characters[0].CharacterId);
        Assert.Equal(2, characters[1].CharacterId);
    }
    
    [Fact]
    public async Task GetCharacter_ReturnsCharacter_WhenExists()
    {
        var controller = await CreateControllerWithSeedData();

        var result = await controller.GetCharacter(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var character = Assert.IsType<Character>(okResult.Value);

        Assert.Equal("Bruce Wayne", character.Name);
    }

    [Fact]
    public async Task GetCharacter_ReturnsNotFound_WhenMissing()
    {
        var controller = await CreateControllerWithSeedData();

        var result = await controller.GetCharacter(999);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(notFound.Value);

        Assert.Equal("CHARACTER_NOT_FOUND", error.Code);
    }
    
    [Fact]
    public async Task AddCharacter_ReturnsBadRequest_WhenNameMissing()
    {
        var controller = await CreateControllerWithSeedData();

        var request = new Character { Name = " " };

        var result = await controller.AddCharacter(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(badRequest.Value);

        Assert.Equal("VALIDATION_ERROR", error.Code);
    }

    [Fact]
    public async Task AddCharacter_CreatesCharacter_WhenValid()
    {
        var controller = await CreateControllerWithSeedData();

        var request = new Character
        {
            Name = " Diana Prince ",
            Alias = " Wonder Woman ",
            Description = " Amazonian "
        };

        var result = await controller.AddCharacter(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var character = Assert.IsType<Character>(created.Value);

        Assert.Equal("Diana Prince", character.Name); // trimmed
        Assert.Equal("Wonder Woman", character.Alias);
    }
    
    [Fact]
    public async Task EditCharacter_ReturnsNotFound_WhenMissing()
    {
        var controller = await CreateControllerWithSeedData();

        var result = await controller.EditCharacter(999, new Character { Name = "Test" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task EditCharacter_ReturnsBadRequest_WhenNameInvalid()
    {
        var controller = await CreateControllerWithSeedData();

        var result = await controller.EditCharacter(1, new Character { Name = " " });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task EditCharacter_UpdatesCharacter_WhenValid()
    {
        var controller = await CreateControllerWithSeedData();

        var result = await controller.EditCharacter(1, new Character
        {
            Name = "Batman Updated",
            Alias = "Dark Knight",
            Description = "Hero"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var character = Assert.IsType<Character>(ok.Value);

        Assert.Equal("Batman Updated", character.Name);
    }
    
    [Fact]
    public async Task DeleteCharacter_ReturnsNotFound_WhenMissing()
    {
        var controller = await CreateControllerWithSeedData();

        var result = await controller.DeleteCharacter(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteCharacter_RemovesCharacter_WhenExists()
    {
        var controller = await CreateControllerWithSeedData();

        var result = await controller.DeleteCharacter(1);

        Assert.IsType<NoContentResult>(result);

        // Verify it's actually gone
        var check = await controller.GetCharacter(1);
        Assert.IsType<NotFoundObjectResult>(check);
    }
}