namespace AgroControl.Domain.Modules.Subscriptions;

public sealed class Subscription
{
    private Subscription() { }

    private Subscription(
        Guid id,
        Guid organizationId,
        PlanCode plan,
        SubscriptionStatus status,
        DateTime startedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Plan = plan;
        Status = status;
        StartedAtUtc = startedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public PlanCode Plan { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? EndsAtUtc { get; private set; }

    public static Subscription CreateBasic(Guid organizationId, DateTime startedAtUtc)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization id is required.", nameof(organizationId));

        return new Subscription(
            Guid.NewGuid(),
            organizationId,
            PlanCode.Basic,
            SubscriptionStatus.Active,
            startedAtUtc);
    }

    public bool IsActive(DateTime nowUtc) =>
        Status == SubscriptionStatus.Active &&
        (EndsAtUtc is null || EndsAtUtc > nowUtc);
}
