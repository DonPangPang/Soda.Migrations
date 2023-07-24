using Microsoft.EntityFrameworkCore;
using Soda.Migrations.Abstractions;
using Soda.Migrations.Domain;

namespace Soda.Migrations.Services;

public interface ISodaMigrationService<TDbContext> where TDbContext:DbContext, ISodaMigrationDbContext
{
    /// <summary>
    /// 获取EF版本
    /// </summary>
    /// <returns></returns>
    string GetEfVersion();
    /// <summary>
    /// 获取本地所有版本
    /// </summary>
    /// <returns></returns>
    IEnumerable<EFMigrationsHistory> GetLocalMigrations();
    /// <summary>
    /// 获取本地所有版本
    /// </summary>
    /// <returns></returns>
    IEnumerable<Type> GetLocalMigrationTypes();
    /// <summary>
    /// 将部署前所有的迁移都加入迁移记录表中
    /// </summary>
    Task DiscardNoPendingMigrationsHistoryAsync();
    /// <summary>
    /// 获取最后一个迁移版本
    /// </summary>
    /// <returns></returns>
    Task<EFMigrationsHistory?> GetLastVersion();
    /// <summary>
    /// 获取未应用的迁移
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<Type>> GetPendingMigrationsAsync();
    /// <summary>
    /// 获取已应用的迁移
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<string>> GetAppliedMigrationsAsync();
    /// <summary>
    /// 应用迁移
    /// </summary>
    /// <param name="migrations"></param>
    Task ApplyMigrationsAsync(IEnumerable<Type> migrations);
    
    /// <summary>
    /// 应用迁移
    /// </summary>
    /// <param name="migrations"></param>
    Task ApplyMigrationsAsync(Type migration);
    /// <summary>
    /// 开始迁移入口
    /// </summary>
    /// <returns></returns>
    Task StartupInitMigrationsAsync();
}