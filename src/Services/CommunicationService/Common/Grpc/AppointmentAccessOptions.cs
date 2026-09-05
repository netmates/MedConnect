namespace CommunicationService.Common.Grpc;

public sealed class AppointmentAccessOptions
{
    public const string SectionName = "AppointmentGrpc";

    public string Address { get; set; } = string.Empty;
}
