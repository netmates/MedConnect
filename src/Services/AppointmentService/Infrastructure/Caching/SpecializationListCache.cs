using System.Text.Encodings.Web;
using System.Text.Json;
using AppointmentService.Application.DTOs.Specialization;
using AppointmentService.Application.Interfaces.Caching;
using Microsoft.Extensions.Caching.Distributed;

namespace AppointmentService.Infrastructure.Caching;

public sealed class SpecializationListCache(IDistributedCache cache) : ISpecializationListCache
{
    private const string Key = "specializations:all";
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<IReadOnlyList<SpecializationDto>?> GetAsync(CancellationToken ct)
    {
        var json = await cache.GetStringAsync(Key, ct);
        if (json is null)
            return null;

        return JsonSerializer.Deserialize<List<SpecializationDto>>(json, JsonOptions);
    }

    public Task SetAsync(IReadOnlyList<SpecializationDto> items, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(items, JsonOptions);
        return cache.SetStringAsync(
            Key,
            json,
            new DistributedCacheEntryOptions
            {
                SlidingExpiration = Lifetime
            },
            ct);
    }

    public Task RemoveAsync(CancellationToken ct) => cache.RemoveAsync(Key, ct);
}
