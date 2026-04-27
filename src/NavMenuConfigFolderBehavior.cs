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
/// Defines the behaviour of the system-level "Configs" folder, which lives inside
/// the "Navigation Menu" root folder and stores all <see cref="NavigationMenuConfig"/>
/// entities that are shared across projects.
///
/// <para>
/// See <see cref="NavMenuRootFolderBehavior"/> for a full explanation of
/// <c>SystemFolderBehavior</c>, <c>IInitializable</c>, and
/// <c>ILookAndFeelProviderFolderBehavior</c>.
/// </para>
/// </summary>
public class NavMenuConfigFolderBehavior : SystemFolderBehavior, IInitializable, ILookAndFeelProviderFolderBehavior
{
    public const string FOLDER_ID = "NAV_MENU_CONFIG_FOLDER";
    public const string FOLDER_NAME = "Configs";

    public override bool CanBeFavorites => false;
    public override bool CanChangeNameAndDescription() => false;
    public override bool CanUserChangeDefaultPage() => false;

    // Allow both NavigationMenuConfig entities and sub-folders inside this folder.
    public override bool CanAddEntity(AbstractFolderEntity entity)
        => entity is NavigationMenuConfig || entity is Folder;

    public override bool? ShowInTreeView() => true;

    /// <summary>
    /// Returns the icon for the legacy (non-React) folder tree.
    /// See <see cref="GetLookAndFeel"/> for the React tree equivalent.
    /// </summary>
    public override ImageInfo GetIcon() => NavMenuImages.ConfigIcon;

    /// <summary>
    /// Returns the visual appearance for the React folder tree.
    /// <c>Color = null</c> keeps the folder label in default black text and lets
    /// the icon SVG render with its native colour.
    /// </summary>
    public LookAndFeel GetLookAndFeel(string folderId)
        => new LookAndFeel(folderId, null, null, NavMenuImages.ConfigIcon);

    /// <summary>
    /// Builds the list of toolbar actions visible when this folder is open.
    ///
    /// <para>
    /// The platform passes all <paramref name="proposedActions"/> that it would normally
    /// show for any folder. We filter and augment that list to show only what is relevant:
    /// <list type="bullet">
    ///   <item>Navigation actions (e.g. breadcrumbs) are always kept.</item>
    ///   <item>An "Add Navigation Menu Config" action is shown only to users who have
    ///         add permission or are portal administrators.</item>
    ///   <item>Import/Export actions are kept so admins can move configs between environments.</item>
    /// </list>
    /// </para>
    /// </summary>
    public override BaseActionType[] GetFolderActions(Folder folder, BaseActionType[] proposedActions, EntityActionType[] types)
    {
        var list = new List<BaseActionType>();

        // Always include navigation actions (breadcrumbs, folder navigation links).
        list.AddRange(proposedActions.Where(a => a is NavigateToFolderAction));

        // Check whether the current user is allowed to create entities in this folder.
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
                // DisplayType.Primary renders the action as a prominent button in the toolbar.
                DisplayType = ActionDisplayType.Primary
            });
        }

        // Retain any import/export actions provided by the platform so admins can
        // move configs between environments using Decisions' built-in export system.
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
        EnsureLookAndFeel(FOLDER_ID, NavMenuImages.ConfigIcon);

        // SetCanUsePermission only needs to run on a fresh install or after an upgrade —
        // not on every start — to avoid overwriting any permission changes made by admins.
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
    /// if either was changed outside the module (e.g. by a direct database edit or an
    /// older version of the code).
    /// </summary>
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
            // Self-heal: repair the name or parent if they drifted from what the module expects.
            folder.EntityName = FOLDER_NAME;
            folder.EntityFolderID = NavMenuRootFolderBehavior.FOLDER_ID;
            orm.Store(folder);
        }
    }
}
