using CommunicationService.Common.Exceptions;
using Grpc.Core;
using MedConnect.Shared.Grpc;

namespace CommunicationService.Common.Grpc;

public sealed class AppointmentAccessClient(
    AppointmentAccess.AppointmentAccessClient grpc,
    ILogger<AppointmentAccessClient> logger)
{
    public async Task<AppointmentAccessInfo> ValidateAsync(Guid appointmentId, string keycloakId, CancellationToken ct)
    {
        ValidateAppointmentAccessResponse response;
        try
        {
            response = await grpc.ValidateAppointmentAccessAsync(
                new ValidateAppointmentAccessRequest
                {
                    AppointmentId = appointmentId.ToString(),
                    KeycloakId = keycloakId
                },
                cancellationToken: ct);
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            logger.LogError(ex,
                "Appointment gRPC unavailable. AppointmentId={AppointmentId}, StatusCode={StatusCode}",
                appointmentId, ex.StatusCode);
            throw new ServiceUnavailableException("Сервис записей временно недоступен. Попробуйте позже.");
        }

        if (!response.Allowed)
        {
            throw response.DenialReason switch
            {
                DenialReason.NotFound => new NotFoundException("Запись не найдена."),
                DenialReason.Closed => new ForbiddenException("Чат недоступен: запись отменена или завершена."),
                _ => new ForbiddenException("Нет доступа к этой записи.")
            };
        }

        if (!Guid.TryParse(response.PatientId, out var patientGuid) ||
            !Guid.TryParse(response.DoctorId, out var doctorGuid))
        {
            throw new BusinessRuleException("Сервис записей вернул некорректные идентификаторы.");
        }

        return new AppointmentAccessInfo
        {
            AppointmentId = appointmentId,
            PatientId = patientGuid,
            DoctorId = doctorGuid,
            PatientKeycloakId = response.PatientKeycloakId,
            DoctorKeycloakId = response.DoctorKeycloakId,
            PatientName = response.PatientName,
            DoctorName = response.DoctorName
        };
    }
}
