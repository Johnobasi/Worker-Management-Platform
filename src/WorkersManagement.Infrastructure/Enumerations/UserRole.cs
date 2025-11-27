using System.Text.Json.Serialization;

namespace WorkersManagement.Infrastructure.Enumerations
{
    public enum UserRole
    {
        SuperAdmin,
        Admin,
        SubTeamLead,
        HOD,
        Worker
    }

    public enum WorkerType
    {
        SmallGroupLeader,
        ZonalCoordinator,
        Worker,
        HOD,
        SubTeamLead,
        TeamPastor,
        CampusPastor
    }
}
