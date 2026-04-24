using System;
using DecisionsFramework;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.Design.Form;
using DecisionsFramework.Design.Projects.Dependency;
using DecisionsFramework.ServiceLayer;
using DecisionsFramework.ServiceLayer.Services.ConfigurationStorage;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Services.Projects;
using DecisionsFramework.ServiceLayer.Utilities;

namespace Decisions.NavigationMenu;

public class NavigationMenuModule : IInitializable, IModuleDependencyInitializer
{
    private static readonly Log log = new Log("NavigationMenu");

    public string ModuleName => "Decisions.NavigationMenu";

    public void Initialize()
    {
        log.Info($"Navigation Menu module version {ModuleVersion.Current} loaded.");
        RegisterToolboxElement();
        MigrateNavMenuFolders();
    }

    private void RegisterToolboxElement()
    {
        var ctx = new SystemUserContext();

        // ModuleFormStepFactory.GetDependentModuleStepFolders() intersects "Module Steps Folder"
        // children with the project's dependent module names. Created by ModuleImporter on ZIP
        // install — must be ensured in dev mode too.
        FolderStructureHelper.CreateFolderIfNotExistsAndSendEvent(
            ctx, "Module Steps Folder", ModuleName, ModuleName,
            "DecisionsFramework.ServiceLayer.Utilities.ComponentsFolder");

        // FormElementsFactory browses sub-folders of the module step folder via
        // FindSubFoldersWithConfigurationsByQuery — elements must be in a named sub-folder,
        // not directly in the module root folder.
        var navFolderId = FolderService.CreateSubPath(
            ctx, ModuleName, "Navigation",
            "DecisionsFramework.ServiceLayer.Utilities.ComponentsFolder");

        var className = typeof(NavigationMenuPart).AssemblyQualifiedName;
        var orm = new ORM<ElementRegistration>();

        var existing = orm.Fetch(new WhereCondition[]
        {
            new FieldWhereCondition("class_name", QueryMatchType.Equals, className)
        });

        if (existing != null && existing.Length > 0)
        {
            foreach (var reg in existing)
            {
                if (reg.EntityFolderID != navFolderId)
                {
                    reg.EntityFolderID = navFolderId;
                    orm.Store(reg);
                }
            }
            return;
        }

        // No registration yet — create via helper then relocate to the module sub-folder
        ConfigurationStorageService.RegisterModulesToolboxElement(
            "Navigation Menu", className, string.Empty, ModuleName, ElementType.PageElement);

        var created = orm.Fetch(new WhereCondition[]
        {
            new FieldWhereCondition("class_name", QueryMatchType.Equals, className)
        });
        foreach (var reg in created ?? Array.Empty<ElementRegistration>())
        {
            if (reg.EntityFolderID != navFolderId)
            {
                reg.EntityFolderID = navFolderId;
                orm.Store(reg);
            }
        }
    }

    public void OnDependencyAdded(string projectId)
    {
        var ctx = new SystemUserContext();
        var rootFolderId   = NavMenuProjectRootFolderBehavior.GetFolderId(projectId);
        var configFolderId = NavMenuConfigProjectFolderBehavior.GetFolderId(projectId);
        var themesFolderId = NavMenuThemeProjectFolderBehavior.GetFolderId(projectId);

        FolderStructureHelper.CreateFolderIfNotExistsAndSendEvent(
            ctx, projectId + ".templates", rootFolderId,
            "Navigation Menu", typeof(NavMenuProjectRootFolderBehavior).FullName);

        FolderStructureHelper.CreateFolderIfNotExistsAndSendEvent(
            ctx, rootFolderId, configFolderId,
            "Configs", typeof(NavMenuConfigProjectFolderBehavior).FullName);

        FolderStructureHelper.CreateFolderIfNotExistsAndSendEvent(
            ctx, rootFolderId, themesFolderId,
            "Themes", typeof(NavMenuThemeProjectFolderBehavior).FullName);

        EnsureProjectDefaultTheme(themesFolderId);
    }

    public void OnDependencyRemoved(string projectId)
    {
        var rootFolderId = NavMenuProjectRootFolderBehavior.GetFolderId(projectId);
        if (new ORM<Folder>().Fetch(rootFolderId) == null)
            return;

        var configFolderId = NavMenuConfigProjectFolderBehavior.GetFolderId(projectId);
        var hasConfigs = new ORM<NavigationMenuConfig>().Fetch(
            new WhereCondition[] { new FieldWhereCondition("entity_folder_id", QueryMatchType.Equals, configFolderId) }
        )?.Length > 0;

        if (!hasConfigs)
            FolderService.Instance.DeleteFolder(new SystemUserContext(), rootFolderId, preserveSubFolders: false);
    }

    private static void MigrateNavMenuFolders()
    {
        var folderOrm = new ORM<Folder>();

        var configFolders = folderOrm.Fetch(new WhereCondition[]
        {
            new FieldWhereCondition("folder_behavior_type", QueryMatchType.Equals,
                typeof(NavMenuConfigProjectFolderBehavior).FullName)
        });

        foreach (var configFolder in configFolders ?? Array.Empty<Folder>())
        {
            var parentId = configFolder.EntityFolderID;

            if (!parentId.EndsWith(".templates"))
            {
                folderOrm.Store(configFolder);
                continue;
            }

            var projectId    = parentId.Substring(0, parentId.Length - ".templates".Length);
            var rootFolderId = NavMenuProjectRootFolderBehavior.GetFolderId(projectId);
            var themesFolderId = NavMenuThemeProjectFolderBehavior.GetFolderId(projectId);
            var ctx = new SystemUserContext();

            FolderStructureHelper.CreateFolderIfNotExistsAndSendEvent(
                ctx, parentId, rootFolderId,
                "Navigation Menu", typeof(NavMenuProjectRootFolderBehavior).FullName);

            configFolder.EntityName     = "Configs";
            configFolder.EntityFolderID = rootFolderId;
            folderOrm.Store(configFolder);

            FolderStructureHelper.CreateFolderIfNotExistsAndSendEvent(
                ctx, rootFolderId, themesFolderId,
                "Themes", typeof(NavMenuThemeProjectFolderBehavior).FullName);

            EnsureProjectDefaultTheme(themesFolderId);
        }
    }

    private static void EnsureProjectDefaultTheme(string themesFolderId)
    {
        var orm = new ORM<NavigationMenuTheme>();
        var existing = orm.Fetch(new WhereCondition[]
        {
            new FieldWhereCondition("entity_folder_id", QueryMatchType.Equals, themesFolderId)
        });
        if (existing != null && existing.Length > 0) return;

        var theme = new NavigationMenuTheme { EntityName = "Default" };
        theme.EntityFolderID = themesFolderId;
        orm.Store(theme);
    }
}
