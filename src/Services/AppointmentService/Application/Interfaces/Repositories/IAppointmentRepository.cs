using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Interfaces.Repositories;

public interface IAppointmentRepository : IRepository<Appointment>
{
    // Получить все записи конкретного пациента
    Task<IReadOnlyList<Appointment>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default);
    // Получить все записи к конкретному врачу
    Task<IReadOnlyList<Appointment>> GetByDoctorIdAsync(Guid doctorId, CancellationToken ct = default);
    // Найти запись, привязанную к конкретному слоту; нужно для проверки, занят ли слот
    Task<Appointment?> GetBySlotIdAsync(Guid slotId, CancellationToken ct = default);
}
