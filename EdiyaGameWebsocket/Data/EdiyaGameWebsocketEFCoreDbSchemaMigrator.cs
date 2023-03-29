using Microsoft.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;

namespace EdiyaGameWebsocket.Data;

public class EdiyaGameWebsocketEFCoreDbSchemaMigrator : ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EdiyaGameWebsocketEFCoreDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the EdiyaGameWebsocketDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<EdiyaGameWebsocketDbContext>()
            .Database
            .MigrateAsync();
    }
}
