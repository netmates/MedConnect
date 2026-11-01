using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Interfaces.Services;

public interface IActiveAppointmentCancellation
{
    /// <summary>
    /// Отменяет переданные записи внутри уже открытой транзакции.
    /// Слот Booked: будущий освобождается (Free), начавшийся или прошедший потребляется (Consume).
    /// В FreedSlots попадают только слоты, которые стали Available.
    /// </summary>
    public Task<AppointmentCancellationResult> CancelAsync(
        IReadOnlyList<Appointment> appointments,
        CancellationToken ct);
}

public sealed class AppointmentCancellationResult
{
    public int CancelledCount { get; init; }
    public IReadOnlyList<FreedDoctorSlot> FreedSlots { get; init; } = [];
}

public sealed class FreedDoctorSlot
{
    public required Guid DoctorId { get; init; }
    public required DateTime StartTime { get; init; }
}
