namespace RLApp.Adapters.Http.Security;

public static class AuthorizationPolicies
{
    public const string AuthenticatedStaff = nameof(AuthenticatedStaff);
    public const string ReceptionOperations = nameof(ReceptionOperations);
    public const string CashierOperations = nameof(CashierOperations);
    public const string DoctorOperations = nameof(DoctorOperations);
    public const string SupervisorOnly = nameof(SupervisorOnly);
    public const string SupportOrSupervisor = nameof(SupportOrSupervisor);
}
