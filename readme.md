# Soda.Migration

使用`AddSodaMigration`可以使用`ISodaMigrationService<TDbContext>`服务, 用来操作迁移.

```csharp
builder.Service.AddSodaMigration<TDbContext>();
```

`UseSodaMigration`为自动迁移, 如果不需要, 可以不写该代码.

```csharp
builder.Service.UseSodaMigration<TDbContext>();
```
