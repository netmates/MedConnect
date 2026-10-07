using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;

namespace AppointmentService.Application.Services;

public sealed class ActiveAppointmentCancellation(
    IAppointmentRepository appointmentRepository,
    IScheduleSlotRepository slotRepository) : IActiveAppointmentCancellation
{
    public async Task<int> CancelAsync(IReadOnlyList<Appointment> appointments, CancellationToken ct)
    {
        var cancelled = 0;
        var now = DateTime.UtcNow;

        foreach (var item in appointments)
        {
            var appointment = await appointmentRepository.GetByIdWithLockAsync(item.Id, ct);
            if (appointment is null)
                continue;

            if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
                continue;

            var slot = await slotRepository.GetByIdWithLockAsync(appointment.SlotId, ct)
                ?? throw new NotFoundException("Слот записи не найден.");

            appointment.Cancel();
            await appointmentRepository.UpdateAsync(appointment, ct);
            cancelled++;

            if (slot.Status == SlotStatus.Booked)
            {
                slot.ReleaseAfterCancellation(now);
                await slotRepository.UpdateAsync(slot, ct);
            }
        }

        return cancelled;
    }
}
