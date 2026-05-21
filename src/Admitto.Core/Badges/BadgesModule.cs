using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypeManagement.AddBadgeType.AdminApi;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypeManagement.DeleteBadgeType.AdminApi;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypeManagement.ListBadgeTypes.AdminApi;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypeManagement.RenameBadgeType.AdminApi;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstanceManagement.AddBadgeInstance.AdminApi;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstanceManagement.DeleteBadgeInstance.AdminApi;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstanceManagement.ListBadgeInstances.AdminApi;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstanceManagement.UpdateBadgeInstance.AdminApi;
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
