using System.Text.Json.Serialization;

namespace AzureCustomRoleDelegator.Data;

public class SubscriptionList
{
    public List<Subscription> Value { get; set; }
}
public class Subscription
{
    public string TenantId { get; set; }
    public string SubscriptionId { get; set; }
    public string DisplayName { get; set; }
    public string State { get; set; }
}

public class ServicePrincipal
{
    public string Id { get; set; }
    public string AppId { get; set; }
    public string DisplayName { get; set; }
    public string ServicePrincipalType { get; set; }
}
public class ODataResponse<T>
{
    public List<T> Value { get; set; }
    [JsonPropertyName("@odata.nextLink")]
    public string NextLink { get; set; }
}
