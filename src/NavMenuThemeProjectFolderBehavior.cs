using System.Collections.Generic;
using System.Linq;
using DecisionsFramework.Design.Projects.FolderStructure;
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

public class NavMenuThemeProjectFolderBehavior : AbstractProjectManageFolderBehavior
{
    public const string FOLDER_SUFFIX = "navmenuthemes";

    public static string GetFolderId(string projectId) => projectId + "." + FOLDER_SUFFIX;

    public override LookAndFeel GetLookAndFeel(string folderId)
        => new LookAndFeel(folderId, "#21659D", null, NavMenuImages.ThemeIcon);

    public override bool CanAddEntity(AbstractFolderEntity entity)
        => entity is NavigationMenuTheme || entity is Folder;

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
                typeof(NavigationMenuTheme),
                "Add Navigation Menu Theme",
                "Create a new reusable navigation menu theme",
                "Add Navigation Menu Theme")
            {
                DisplayType = ActionDisplayType.Primary
            });
        }

        list.AddRange(proposedActions.Where(a => a.Category == "Import/Export"));
        return list.ToArray();
    }
}
