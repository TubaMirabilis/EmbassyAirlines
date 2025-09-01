using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

namespace Deployment;

internal static class IdentityService
{
    public static async Task<Role> EnsureRoleAsync(string roleName)
    {
        Console.WriteLine($"Ensuring role '{roleName}' exists...");
        using var iamClient = new AmazonIdentityManagementServiceClient();
        var req = new ListRolesRequest();
        var res = await iamClient.ListRolesAsync(req);
        var existingRole = res.Roles.Find(r => r.RoleName == roleName);
        if (existingRole is not null)
        {
            Console.WriteLine($"Role {existingRole.RoleName} already exists.");
            return existingRole;
        }
        var req2 = new CreateRoleRequest
        {
            RoleName = roleName,
            AssumeRolePolicyDocument = @"{
                ""Version"": ""2012-10-17"",
                ""Statement"": [
                    {
                        ""Effect"": ""Allow"",
                        ""Principal"": {
                            ""Service"": ""lambda.amazonaws.com""
                        },
                        ""Action"": ""sts:AssumeRole""
                    }
                ]
            }"
        };
        var res2 = await iamClient.CreateRoleAsync(req2);
        return res2.Role;
    }
    public static async Task AttachPolicyAsync(string policyArn, string roleName)
    {
        Console.WriteLine($"Attaching policy '{policyArn}' to role '{roleName}'...");
        using var iamClient = new AmazonIdentityManagementServiceClient();
        var req = new AttachRolePolicyRequest
        {
            PolicyArn = policyArn,
            RoleName = roleName
        };
        await iamClient.AttachRolePolicyAsync(req);
    }
}
