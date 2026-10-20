using AppointmentService.Application.Interfaces;
using MedConnect.Shared.Http;

namespace AppointmentService.Infrastructure.Http;

public sealed class HttpCorrelationIdAccessor(IHttpContextAccessor httpContextAccessor) : ICorrelationIdAccessor
{
    public string? Get()
        => httpContextAccessor.HttpContext?.Items[CorrelationIdKeys.ItemKey] as string;
}
