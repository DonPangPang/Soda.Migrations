using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Soda.Migrations.Domain;

[Table("__EFMigrationsHistory")]
public class EFMigrationsHistory
{
    [Key]
    [MaxLength(150)]
    public string MigrationId { get; set; } = null!;

    [MaxLength(32)]
    public string ProductVersion { get; set; } = null!;

    [NotMapped]
    public string Sort => MigrationId.Split("_")[0];
}