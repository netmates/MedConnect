using Npgsql;

namespace AppointmentService.Infrastructure.Persistence;

public static class PostgresExceptionHelper
{
    /// <summary>Является ли исключение нарушением unique-ограничения PostgreSQL.</summary>
    public static bool IsUniqueViolation(Exception exception)
    {
        for (var ex = exception; ex is not null; ex = ex.InnerException)
        {
            if (ex is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                return true;
        }

        return false;
    }
}
