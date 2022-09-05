namespace AzureCustomRoleDelegator.Data;

public class ResourceGroupList
{
    public List<ResourceGroup> Value { get; set; }
}
public class ResourceGroup
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Location { get; set; }
    public bool IsNodeResourceGroup { get; set; }
    public string ParentResourceGroup { get; internal set; }
}
