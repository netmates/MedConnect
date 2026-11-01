using System.Text.Encodings.Web;
using System.Text.Json;
using AppointmentService.Application.DTOs.ScheduleSlot;
using AppointmentService.Application.Interfaces.Caching;
using Microsoft.Extensions.Caching.Distributed;

namespace AppointmentService.Infrastructure.Caching;

public sealed class DoctorAvailableSlotCache(IDistributedCache cache) : IDoctorAvailableSlotCache
{
    private static string Key(Guid doctorId, DateTime date) => $"doctor:{doctorId:D}:slots:{date:yyyy-MM-dd}";
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<IReadOnlyList<ScheduleSlotDto>?> GetAsync(Guid doctorId, DateTime date, CancellationToken ct)
    {
        var json = await cache.GetStringAsync(Key(doctorId, date), ct);
        if (json is null)
            return null;

        return JsonSerializer.Deserialize<List<ScheduleSlotDto>>(json, JsonOptions);
    }

    public Task SetAsync(Guid doctorId, DateTime date, IReadOnlyList<ScheduleSlotDto> slots, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(slots, JsonOptions);
        return cache.SetStringAsync(
            Key(doctorId, date),
            json,
            new DistributedCacheEntryOptions
            {
                SlidingExpiration = Lifetime
            },
            ct);
    }

    public Task RemoveAsync(Guid doctorId, DateTime date, CancellationToken ct) => cache.RemoveAsync(Key(doctorId, date), ct);
}
