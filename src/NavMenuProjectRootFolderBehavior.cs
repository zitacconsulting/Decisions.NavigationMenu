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
    /// No toolbar actions are shown for this root folder — users navigate into
    /// the Configs or Themes sub-folders to create or manage entities.
    /// </summary>
    public override BaseActionType[] GetFolderActions(Folder folder, BaseActionType[] proposedActions, EntityActionType[] types)
        => System.Array.Empty<BaseActionType>();
}
