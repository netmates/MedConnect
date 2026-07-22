using AppointmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointmentService.Infrastructure.Persistence.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.KeycloakId)
            .IsRequired()
            .HasMaxLength(Doctor.MaxKeycloakIdLength);

        builder.Property(d => d.LastName)
            .IsRequired()
            .HasMaxLength(Doctor.MaxLastNameLength);

        builder.Property(d => d.FirstName)
            .IsRequired()
            .HasMaxLength(Doctor.MaxFirstNameLength);

        builder.Property(d => d.MiddleName)
            .HasMaxLength(Doctor.MaxMiddleNameLength);

        builder.Property(d => d.Description)
            .HasMaxLength(Doctor.MaxDescriptionLength);

        builder.Property(d => d.ExperienceYears)
            .IsRequired();

        builder.Property(d => d.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        // Ускоряет GetByKeycloakIdAsync
        builder.HasIndex(d => d.KeycloakId)
            .IsUnique();

        // Нельзя удалить врача пока у него есть слоты
        builder.HasMany(d => d.ScheduleSlots)
            .WithOne(s => s.Doctor)
            .HasForeignKey(s => s.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
