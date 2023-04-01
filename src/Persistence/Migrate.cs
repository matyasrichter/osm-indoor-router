namespace Persistence;

using Microsoft.EntityFrameworkCore;

public static class Migrate
{
    public static void MigrateUp(RoutingDbContext context)
    {
        if (context.Database.GetPendingMigrations().Any())
            context.Database.Migrate();
    }
}
