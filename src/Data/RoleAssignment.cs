using System.Net;

namespace AzureCustomRoleDelegator.Data;

public class ResourceList
{
    public List<Resource> Value { get; set; }
}
public class Resource
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public ResourceIdentity Identity { get; set; }
    public bool IsUserAssignedManagedIdentity => Type.Equals("Microsoft.ManagedIdentity/userAssignedIdentities", StringComparison.OrdinalIgnoreCase);
    public ResourceProperties Properties { get; set; }
    public bool IsSelected { get; set; }
}
public class ResourceProperties
{
    public string NodeResourceGroup { get; set; }
}
public class UserAssignedIdentity
{
    public string Id { get; set; }
    public string Name { get; set; }
    public ResourceIdentity Properties { get; set; }
}
public class ResourceIdentity
{
    public string PrincipalId { get; set; }
    public string TenantId { get; set; }
    public string Type { get; set; }
}

public class RoleAssignmentRequestResponse
{
    public RoleAssignmentRequest Request { get; set; }
    public bool IsSuccessful { get; set; }
    public string Message { get; set; }
    public HttpResponseMessage Response { get; set; }
}
public class RoleAssignmentRequest
{
    public RoleAssignmentProperties Properties { get; set; }
    public ErrorDetails Error { get; set; }
}
public class ErrorDetails
{
    public string Code { get; set; }
    public string Message { get; set; }
}
public class RoleAssignmentProperties
{
    public string PrincipalId { get; set; }
    public string RoleDefinitionId { get; set; }
}
public enum PrincipalType
{
    User,
    ServicePrincipal,
    UserAssignedManagedIdentity,
    SystemAssignedManagedIdentity
}
