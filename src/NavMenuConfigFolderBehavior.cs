using System.Collections.Generic;
using System.Linq;
using DecisionsFramework;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.ServiceLayer;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Actions.Common;
using DecisionsFramework.ServiceLayer.Services.Accounts;
using DecisionsFramework.ServiceLayer.Services.Administration;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Services.Image;
using DecisionsFramework.ServiceLayer.Utilities;
using DecisionsFramework.Utilities;

namespace Decisions.NavigationMenu;

public class NavMenuConfigFolderBehavior : SystemFolderBehavior, IInitializable
{
    public const string FOLDER_ID = "NAV_MENU_CONFIG_FOLDER";
    public const string FOLDER_NAME = "Configs";

    public override bool CanBeFavorites => false;
    public override bool CanChangeNameAndDescription() => false;
    public override bool CanUserChangeDefaultPage() => false;

    public override bool CanAddEntity(AbstractFolderEntity entity)
        => entity is NavigationMenuConfig || entity is Folder;
    public override bool? ShowInTreeView() => true;

    public override ImageInfo GetIcon() => NavMenuImages.ConfigIcon;

    public override BaseActionType[] GetFolderActions(Folder folder, BaseActionType[] proposedActions, EntityActionType[] types)
    {
        var list = new List<BaseActionType>();
        list.AddRange(proposedActions.Where(a => a is NavigateToFolderAction));

        var account = UserContextHolder.GetCurrent().GetAccount();
        bool canAdd = account.GetUserRights<PortalAdministratorModuleRight>() != null
            || FolderPermissionHelper.VerifyAccountPermission(folder.FolderID, account, FolderPermission.CanAdd);

        if (canAdd)
        {
            list.Add(new AddEntityAction(
                typeof(NavigationMenuConfig),
                "Add Navigation Menu Config",
                "Create a new reusable navigation menu configuration",
                "Add Navigation Menu Config")
            {
                DisplayType = ActionDisplayType.Primary
            });
        }

        list.AddRange(proposedActions.Where(a => a.Category == "Import/Export"));
        return list.ToArray();
    }

    public void Initialize()
    {
        EnsureFolder();

        if (DecisionsVersion.IsCleanInstallOrUpgrade())
            SystemFolders.SetCanUsePermission("ALL USERS GROUP", FOLDER_ID);
    }

    private static void EnsureFolder()
    {
        var orm = new ORM<Folder>();
        var folder = orm.Fetch(FOLDER_ID);

        if (folder == null)
        {
            folder = new Folder(FOLDER_ID, FOLDER_NAME, NavMenuRootFolderBehavior.FOLDER_ID)
            {
                FolderBehaviorType = typeof(NavMenuConfigFolderBehavior).FullName,
                EntityDescription = "Store reusable navigation menu configurations shared across pages and projects."
            };
            orm.Store(folder);
            SystemFolders.SetCanUsePermission("ALL USERS GROUP", FOLDER_ID);
        }
        else if (folder.EntityName != FOLDER_NAME || folder.EntityFolderID != NavMenuRootFolderBehavior.FOLDER_ID)
        {
            folder.EntityName = FOLDER_NAME;
            folder.EntityFolderID = NavMenuRootFolderBehavior.FOLDER_ID;
            orm.Store(folder);
        }
    }
}
