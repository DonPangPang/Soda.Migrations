using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soda.Migrations.Abstractions;
using Soda.Migrations.Attributes;
using Soda.Migrations.Domain;
using Soda.Migrations.Exceptions;
using Soda.Migrations.Helpers;

namespace Soda.Migrations.Services;

public class SodaMigrationService<TDbContext> :ISodaMigrationService<TDbContext> where TDbContext:DbContext, ISodaMigrationDbContext
{
    private readonly TDbContext _dbContext;
    private readonly ILogger<ISodaMigrationService<TDbContext>> _logger;
    private readonly Assembly _efAssembly;

    public SodaMigrationService(TDbContext dbContext
        , IOptions<SodaMigrationOptions> options
        , ILogger<ISodaMigrationService<TDbContext>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _efAssembly = options.Value.EfAssembly;
    }
    
    private IMigrator GetMigrator()
    {
        return _dbContext.GetService<IMigrator>();
    }

    public string GetEfVersion()
    {
        return Microsoft.EntityFrameworkCore.Infrastructure.ProductInfo.GetVersion();
    }
    
    /// <summary>
    /// 获取本地所有版本
    /// </summary>
    /// <returns></returns>
    public IEnumerable<EFMigrationsHistory> GetLocalMigrations()
    {
        var migrations = _efAssembly.GetTypes().Where(x =>
                x.Namespace != null &&
                x.GetCustomAttributes<MigrationAttribute>().Any() && x.GetCustomAttribute<DbContextAttribute>()?.ContextType.BaseType == typeof(TDbContext))
            .Select(x =>
                new EFMigrationsHistory()
                {
                    MigrationId = x.GetCustomAttribute<MigrationAttribute>()!.Id,
                    ProductVersion = GetEfVersion(),
                })
            .AsQueryable();

        return migrations;
    }
    
    /// <summary>
    /// 获取本地所有版本
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Type> GetLocalMigrationTypes()
    {
        var migrations = _efAssembly.GetTypes().Where(x =>
                x.Namespace != null &&
                x.GetCustomAttributes<MigrationAttribute>().Any() && x.GetCustomAttribute<DbContextAttribute>()?.ContextType.BaseType == typeof(TDbContext))
            .AsQueryable();

        return migrations;
    }

