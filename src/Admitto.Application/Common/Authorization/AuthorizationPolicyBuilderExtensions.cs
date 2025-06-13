using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.Application.Common.Authorization;

public static class AuthorizationPolicyBuilderExtensions
{
    public static AuthorizationPolicyBuilder RequireRebacCheck(this AuthorizationPolicyBuilder builder, string relation)
    {
        return builder.RequireRebacCheck(relation, "system", "system");
    }

    public static AuthorizationPolicyBuilder RequireRebacCheck(this AuthorizationPolicyBuilder builder, string relation,
        string objectType, string objectId)
    {
        return builder.RequireRebacCheck(relation, objectType, _ => objectId);
    }
    
    public static AuthorizationPolicyBuilder RequireRebacCheck(this AuthorizationPolicyBuilder builder, string relation,
        string objectType, Func<HttpContext, string> getObjectId)
    {
        builder.Requirements.Add(new RebacAuthorizationRequirement(relation, objectType, getObjectId));
        return builder;
    }
}
