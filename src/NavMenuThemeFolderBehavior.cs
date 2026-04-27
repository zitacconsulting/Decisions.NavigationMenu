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

/// <summary>
/// Defines the behaviour of the system-level "Themes" folder, which lives inside
/// the "Navigation Menu" root folder and stores all <see cref="NavigationMenuTheme"/>
/// entities that are shared across projects.
///
/// <para>
/// See <see cref="NavMenuRootFolderBehavior"/> for a full explanation of
/// <c>SystemFolderBehavior</c>, <c>IInitializable</c>, and
/// <c>ILookAndFeelProviderFolderBehavior</c>.
/// </para>
/// </summary>
public class NavMenuThemeFolderBehavior : SystemFolderBehavior, IInitializable, ILookAndFeelProviderFolderBehavior
{
    public const string FOLDER_ID = "NAV_MENU_THEME_FOLDER";
    public const string FOLDER_NAME = "Themes";

    public override bool CanBeFavorites => false;
    public override bool CanChangeNameAndDescription() => false;
    public override bool CanUserChangeDefaultPage() => false;

    // Allow both NavigationMenuTheme entities and sub-folders inside this folder.
    public override bool CanAddEntity(AbstractFolderEntity entity)
        => entity is NavigationMenuTheme || entity is Folder;

    public override bool? ShowInTreeView() => true;

    /// <summary>
    /// Returns the icon for the legacy (non-React) folder tree.
    /// See <see cref="GetLookAndFeel"/> for the React tree equivalent.
    /// </summary>
    public override ImageInfo GetIcon() => NavMenuImages.ThemeIcon;

    /// <summary>
    /// Returns the visual appearance for the React folder tree.
    /// <c>Color = null</c> keeps the folder label in default black text and lets
    /// the icon SVG render with its native grey colour (unlike Root and Config, the
    /// ThemeIcon SVG is natively grey so no colour override is needed).
    /// </summary>
    public LookAndFeel GetLookAndFeel(string folderId)
        => new LookAndFeel(folderId, null, null, NavMenuImages.ThemeIcon);

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

    /// <summary>
    /// Called by Decisions on every server start. Ensures the folder exists with the
    /// correct name and parent, then corrects any stale look-and-feel data.
    /// </summary>
    public void Initialize()
    {
        EnsureFolder();
        EnsureLookAndFeel(FOLDER_ID, NavMenuImages.ThemeIcon);

        if (DecisionsVersion.IsCleanInstallOrUpgrade())
            SystemFolders.SetCanUsePermission("ALL USERS GROUP", FOLDER_ID);
    }

    /// <summary>
    /// Corrects a stale <see cref="LookAndFeel"/> record in the database if one exists
    /// with a non-null colour. See <see cref="NavMenuRootFolderBehavior.EnsureLookAndFeel"/>
    /// for a full explanation of why this is necessary.
    /// </summary>
    private static void EnsureLookAndFeel(string folderId, ImageInfo icon)
    {
        var stored = LookAndFeelHelper.GetForFolder(folderId);
        if (stored == null || stored.Color == null)
            return;

        stored.Color = null;
        stored.Icon = icon;
        stored.Store();
    }

    /// <summary>
    /// Creates the folder if it does not exist, or repairs its name and parent folder ID
    /// if either drifted from the expected values. See
    /// <see cref="NavMenuConfigFolderBehavior.EnsureFolder"/> for a full explanation.
    /// </summary>
    private static void EnsureFolder()
    {
        var orm = new ORM<Folder>();
        var folder = orm.Fetch(FOLDER_ID);

        if (folder == null)
        {
            folder = new Folder(FOLDER_ID, FOLDER_NAME, NavMenuRootFolderBehavior.FOLDER_ID)
            {
                FolderBehaviorType = typeof(NavMenuThemeFolderBehavior).FullName,
                EntityDescription = "Store reusable navigation menu themes shared across configs."
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
