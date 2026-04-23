using System;
using DecisionsFramework.Design.Properties.Attributes;
using DecisionsFramework.ServiceLayer.Services.SearchFilters;

namespace Decisions.NavigationMenu;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ProjectFolderPickerEditorAttribute : FolderPickerEditorAttribute
{
    // DefaultFolderBehaviorFilter allows PublicFolderBehavior, DefaultFolderBehavior,
    // ProjectsDefaultFolderBehavior and DesignerProjectFolder — excludes management
    // sub-folders like "Flows", "Forms", "Pages", etc.
    private static readonly string[] _filterNames =
        { typeof(DefaultFolderBehaviorFilter).FullName };

    protected override string[] FilterNames => _filterNames;

    public ProjectFolderPickerEditorAttribute()
        : base("PROJECTS", "Pick Target Folder", PickerFolderVisibility.All)
    {
        // When NavigationMenuItemDef.ProjectId is stamped (IProjectAware),
        // RestrictToProject = true lets the framework scope to Project/Dependencies tabs.
        // Falls back to showing all projects for new unsaved items.
        RestrictToProject = true;
        SupportsTabs = true;
        ShowTabs = PickerTabs.ProjectDefault;
    }
}
