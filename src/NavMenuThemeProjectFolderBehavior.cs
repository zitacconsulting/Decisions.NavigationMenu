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

/// <summary>
/// Defines the behaviour of the per-project "Themes" folder, which stores
/// <see cref="NavigationMenuTheme"/> entities that belong to a specific project.
///
/// <para>
/// See <see cref="NavMenuProjectRootFolderBehavior"/> for an explanation of how
/// project folders work and how their IDs are derived.
/// </para>
/// </summary>
public class NavMenuThemeProjectFolderBehavior : AbstractProjectManageFolderBehavior
{
    /// <summary>
    /// Suffix used to build the folder ID for a given project.
    /// Example: project <c>"proj123"</c> → folder ID <c>"proj123.navmenuthemes"</c>.
    /// </summary>
    public const string FOLDER_SUFFIX = "navmenuthemes";

    /// <summary>Returns the folder ID for a given project.</summary>
    public static string GetFolderId(string projectId) => projectId + "." + FOLDER_SUFFIX;

    /// <summary>
    /// Returns the visual appearance for the React folder tree.
    ///
    /// <para>
    /// Uses <see cref="NavMenuImages.ThemeProjectIcon"/> (the coloured hover variant of
    /// the color picker SVG) rather than <see cref="NavMenuImages.ThemeIcon"/> (the gray
    /// default variant used by the system Themes folder). The <c>#21659D</c> colour
    /// override tints it blue, making it consistent with the other blue project folders.
    /// </para>
    /// </summary>
    public override LookAndFeel GetLookAndFeel(string folderId)
        => new LookAndFeel(folderId, "#21659D", null, NavMenuImages.ThemeProjectIcon);

    /// <summary>
    /// Marks this folder as exportable so themes are included in project check-in and
    /// export packages. Without this, <c>is_folder_exportable = false</c> in the DB and
    /// the project check-in scanner never visits this folder — theme changes go undetected.
    ///
    /// <para>
    /// <c>ExportChildrenOnly</c> defaults to <c>false</c>, so both the folder record itself
    /// and the theme entities inside it are included in the export. The folder record must
    /// be in the export so it is recreated on checkout before the theme entities that
    /// reference it as their parent folder.
    /// </para>
    ///
    /// <para>
    /// See <see cref="NavMenuProjectRootFolderBehavior.IsExportable"/> for a full
    /// explanation of how <c>IsFolderExportable</c> (a computed <c>[ORMField]</c>) works
    /// and why <see cref="NavigationMenuModule"/> re-stores existing folders on startup.
    /// </para>
    /// </summary>
    public override bool IsExportable(Folder f) => true;

    // Allow both NavigationMenuTheme entities and sub-folders inside this folder.
    public override bool CanAddEntity(AbstractFolderEntity entity)
        => entity is NavigationMenuTheme || entity is Folder;

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
