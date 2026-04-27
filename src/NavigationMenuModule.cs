using System;
using System.Collections.Generic;
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
        EnsureBuiltInThemes();
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

    private static void EnsureBuiltInThemes()
    {
        var orm = new ORM<NavigationMenuTheme>();
        var existing = orm.Fetch(new WhereCondition[]
        {
            new FieldWhereCondition("entity_folder_id", QueryMatchType.Equals, NavMenuThemeFolderBehavior.FOLDER_ID)
        }) ?? Array.Empty<NavigationMenuTheme>();

        var existingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in existing)
            existingIds.Add(t.GetEntityId() ?? string.Empty);

        foreach (var theme in BuiltInThemes())
        {
            if (existingIds.Contains(theme.GetEntityId())) continue;
            theme.EntityFolderID = NavMenuThemeFolderBehavior.FOLDER_ID;
            orm.Store(theme);
        }
    }

    private static NavigationMenuTheme[] BuiltInThemes() => new[]
    {
        // Decisions platform color scheme: #253167 navy, #3A85C5 blue, #A6D5F5 light blue
        new NavigationMenuTheme("NAV_MENU_THEME_DECISIONS")
        {
            EntityName             = "Decisions",
            ControlBackgroundColor = "#253167",
            SeparatorColor         = "#A6D5F5",
            SeparatorThickness     = 1,
            TopLevelStyle = new NavMenuItemStyle
            {
                BackgroundColor              = "#253167",
                HoverBackgroundColor         = "#3A85C5",
                SelectedBackgroundColor      = "#3A85C5",
                SelectedHoverBackgroundColor = "#2E72B0",
                TextColor                    = "#A6D5F5",
                HoverTextColor               = "#ffffff",
                SelectedTextColor            = "#ffffff",
                SelectedHoverTextColor       = "#ffffff",
                FontFamily                   = "Segoe UI",
                FontSize                     = 14,
                FontWeight                   = NavMenuFontWeight.Normal
            },
            SubItemStyle = new NavMenuItemStyle
            {
                BackgroundColor              = "#ffffff",
                HoverBackgroundColor         = "#EBF5FB",
                SelectedBackgroundColor      = "#D6EAF8",
                SelectedHoverBackgroundColor = "#BCD8F0",
                TextColor                    = "#253167",
                HoverTextColor               = "#3A85C5",
                SelectedTextColor            = "#253167",
                SelectedHoverTextColor       = "#3A85C5",
                FontFamily                   = "Segoe UI",
                FontSize                     = 13,
                FontWeight                   = NavMenuFontWeight.Normal
            }
        },

        // Clean neutral light theme — different font and generous spacing
        new NavigationMenuTheme("NAV_MENU_THEME_LIGHT")
        {
            EntityName             = "Light",
            ControlBackgroundColor = "#f4f4f4",
            ItemSpacing            = 8,
            TopLevelStyle = new NavMenuItemStyle
            {
                BackgroundColor              = "#f4f4f4",
                HoverBackgroundColor         = "#e0e0e0",
                SelectedBackgroundColor      = "#505050",
                SelectedHoverBackgroundColor = "#303030",
                TextColor                    = "#303030",
                HoverTextColor               = "#101010",
                SelectedTextColor            = "#ffffff",
                SelectedHoverTextColor       = "#ffffff",
                FontFamily                   = "Arial",
                FontSize                     = 15,
                FontWeight                   = NavMenuFontWeight.Normal
            },
            SubItemStyle = new NavMenuItemStyle
            {
                BackgroundColor              = "#ffffff",
                HoverBackgroundColor         = "#f9f9f9",
                SelectedBackgroundColor      = "#ececec",
                SelectedHoverBackgroundColor = "#e0e0e0",
                TextColor                    = "#404040",
                HoverTextColor               = "#101010",
                SelectedTextColor            = "#101010",
                SelectedHoverTextColor       = "#101010",
                FontFamily                   = "Arial",
                FontSize                     = 13,
                FontWeight                   = NavMenuFontWeight.Normal
            }
        },

        // True dark theme — Windows blue accent, Verdana
        new NavigationMenuTheme("NAV_MENU_THEME_DARK")
        {
            EntityName             = "Dark",
            ControlBackgroundColor = "#1e1e1e",
            SeparatorColor         = "#3d3d3d",
            SeparatorThickness     = 1,
            TopLevelStyle = new NavMenuItemStyle
            {
                BackgroundColor              = "#1e1e1e",
                HoverBackgroundColor         = "#2d2d2d",
                SelectedBackgroundColor      = "#0078d4",
                SelectedHoverBackgroundColor = "#006cbe",
                TextColor                    = "#cccccc",
                HoverTextColor               = "#ffffff",
                SelectedTextColor            = "#ffffff",
                SelectedHoverTextColor       = "#ffffff",
                FontFamily                   = "Verdana",
                FontSize                     = 13,
                FontWeight                   = NavMenuFontWeight.Normal
            },
            SubItemStyle = new NavMenuItemStyle
            {
                BackgroundColor              = "#252525",
                HoverBackgroundColor         = "#2d2d2d",
                SelectedBackgroundColor      = "#1a3a5c",
                SelectedHoverBackgroundColor = "#1e4470",
                TextColor                    = "#bbbbbb",
                HoverTextColor               = "#ffffff",
                SelectedTextColor            = "#5dafff",
                SelectedHoverTextColor       = "#5dafff",
                FontFamily                   = "Verdana",
                FontSize                     = 12,
                FontWeight                   = NavMenuFontWeight.Normal
            }
        }
    };
}
