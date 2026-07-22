using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointmentService.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.PatientId)
            .IsRequired();

        builder.Property(a => a.DoctorId)
            .IsRequired();

        builder.Property(a => a.SlotId)
            .IsRequired();
        
        builder.Property(a => a.Reason)
            .HasMaxLength(Appointment.MaxReasonLength);

        builder.Property(a => a.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();

        // Частичный уникальный индекс по SlotId:
        // Один слот не может иметь две активные записи одновременно
        // Cancelled исключены из индекса — после отмены слот можно забронировать снова        
        builder.HasIndex(a => a.SlotId)
            .IsUnique()
            .HasFilter($"\"Status\" != {(int)AppointmentStatus.Cancelled}");

        // Индексы для ускорения GetByPatientIdAsync и GetByDoctorIdAsync
        builder.HasIndex(a => a.PatientId);
        builder.HasIndex(a => a.DoctorId);

        // Нельзя удалить пациента пока у него есть записи
        builder.HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Нельзя удалить врача пока у него есть записи
        builder.HasOne(a => a.Doctor)
            .WithMany()
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Нельзя удалить слот пока к нему привязана запись
        builder.HasOne(a => a.Slot)
            .WithMany()
            .HasForeignKey(a => a.SlotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
