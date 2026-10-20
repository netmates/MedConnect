using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Interfaces.Services;

public interface IActiveAppointmentCancellation
{
    /// <summary>
    /// Отменяет переданные записи внутри уже открытой транзакции.
    /// Слот Booked: будущий освобождается (Free), начавшийся или прошедший потребляется (Consume).
    /// </summary>
    public Task<int> CancelAsync(IReadOnlyList<Appointment> appointments, CancellationToken ct);
}
