using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soda.Migrations.Abstractions;
using Soda.Migrations.Services;

namespace Soda.Migrations;

public static class SodaMigrationExtensions
{
    public static void AddSodaMigration<TDbContext>(this IServiceCollection serviceCollection, Action<SodaMigrationOptions>? optionAction = null)
        where TDbContext : DbContext, ISodaMigrationDbContext
    {
        SodaMigrationOptions options = new SodaMigrationOptions()
        {
            Assembly = Assembly.GetExecutingAssembly().Location
        };
        optionAction?.Invoke(options);
        serviceCollection.Configure<SodaMigrationOptions>(opts =>
        {
            opts.Assembly = options.Assembly;
        });
        
        serviceCollection.AddSingleton<ISodaMigrationService<TDbContext>, SodaMigrationService<TDbContext>>();
    }

    public static void UseSodaMigration<TDbContext>(this IHost applicationBuilder)where TDbContext:DbContext, ISodaMigrationDbContext
    {
        var service = applicationBuilder.Services.GetRequiredService<ISodaMigrationService<TDbContext>>();
        service.StartupInitMigrationsAsync();
    } 
}