using System.Text.Encodings.Web;
using System.Text.Json;
using AppointmentService.Application.DTOs.Doctor;
using AppointmentService.Application.Interfaces.Caching;
using Microsoft.Extensions.Caching.Distributed;

namespace AppointmentService.Infrastructure.Caching;

public sealed class DoctorCardCache(IDistributedCache cache) : IDoctorCardCache
{
    private static string Key(Guid doctorId) => $"doctors:{doctorId:D}";
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<DoctorDto?> GetAsync(Guid doctorId, CancellationToken ct)
    {
        var json = await cache.GetStringAsync(Key(doctorId), ct);
        if (json is null)
            return null;

        return JsonSerializer.Deserialize<DoctorDto>(json, JsonOptions);
    }

    public Task SetAsync(DoctorDto doctor, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(doctor, JsonOptions);
        return cache.SetStringAsync(
            Key(doctor.Id),
            json,
            new DistributedCacheEntryOptions
            {
                SlidingExpiration = Lifetime
            },
            ct);
    }

    public Task RemoveAsync(Guid doctorId, CancellationToken ct) => cache.RemoveAsync(Key(doctorId), ct);
}
