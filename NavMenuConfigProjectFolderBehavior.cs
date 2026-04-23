using DecisionsFramework.Design.Projects.FolderStructure;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Services.Image;
using DecisionsFramework.ServiceLayer;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Actions.Common;
using DecisionsFramework.ServiceLayer.Services.Accounts;
using DecisionsFramework.ServiceLayer.Services.Administration;
using DecisionsFramework.ServiceLayer.Utilities;
using DecisionsFramework.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Decisions.NavigationMenu;

public class NavMenuConfigProjectFolderBehavior : AbstractProjectManageFolderBehavior
{
    public const string FOLDER_SUFFIX = "navmenu";

    public static string GetFolderId(string projectId) => projectId + "." + FOLDER_SUFFIX;

    public override bool IsExportable(Folder f) => true;

    public override bool ExportChildrenOnly(Folder folder) => true;

    public override LookAndFeel GetLookAndFeel(string folderId)
        => new LookAndFeel(folderId, "#21659D", null, NavMenuImages.ConfigIcon);

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

    public override bool CanAddEntity(AbstractFolderEntity entity)
        => entity is NavigationMenuConfig || entity is Folder;
}
