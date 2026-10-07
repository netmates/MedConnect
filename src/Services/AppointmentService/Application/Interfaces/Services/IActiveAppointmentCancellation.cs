using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Interfaces.Services;

public interface IActiveAppointmentCancellation
{
    /// <summary>
    /// Отменяет переданные записи внутри уже открытой транзакции.
    /// Слот Booked: будущий освобождается, начавшийся или прошедший потребляется.
    /// </summary>
    public Task<int> CancelAsync(IReadOnlyList<Appointment> appointments, CancellationToken ct);
}
