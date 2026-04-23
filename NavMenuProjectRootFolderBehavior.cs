using DecisionsFramework.Design.Projects.FolderStructure;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Services.Image;

namespace Decisions.NavigationMenu;

public class NavMenuProjectRootFolderBehavior : AbstractProjectManageFolderBehavior
{
    public const string FOLDER_SUFFIX = "navmenuroot";

    public static string GetFolderId(string projectId) => projectId + "." + FOLDER_SUFFIX;

    public override LookAndFeel GetLookAndFeel(string folderId)
        => new LookAndFeel(folderId, "#21659D", null, NavMenuImages.RootIcon);

    public override BaseActionType[] GetFolderActions(Folder folder, BaseActionType[] proposedActions, EntityActionType[] types)
        => System.Array.Empty<BaseActionType>();
}
