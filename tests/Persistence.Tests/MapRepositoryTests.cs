namespace Persistence.Tests;

using Microsoft.EntityFrameworkCore;

public class MapRepositoryTests : DatabaseTest
{
    [Fact]
    public async Task ExecuteCommand()
    {
        var result = await DbContext.Database.ExecuteSqlRawAsync("SELECT 1;");
        Assert.Equal(-1, result);
    }
}