    /// <summary>
    /// 将部署前所有的迁移都加入迁移记录表中
    /// </summary>
    public async Task DiscardNoPendingMigrationsHistoryAsync()
    {
        var lastVersion = await GetLastVersion();

        var applied = await GetAppliedMigrationsAsync();

        var migrations = lastVersion is null
            ? GetLocalMigrations().ToList()
            : GetLocalMigrations().Where(x => String.CompareOrdinal(x.Sort, lastVersion.Sort) <= 0 && (applied != null && !applied.Contains(x.MigrationId))).ToList();

        if (migrations.Any())
        {
            _dbContext.AddRange(migrations);

            await _dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 获取最后一个迁移版本
    /// </summary>
    /// <returns></returns>
    public async Task<EFMigrationsHistory?> GetLastVersion()
    {
        var applied = await _dbContext.EFMigrationsHistory.ToListAsync();

        var thisContextLocal = GetLocalMigrations().Select(x => x.MigrationId);

        var thisContextVersions = applied.Where(x => thisContextLocal.Contains(x.MigrationId));

        return thisContextVersions.MaxBy(x => x.Sort);
    }

    /// <summary>
    /// 获取未应用的迁移
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<Type>> GetPendingMigrationsAsync()
    {
        var lastVersion = await GetLastVersion();

        return lastVersion is null
            ? GetLocalMigrationTypes()
            : GetLocalMigrationTypes()
                .Where(x => String.CompareOrdinal(x.GetCustomAttribute<MigrationAttribute>()!.Id,
                                lastVersion.MigrationId) >
                            0)
                .OrderBy(x => x.GetCustomAttribute<MigrationAttribute>()!.Id);
    }

    /// <summary>
    /// 获取已应用的迁移
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<string>> GetAppliedMigrationsAsync()
    {
        return await _dbContext.Database.GetAppliedMigrationsAsync();
    }

    /// <summary>
    /// 应用迁移
    /// </summary>
    /// <param name="migrations"></param>
    public async Task ApplyMigrationsAsync(IEnumerable<Type> migrations)
    {
        if (!migrations.Any())
        {
            return;
        }

        var migrator = GetMigrator();
        
        foreach (var migration in migrations)
        {
            var id = migration.GetCustomAttribute<MigrationAttribute>()!.Id;
            if (migration.GetCustomAttributes<TryMigrationAttribute>().Any())
            {
                try
                {
                    await migrator.MigrateAsync(id);
                }
                catch (Exception ex)
                {
                    var skip = new EFMigrationsHistory
                    {
                        MigrationId = id,
                        ProductVersion = GetEfVersion()
                    };

                    _dbContext.AddRange(skip);
                    await _dbContext.SaveChangesAsync();
                    
                    var errMsg = @$"
                    迁移 [{id}] 已跳过!\n
                    迁移失败!\n
                    提示信息为: {ex.Message}\n
                    请检查 Migrations 文件! 或者手动更改 __MigrationsHistory 表, 将该迁移添加入表中标记为已应用即可.";
#if DEBUG
                    Trace.WriteLine(errMsg);
#endif
                    ConsoleHelper.WriteWarning(errMsg);
                }
            }
            else if (migration.GetCustomAttributes<SkipMigrationAttribute>().Any())
            {
                var skip = new EFMigrationsHistory
                {
                    MigrationId = id,
                    ProductVersion = GetEfVersion()
                };
                
                _dbContext.AddRange(skip);
                await _dbContext.SaveChangesAsync();
                
                var errMsg = @$"
                    迁移 [{id}] 已跳过!\n
                    或者手动更改 __MigrationsHistory 表, 将该迁移添加入表中标记为已应用即可.";
#if DEBUG
                Trace.WriteLine(errMsg);   
#endif
                ConsoleHelper.WriteWarning(errMsg);
            }
            else
            {
                try
                {
                    await migrator.MigrateAsync(id);
                }
                catch (Exception ex)
                {
                    var errMsg = @$"
                    迁移 [{id}] 失败!\n
                    迁移失败!\n
                    提示信息为: {ex.Message}\n
                    请检查 Migrations 文件! 或者手动更改 __MigrationsHistory 表, 将该迁移添加入表中标记为已应用即可.
                    ";
                    
                    ConsoleHelper.WriteError(errMsg);

                    throw new MigrationException(errMsg);
                }
            }

#if DEBUG
            Trace.WriteLine($"迁移 [{id}] 已应用.");   
#endif
            ConsoleHelper.WriteInfo($"迁移 [{id}] 已应用.");
        }
    }

    public async Task ApplyMigrationsAsync(Type migration)
    {
        var migrator = GetMigrator();
        
        var id = migration.GetCustomAttribute<MigrationAttribute>()!.Id;
            if (migration.GetCustomAttributes<TryMigrationAttribute>().Any())
            {
                try
                {
                    await migrator.MigrateAsync(id);
                }
                catch (Exception ex)
                {
                    var skip = new EFMigrationsHistory
                    {
                        MigrationId = id,
                        ProductVersion = GetEfVersion()
                    };

                    _dbContext.AddRange(skip);
                    await _dbContext.SaveChangesAsync();
                    
                    var errMsg = @$"
                    迁移 [{id}] 已跳过!\n
                    迁移失败!\n
                    提示信息为: {ex.Message}\n
                    请检查 Migrations 文件! 或者手动更改 __MigrationsHistory 表, 将该迁移添加入表中标记为已应用即可.";
#if DEBUG
                    Trace.WriteLine(errMsg);
#endif
                    ConsoleHelper.WriteWarning(errMsg);
                }
            }
            else if (migration.GetCustomAttributes<SkipMigrationAttribute>().Any())
            {
                var skip = new EFMigrationsHistory
                {
                    MigrationId = id,
                    ProductVersion = GetEfVersion()
                };
                
                _dbContext.AddRange(skip);
                await _dbContext.SaveChangesAsync();
                
                var errMsg = @$"
                    迁移 [{id}] 已跳过!\n
                    或者手动更改 __MigrationsHistory 表, 将该迁移添加入表中标记为已应用即可.";
#if DEBUG
                Trace.WriteLine(errMsg);   
#endif
                ConsoleHelper.WriteWarning(errMsg);
            }
            else
            {
                try
                {
                    await migrator.MigrateAsync(id);
                }
                catch (Exception ex)
                {
                    var errMsg = @$"
                    迁移 [{id}] 失败!\n
                    迁移失败!\n
                    提示信息为: {ex.Message}\n
                    请检查 Migrations 文件! 或者手动更改 __MigrationsHistory 表, 将该迁移添加入表中标记为已应用即可.
                    ";
                    
                    ConsoleHelper.WriteError(errMsg);

                    throw new MigrationException(errMsg);
                }
            }

#if DEBUG
            Trace.WriteLine($"迁移 [{id}] 已应用.");   
#endif
            ConsoleHelper.WriteInfo($"迁移 [{id}] 已应用.");
    }

    /// <summary>
    /// 开始迁移入口
    /// </summary>
    /// <returns></returns>
    public async Task StartupInitMigrationsAsync()
    {
        ConsoleHelper.WriteInfo($"开始迁移...");
        var migrationsHistory = await GetAppliedMigrationsAsync();

        if (migrationsHistory.Any())
        {
            var pendingMigrations = await GetPendingMigrationsAsync();

            await ApplyMigrationsAsync(pendingMigrations);
        }

        await DiscardNoPendingMigrationsHistoryAsync();
        ConsoleHelper.WriteInfo($"迁移完成...");
    }
}