namespace AzureCustomRoleDelegator.Data;

public class RoleDefinitionList
{
    public List<RoleDefinition> Value { get; set; }
}
public class RoleDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public RoleDefinitionProperties Properties { get; set; }
    public bool ViewExpanded { get; set; }
    public bool IsSelected { get; set; }
}
public class RoleDefinitionProperties
{
    public string RoleName { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public string[] AssignableScopes { get; set; }
    public PermissionSet[] Permissions { get; set; }
}
public class PermissionSet
{
    public string[] Actions { get; set; }
    public string[] NoActions { get; set; }
}
