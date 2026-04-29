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

/// <summary>
/// Defines the behaviour of the per-project "Configs" folder, which stores
/// <see cref="NavigationMenuConfig"/> entities that belong to a specific project.
///
/// <para>
/// See <see cref="NavMenuProjectRootFolderBehavior"/> for an explanation of how
/// project folders work and how their IDs are derived.
/// </para>
/// </summary>
public class NavMenuConfigProjectFolderBehavior : AbstractProjectManageFolderBehavior
{
    /// <summary>
    /// Suffix used to build the folder ID for a given project.
    /// Example: project <c>"proj123"</c> → folder ID <c>"proj123.navmenu"</c>.
    /// </summary>
    public const string FOLDER_SUFFIX = "navmenu";

    /// <summary>Returns the folder ID for a given project.</summary>
    public static string GetFolderId(string projectId) => projectId + "." + FOLDER_SUFFIX;

    /// <summary>
    /// Marks this folder as exportable and ensures the folder entity itself is included
    /// in the export (not just its children).
    ///
    /// <para>
    /// <b>Why the folder entity must be exported (<c>ExportChildrenOnly = false</c>):</b>
    /// When a project is checked out to a server where it has never existed before,
    /// <c>OnDependencyAdded</c> is NOT called — only the data records from the export ZIP
    /// are imported. The import system creates folder entities before non-folder entities,
    /// so if the config folder record is in the ZIP, it is created before the
    /// <see cref="NavigationMenuConfig"/> entities that reference it as their parent. Without
    /// the folder record in the export, those configs land in "Orphan Entities".
    /// </para>
    ///
    /// <para>
    /// See <see cref="NavMenuProjectRootFolderBehavior.IsExportable"/> for a full
    /// explanation of how <c>IsFolderExportable</c> (a computed <c>[ORMField]</c>) works
    /// and why <see cref="NavigationMenuModule"/> re-stores existing folders on startup.
    /// </para>
    /// </summary>
    public override bool IsExportable(Folder f) => true;
    public override bool ExportChildrenOnly(Folder folder) => false;

    /// <summary>
    /// Returns the visual appearance for the React folder tree.
    /// Uses the standard Decisions project blue (<c>#21659D</c>) to tint both the
    /// folder label text and the icon SVG.
    /// </summary>
    public override LookAndFeel GetLookAndFeel(string folderId)
        => new LookAndFeel(folderId, "#21659D", null, NavMenuImages.ConfigIcon);

    /// <summary>
    /// Builds the list of toolbar actions visible when this folder is open.
    /// See <see cref="NavMenuConfigFolderBehavior.GetFolderActions"/> for a full
    /// explanation of the filtering logic — this follows the same pattern.
    /// </summary>
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

    // Allow both NavigationMenuConfig entities and sub-folders inside this folder.
    public override bool CanAddEntity(AbstractFolderEntity entity)
        => entity is NavigationMenuConfig || entity is Folder;
}
