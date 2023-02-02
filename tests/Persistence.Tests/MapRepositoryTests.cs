namespace Persistence.Tests;

using Microsoft.EntityFrameworkCore;

[Collection("DB")]
[Trait("Category", "DB")]
public sealed class MapRepositoryTests
{
    private readonly DbContext dbContext;

    public MapRepositoryTests(DatabaseFixture dbFixture) => dbContext = dbFixture.DbContext;

    [Fact]
    public async Task ExecuteCommand()
    {
        var result = await dbContext.Database.ExecuteSqlRawAsync("SELECT 1;");
        Assert.Equal(-1, result);
    }
}
