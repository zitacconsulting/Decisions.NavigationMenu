using DecisionsFramework.Design.Projects.FolderStructure;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Services.Image;

namespace Decisions.NavigationMenu;

/// <summary>
/// Defines the behaviour of the per-project "Navigation Menu" root folder.
///
/// <para>
/// <b>Project folders vs system folders:</b> System folders (see
/// <see cref="NavMenuRootFolderBehavior"/>) are shared singletons created once for the
/// entire Decisions installation. Project folders are created once per project and their
/// IDs are derived from the project ID using a known suffix, so each project gets its
/// own isolated copy of the folder.
/// </para>
///
/// <para>
/// <b>AbstractProjectManageFolderBehavior</b> is the base class for project-scoped folders.
/// It already implements <c>ILookAndFeelProviderFolderBehavior</c>, so our
/// <see cref="GetLookAndFeel"/> override is automatically picked up by the React folder
/// tree without needing to declare the interface separately. The platform calls
/// <see cref="GetLookAndFeel"/> when building the folder tree; it does NOT fall back to a
/// stored database record for project folders the way it does for system folders.
/// </para>
/// </summary>
public class NavMenuProjectRootFolderBehavior : AbstractProjectManageFolderBehavior
{
    /// <summary>
    /// Suffix appended to the project ID to form this folder's unique database ID.
    /// For example, if the project ID is <c>"proj123"</c>, the folder ID will be
    /// <c>"proj123.navmenuroot"</c>.
    /// </summary>
    public const string FOLDER_SUFFIX = "navmenuroot";

    /// <summary>Returns the folder ID for a given project.</summary>
    public static string GetFolderId(string projectId) => projectId + "." + FOLDER_SUFFIX;

    /// <summary>
    /// Returns the visual appearance (colour + icon) for this folder in the React folder tree.
    ///
    /// <para>
    /// <c>Color = "#21659D"</c> is the standard Decisions blue used for project folders.
    /// This colour is applied to both the folder label text and the SVG icon fill,
    /// making the icon render in blue to match the project folder style convention.
    /// </para>
    /// </summary>
    public override LookAndFeel GetLookAndFeel(string folderId)
        => new LookAndFeel(folderId, "#21659D", null, NavMenuImages.RootIcon);

    /// <summary>
    /// Marks this folder as exportable so it is included in project exports and check-in
    /// packages. The folder entity itself (not just its children) must be in the export so
    /// that on project checkout to a new server, the folder record is recreated with the
    /// same ID before the Configs and Themes sub-folders (which reference this folder as
    /// their parent) are imported.
    ///
    /// <para>
    /// <c>ExportChildrenOnly</c> defaults to <c>false</c> (from
    /// <see cref="DefaultFolderBehavior"/>) — this means the folder entity itself IS
    /// included in the export alongside its children.
    /// </para>
    ///
    /// <para>
    /// <b>Important:</b> <c>IsFolderExportable</c> is a computed <c>[ORMField]</c> on
    /// <see cref="DecisionsFramework.ServiceLayer.Services.Folder.Folder"/>. The DB column
    /// is only written when the folder entity is stored — it is NOT read back on load (the
    /// getter always calls the behavior at runtime). This means existing folders created by
    /// an older version of the module need to be re-stored once to update the DB column.
    /// <see cref="NavigationMenuModule.Initialize"/> handles this via
    /// <c>EnsureProjectFolders()</c> on every server start.
    /// </para>
    /// </summary>
    public override bool IsExportable(Folder f) => true;

    /// <summary>
    /// No toolbar actions are shown for this root folder — users navigate into
    /// the Configs or Themes sub-folders to create or manage entities.
    /// </summary>
    public override BaseActionType[] GetFolderActions(Folder folder, BaseActionType[] proposedActions, EntityActionType[] types)
        => System.Array.Empty<BaseActionType>();
}
