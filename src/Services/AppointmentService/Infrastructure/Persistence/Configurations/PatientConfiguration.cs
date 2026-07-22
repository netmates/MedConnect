using AppointmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointmentService.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.KeycloakId)
            .IsRequired()
            .HasMaxLength(Patient.MaxKeycloakIdLength);

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(Patient.MaxLastNameLength);

        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(Patient.MaxFirstNameLength);

        builder.Property(p => p.MiddleName)
            .HasMaxLength(Patient.MaxMiddleNameLength);
        
        builder.Property(p => p.Phone)
            .HasMaxLength(Patient.MaxPhoneLength);

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        // Ускоряет GetByKeycloakIdAsync
        builder.HasIndex(p => p.KeycloakId)
            .IsUnique();
        
        // Нельзя удалить пациента пока у него есть записи
        builder.HasMany(p => p.Appointments)
            .WithOne(a => a.Patient)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
