namespace Soda.Migrations.Exceptions;

public class MigrationException:Exception
{
    public MigrationException(string message):base(message)
    {
        
    }
}