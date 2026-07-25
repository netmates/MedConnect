using AppointmentService.Application.Extensions;
using AppointmentService.Infrastructure.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Application
builder.Services.AddApplicationServices();

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
