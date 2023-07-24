using System.Reflection;
using Microsoft.Extensions.Options;

namespace Soda.Migrations;

public class SodaMigrationOptions:IOptions<SodaMigrationOptions>
{
    public required string Assembly { get; set; }
    internal Assembly EfAssembly => System.Reflection.Assembly.Load(Assembly);
    
    public SodaMigrationOptions Value => this;
}