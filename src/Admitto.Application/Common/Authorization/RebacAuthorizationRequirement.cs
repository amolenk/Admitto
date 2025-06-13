using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.Application.Common.Authorization;

public class RebacAuthorizationRequirement(string relation, string objectType, Func<HttpContext, string> getObjectId)
    : IAuthorizationRequirement
{
    public string Relation { get; } = relation;
    public string ObjectType { get; } = objectType;
    public Func<HttpContext, string> GetObjectId { get; } = getObjectId;
}
