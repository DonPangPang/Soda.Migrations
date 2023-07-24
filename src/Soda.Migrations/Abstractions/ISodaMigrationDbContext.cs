using Microsoft.EntityFrameworkCore;
using Soda.Migrations.Domain;

namespace Soda.Migrations.Abstractions;

public interface ISodaMigrationDbContext
{
    public DbSet<EFMigrationsHistory> EFMigrationsHistory { get; set; }
}