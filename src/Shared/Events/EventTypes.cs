namespace MedConnect.Shared.Events;

public static class EventTypes
{
    public const string AppointmentCreated = "AppointmentCreated";
    public const string ParticipantNameUpdated = "ParticipantNameUpdated";
}

public static class RoutingKeys
{
    public const string AppointmentCreated = "appointments.appointment.created.v1";
    public const string ParticipantNameUpdated = "participants.participant.name-updated.v1";
}

public static class ParticipantRoles
{
    public const string Patient = "Patient";
    public const string Doctor = "Doctor";
}
