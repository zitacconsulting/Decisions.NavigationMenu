using DecisionsFramework;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.ServiceLayer;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Services.Image;
using DecisionsFramework.ServiceLayer.Utilities;

namespace Decisions.NavigationMenu;

/// <summary>
/// Defines the behaviour of the top-level system folder "Navigation Menu".
///
/// <para>
/// <b>SystemFolderBehavior</b> is the base class for folders that are created and owned
/// by a module rather than by a user. The platform ensures these folders always exist and
/// places them in a predefined location in the folder tree. System folders are shared
/// across all projects.
/// </para>
///
/// <para>
/// <b>IInitializable</b> causes Decisions to call <see cref="Initialize"/> once on every
/// server start. This is where we ensure the folder exists in the database.
/// </para>
///
/// <para>
/// <b>ILookAndFeelProviderFolderBehavior</b> lets the folder expose its own icon without
/// relying on the "Use default icons in folder tree" portal setting. Without this interface,
/// the platform only shows folder icons when that setting is turned on. Implementing it
/// means our icon is always visible regardless of the portal configuration.
/// </para>
/// </summary>
public class NavMenuRootFolderBehavior : SystemFolderBehavior, IInitializable, ILookAndFeelProviderFolderBehavior
{
    /// <summary>
    /// The fixed database ID for this folder. Using a hard-coded string (rather than a
    /// generated GUID) means the folder is always found by the same ID across all
    /// installations and upgrades.
    /// </summary>
    public const string FOLDER_ID = "NAV_MENU_ROOT_FOLDER";

    public const string FOLDER_NAME = "Navigation Menu";

    // Prevent users from adding this folder to Favourites, renaming it, or
    // changing its default landing page — it is a module-owned system folder.
    public override bool CanBeFavorites => false;
    public override bool CanChangeNameAndDescription() => false;
    public override bool CanUserChangeDefaultPage() => false;

    // Only sub-folders (not configs or themes) may be placed directly in the root folder.
    public override bool CanAddEntity(AbstractFolderEntity entity) => entity is Folder;

    // Always show this folder in the navigation tree, regardless of portal settings.
    public override bool? ShowInTreeView() => true;

    /// <summary>
    /// Returns the icon shown in the legacy (non-React) folder tree.
    /// The React tree reads the icon from <see cref="GetLookAndFeel"/> instead.
    /// Both methods must return the same icon to stay consistent.
    /// </summary>
    public override ImageInfo GetIcon() => NavMenuImages.RootIcon;

    /// <summary>
    /// Returns the visual appearance (colour + icon) for this folder in the React folder tree.
    ///
    /// <para>
    /// <c>Color = null</c> means the folder label renders in the default black text and the
    /// icon SVG renders with its native fill colour (blue in this case — acceptable because
    /// only administrators see system folders).
    /// </para>
    ///
    /// <para>
    /// A stored <see cref="LookAndFeel"/> entity in the database always takes priority over
    /// this method. See <see cref="EnsureLookAndFeel"/> for how we handle stale DB records.
    /// </para>
    /// </summary>
    public LookAndFeel GetLookAndFeel(string folderId)
        => new LookAndFeel(folderId, null, null, NavMenuImages.RootIcon);

    /// <summary>
    /// Remove all toolbar actions from this folder. Users cannot create anything directly
    /// inside the root folder — configs and themes live in their own sub-folders.
    /// </summary>
    public override BaseActionType[] GetFolderActions(Folder folder, BaseActionType[] proposedActions, EntityActionType[] types)
        => System.Array.Empty<BaseActionType>();

    /// <summary>
    /// Called by Decisions on every server start. Creates the folder if it does not yet
    /// exist, then corrects any stale look-and-feel data in the database.
    /// </summary>
    public void Initialize()
    {
        var orm = new ORM<Folder>();
        var folder = orm.Fetch(FOLDER_ID);

        if (folder == null)
        {
            folder = new Folder(FOLDER_ID, FOLDER_NAME, "FLOW MANAGEMENT FOLDER")
            {
                FolderBehaviorType = typeof(NavMenuRootFolderBehavior).FullName,
                EntityDescription = "Navigation Menu configs and themes."
            };
            orm.Store(folder);

            // Grant all users read access so the folder is visible in the tree.
            SystemFolders.SetCanUsePermission("ALL USERS GROUP", FOLDER_ID);
        }

        EnsureLookAndFeel(FOLDER_ID, NavMenuImages.RootIcon);
    }

    /// <summary>
    /// Corrects a stale <see cref="LookAndFeel"/> record in the database if one exists
    /// with a non-null colour.
    ///
    /// <para>
    /// Decisions stores look-and-feel settings as a <see cref="LookAndFeel"/> ORM entity
    /// in the <c>look_and_feel</c> database table. A stored entity always takes priority
    /// over <see cref="GetLookAndFeel"/>, so if an old version of this module saved a
    /// blue colour for this folder, it would override our null-colour implementation even
    /// after deploying the corrected code.
    /// </para>
    ///
    /// <para>
    /// This method detects that situation and fixes the stored record so the folder
    /// displays correctly without requiring manual database intervention.
    /// </para>
    /// </summary>
    private static void EnsureLookAndFeel(string folderId, ImageInfo icon)
    {
        var stored = LookAndFeelHelper.GetForFolder(folderId);

        // If no stored record exists, GetLookAndFeel() above will be used — nothing to fix.
        // If the stored record already has no colour, it is already correct.
        if (stored == null || stored.Color == null)
            return;

        stored.Color = null;
        stored.Icon = icon;
        stored.Store();
    }
}
