using DecisionsFramework;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.ServiceLayer;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Services.Image;
using DecisionsFramework.ServiceLayer.Utilities;

namespace Decisions.NavigationMenu;

public class NavMenuRootFolderBehavior : SystemFolderBehavior, IInitializable
{
    public const string FOLDER_ID = "NAV_MENU_ROOT_FOLDER";
    public const string FOLDER_NAME = "Navigation Menu";

    public override bool CanBeFavorites => false;
    public override bool CanChangeNameAndDescription() => false;
    public override bool CanUserChangeDefaultPage() => false;
    public override bool CanAddEntity(AbstractFolderEntity entity) => entity is Folder;
    public override bool? ShowInTreeView() => true;

    public override ImageInfo GetIcon() => NavMenuImages.RootIcon;

    public override BaseActionType[] GetFolderActions(Folder folder, BaseActionType[] proposedActions, EntityActionType[] types)
        => System.Array.Empty<BaseActionType>();

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
            SystemFolders.SetCanUsePermission("ALL USERS GROUP", FOLDER_ID);
        }
        else
        {
            orm.Store(folder);
        }
    }
}
