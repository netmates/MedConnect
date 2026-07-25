using AppointmentService.Application.Validators;
using AppointmentService.Infrastructure.Extensions;
using Scalar.AspNetCore;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateSlotValidator>();

// Application сервисы
//builder.Services.AddScoped<AppointmentApplicationService>();
//builder.Services.AddScoped<DoctorApplicationService>();
//builder.Services.AddScoped<PatientApplicationService>();
//builder.Services.AddScoped<ScheduleApplicationService>();
//builder.Services.AddScoped<SpecializationService>();

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

//app.UseAuthentication();
//app.UseAuthorization();
app.MapControllers();

app.Run();
