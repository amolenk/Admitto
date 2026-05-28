using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.AddBadgeType.AdminApi;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.DeleteBadgeType.AdminApi;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.ListBadgeTypes.AdminApi;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.RenameBadgeType.AdminApi;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.AddBadgeInstance.AdminApi;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.DeleteBadgeInstance.AdminApi;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.ListBadgeInstances.AdminApi;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.UpdateBadgeInstance.AdminApi;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeExport.ExportBadgeCsv.AdminApi;

namespace Amolenk.Admitto.Core.Badges;

public static class BadgesModule
{
    public const string Key = nameof(Badges);
    public const string NamespacePrefix = "Amolenk.Admitto.Core.Badges";

    public static RouteGroupBuilder MapBadgesAdminEndpoints(this RouteGroupBuilder group)
    {
        var badgeTypesGroup = group
            .MapGroup("/teams/{teamId:guid}/events/{eventId:guid}/badge-types");

        badgeTypesGroup
            .MapAddBadgeType()
            .MapRenameBadgeType()
            .MapDeleteBadgeType()
            .MapListBadgeTypes()
            .MapAddBadgeInstance()
            .MapUpdateBadgeInstance()
            .MapDeleteBadgeInstance()
            .MapListBadgeInstances()
            .MapExportBadgeCsv();

        return group;
    }
}
