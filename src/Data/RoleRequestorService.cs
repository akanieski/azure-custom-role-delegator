using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace AzureCustomRoleDelegator.Data;

public class RoleRequestorServiceSettings
{
    public string[] BlockedRoles { get; set; } = new string[] { };
}
public class RoleRequestorService
{
    private IDownstreamWebApi _api;
    private RoleRequestorServiceSettings _settings;

    public RoleRequestorService(IDownstreamWebApi api, RoleRequestorServiceSettings settings)
    {
        _api = api;
        _settings = settings;
    }

    public async Task<IEnumerable<Subscription>> GetSubscriptions()
    {
        return (await _api.CallWebApiForUserAsync<SubscriptionList>("ResourceManagement",
            options => options.RelativePath = "/subscriptions?api-version=2020-01-01")).Value.OrderBy(s => s.DisplayName);
    }

    // Find what scope (what resource) you want to assign roles for
    public async Task<IEnumerable<Resource>> SearchResources(string subscriptionId, string searchTerm)
    {
        // Get resources and resource groups the USER has access to that match the search term
        // <- Add that to the resultset
        // Then iterate over those resources looking for AKS clusters ..
        // for each we will fetch the NRG and scan for resources as the APP that match the search term
        List<Resource> resources = new List<Resource>();

        var initialResp = await _api.CallWebApiForUserAsync<ResourceList>("ResourceManagement",
            options => options.RelativePath = $"/subscriptions/{subscriptionId}/resources?$filter=substringof(name, '{searchTerm.TrimStart().TrimEnd()}')&api-version=2021-04-01");

        resources.AddRange(initialResp.Value);

        foreach (var resource in resources.ToArray())
        {
            if (resource.Type.Equals("Microsoft.ContainerService/ManagedClusters", StringComparison.OrdinalIgnoreCase))
            {
                var clusterDetails = await _api.CallWebApiForUserAsync<Resource>("ResourceManagement", options => {
                    options.RelativePath = $"{resource.Id}?api-version=2022-01-01";
                    options.Scopes = "https://management.azure.com/.default";
                });
                var nrgDetails = await (await _api.CallWebApiForAppAsync("ResourceManagement",
                    options => options.RelativePath = $"/subscriptions/{subscriptionId}/resourcegroups/{clusterDetails.Properties.NodeResourceGroup}?api-version=2021-04-01")).Content.ReadFromJsonAsync<Resource>();
                //var nrgResources = (await (await _api.CallWebApiForAppAsync("ResourceManagement",
                //    options => options.RelativePath = $"/subscriptions/{subscriptionId}/resourceGroups/{clusterDetails.Properties.NodeResourceGroup}/resources?$filter=name eq '{searchTerm}'&api-version=2021-04-01")).Content.ReadFromJsonAsync<ResourceList>()).Value;
                
                resources.Add(nrgDetails);
            }
        }
        var RGs = await _api.CallWebApiForUserAsync("ResourceManagement",
            options => options.RelativePath = $"/subscriptions/{subscriptionId}/resourcegroups/{searchTerm}?api-version=2021-04-01");
        if (RGs.StatusCode == HttpStatusCode.OK)
        {
            resources.Add(await RGs.Content.ReadFromJsonAsync<Resource>());
        }
        return resources;
    }

    // Find what roles you can assign to the given scope - filtering out any blocked roles
    public async Task<IEnumerable<RoleDefinition>> GetRoles(string scope)
    {
        return from role in (await _api.CallWebApiForUserAsync<RoleDefinitionList>("ResourceManagement", options => 
                        options.RelativePath = $"{scope}/providers/Microsoft.Authorization/roleDefinitions?api-version=2015-07-01")).Value
               where !_settings.BlockedRoles.Any(blockedRole => blockedRole.Equals(role.Properties.RoleName, StringComparison.OrdinalIgnoreCase))
               select role;
    }

    // Find who you want to assign roles to
    public async Task<IEnumerable<ServicePrincipal>> SearchPrincipals(string searchTerm)
    {
        //$"https://graph.microsoft.com/v1.0/servicePrincipals?$filter=displayName eq '{ServicePrincipalSearchTerm}'"
        var result = await _api.CallWebApiForAppAsync("GraphApi", options =>
        {
            options.RelativePath = $"/servicePrincipals?$filter=startsWith(displayName, '{searchTerm}')&$top=999";
            options.Scopes = "https://graph.microsoft.com/.default";
        });

        if (result.StatusCode == HttpStatusCode.OK)
        {
            return (await result.Content.ReadFromJsonAsync<ODataResponse<ServicePrincipal>>()).Value;
        }

        return null;
    }

    public async Task<(bool, string)> CreateRoleAssignment(string scope, string principalId, string roleDefId)
    {
        var request = new RoleAssignmentRequest()
        {
            Properties = new RoleAssignmentProperties()
            {
                PrincipalId = principalId,
                RoleDefinitionId = roleDefId
            }
        };
        var path = $"{scope}/providers/Microsoft.Authorization/roleAssignments/{Guid.NewGuid().ToString()}?api-version=2015-07-01";

        var result = await _api.CallWebApiForAppAsync("ResourceManagement", options =>
        {
            options.HttpMethod = HttpMethod.Put;
            options.Scopes = "https://management.azure.com/.default";
            options.RelativePath = path;
        }, new StringContent(JsonSerializer.Serialize(request), new MediaTypeHeaderValue("application/json")));

        if (result.StatusCode == HttpStatusCode.Created)
        {
            return (true, "");
        }
        return (false, (await result.Content.ReadFromJsonAsync<RoleAssignmentRequest>()).Error.Message);
    }
}
