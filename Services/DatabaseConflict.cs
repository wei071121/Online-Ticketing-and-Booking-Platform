using Microsoft.Data.SqlClient;

namespace QigloRestaurant.Web.Services;

// SQL Server may wrap a deadlock in several EF Core exception layers. Keep the
// technical detail out of the UI and let the user safely retry the operation.
public static class DatabaseConflict
{
    public static bool IsDeadlock(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException { Number: 1205 })
            {
                return true;
            }
        }

        return false;
    }
}
