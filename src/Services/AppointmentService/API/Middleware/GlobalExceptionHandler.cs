using AppointmentService.Application.Exceptions;
using AppointmentService.Domain.Exceptions;
using AppointmentService.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.API.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            BusinessRuleException => (StatusCodes.Status400BadRequest, "Business Rule Violation"),
            DomainException => (StatusCodes.Status400BadRequest, "Domain Error"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation Error"),
            DbUpdateException ex when PostgresExceptionHelper.IsUniqueViolation(ex) => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception");
        else
            logger.LogWarning(exception, "Handled exception: {Title}", title);

        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            await Results.ValidationProblem(
                errors,
                detail: "Одна или несколько ошибок валидации.",
                title: title,
                statusCode: status).ExecuteAsync(httpContext);

            return true;
        }

        var detail = status == StatusCodes.Status500InternalServerError
            ? "Произошла внутренняя ошибка."
            : exception is DbUpdateException dbEx && PostgresExceptionHelper.IsUniqueViolation(dbEx)
                ? "Операция конфликтует с текущим состоянием данных."
                : exception.Message;

        await Results.Problem(
            title: title,
            detail: detail,
            statusCode: status).ExecuteAsync(httpContext);

        return true;
    }
}
