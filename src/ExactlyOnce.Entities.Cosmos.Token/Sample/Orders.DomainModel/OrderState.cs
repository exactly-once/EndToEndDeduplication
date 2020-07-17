namespace Orders.DomainModel
{
    public enum OrderState
    {
        Created,
        Submitted,
        Billed,
        BillingFailed
    }
}