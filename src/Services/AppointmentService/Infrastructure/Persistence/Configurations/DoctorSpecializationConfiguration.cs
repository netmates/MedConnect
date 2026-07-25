using AppointmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointmentService.Infrastructure.Persistence.Configurations;

public class DoctorSpecializationConfiguration : IEntityTypeConfiguration<DoctorSpecialization>
{
    public void Configure(EntityTypeBuilder<DoctorSpecialization> builder)
    {
        builder.HasKey(ds => new { ds.DoctorId, ds.SpecializationId });

        // Запрет удаления врача
        builder.HasOne(ds => ds.Doctor)
            .WithMany(d => d.DoctorSpecializations)
            .HasForeignKey(ds => ds.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Нельзя удалить специализацию пока есть врачи с ней
        builder.HasOne(ds => ds.Specialization)
            .WithMany(s => s.DoctorSpecializations)
            .HasForeignKey(ds => ds.SpecializationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

