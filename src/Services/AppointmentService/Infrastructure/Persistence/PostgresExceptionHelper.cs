using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AppointmentService.Infrastructure.Persistence;

public static class PostgresExceptionHelper
{
    /// <summary>Postgres unique_violation (SQLSTATE 23505).</summary>
    public static bool IsUniqueViolation(Exception exception)
    {
        for (var ex = exception; ex is not null; ex = ex.InnerException)
        {
            if (ex is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                return true;
        }

        return false;
    }

    public static bool IsUniqueViolation(DbUpdateException exception)
        => IsUniqueViolation((Exception)exception);
}
