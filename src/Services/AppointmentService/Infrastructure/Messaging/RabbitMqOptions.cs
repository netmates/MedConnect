using System.ComponentModel.DataAnnotations;

namespace AppointmentService.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    [Required]
    public string Host { get; set; } = "localhost";
    [Range(1, 65535)]
    public int Port { get; set; } = 5672;
    [Required]
    public string Username { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
    [Required]
    public string ExchangeName { get; set; } = "medconnect.events";
    [Required]
    public string ClientProvidedName { get; set; } = "AppointmentService-publisher";
}
