using AppointmentService.Application.DTOs.Appointment;
using AppointmentService.Application.Interfaces.Services;
using Grpc.Core;
using MedConnect.Shared.Grpc;

namespace AppointmentService.API.Grpc;

public sealed class AppointmentGrpcService(IAppointmentApplicationService appointments) : AppointmentAccess.AppointmentAccessBase
{
    public override async Task<ValidateAppointmentAccessResponse> ValidateAppointmentAccess(
        ValidateAppointmentAccessRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.AppointmentId, out var appointmentId)
            || string.IsNullOrWhiteSpace(request.KeycloakId))
        {
            return new ValidateAppointmentAccessResponse
            {
                Allowed = false,
                DenialReason = DenialReason.Forbidden
            };
        }

        var result = await appointments.ValidateAccessAsync(appointmentId, request.KeycloakId, context.CancellationToken);

        if (!result.Allowed)
        {
            return new ValidateAppointmentAccessResponse
            {
                Allowed = false,
                DenialReason = result.DenialReason switch
                {
                    AppointmentAccessDenial.NotFound => DenialReason.NotFound,
                    AppointmentAccessDenial.Closed => DenialReason.Closed,
                    _ => DenialReason.Forbidden
                }
            };
        }

        return new ValidateAppointmentAccessResponse
        {
            Allowed = true,
            DenialReason = DenialReason.Unspecified,
            AppointmentId = result.AppointmentId.ToString(),
            PatientId = result.PatientId.ToString(),
            DoctorId = result.DoctorId.ToString(),
            PatientKeycloakId = result.PatientKeycloakId,
            DoctorKeycloakId = result.DoctorKeycloakId,
            PatientName = result.PatientName,
            DoctorName = result.DoctorName
        };
    }
}
