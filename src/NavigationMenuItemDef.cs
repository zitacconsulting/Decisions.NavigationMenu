using System;
using System.Linq;
using System.Runtime.Serialization;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.Properties.Attributes;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Services.ConfigurationStorage;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Services.Image;
using DecisionsFramework.ServiceLayer.Services.Projects;
using DecisionsFramework.ServiceLayer.Utilities;
using DecisionsFramework.Utilities;

namespace Decisions.NavigationMenu;

public enum NavMenuIconPosition { Left, Right }

[Writable]
[DataContract]
[CategoryClassification(0, "Item")]
[CategoryClassification(1, "Dropdown")]
[CategoryClassification(2, "Navigation")]
[CategoryClassification(3, "Action")]
[CategoryClassification(4, "Selection Bus")]
[CategoryClassification(5, "Style")]
public class NavigationMenuItemDef : IProjectAware
{
    [PropertyHidden(true)]
    public string ProjectId { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(0, "Label", new[] { "Item" })]
    public string Label { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(1, "Separator After", new[] { "Item" })]
    public bool SeparatorAfter { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(0, "Open URL", new[] { "Navigation" })]
    public bool OpenUrl { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(1, "URL", new[] { "Navigation" })]
    [PropertyHiddenByValue(nameof(OpenUrl), false, true)]
    public string Url { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(2, "Open In New Page", new[] { "Navigation" })]
    [PropertyHiddenByValue(nameof(OpenUrl), false, true)]
    public bool OpenUrlInNewPage { get; set; } = true;

    [WritableValue]
    [DataMember]
    [PropertyClassification(3, "Target Folder", new[] { "Navigation" })]
    [ProjectFolderPickerEditor]
    [PropertyHiddenByValue(nameof(OpenUrl), true, true)]
    public string FolderId { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(4, "Page Name", new[] { "Navigation" })]
    [SelectStringEditor("PageNameList", SelectStringEditorType.DropdownList, true)]
    [PropertyHiddenByValue(nameof(FolderId), null, true)]
    [PropertyHiddenByValue(nameof(OpenUrl), true, true)]
    public string PageName { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(5, "Open in New Window", new[] { "Navigation" })]
    [PropertyHiddenByValue(nameof(FolderId), null, true)]
    [PropertyHiddenByValue(nameof(OpenUrl), true, true)]
    public bool OpenInNewWindow { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(6, "Hide Portal", new[] { "Navigation" })]
    [PropertyHiddenByValue(nameof(OpenInNewWindow), false, true)]
    [PropertyHiddenByValue(nameof(OpenUrl), true, true)]
    public bool HidePortal { get; set; }

    [PropertyHidden(true)]
    [System.Text.Json.Serialization.JsonIgnore]
    public string[] PageNameList
    {
        get
        {
            if (!string.IsNullOrEmpty(FolderId) && FolderService.Instance.Exists(UserContextHolder.GetCurrent(), FolderId))
            {
                var viewData = FolderService.Instance.GetFolderViewData(UserContextHolder.GetCurrent(), FolderId, new EntityActionType[0]);
                if (viewData != null && ArrayUtilities.IsNotEmpty(viewData.Pages))
                    return viewData.Pages.Select(p => p.ViewPageName).ToArray();
            }
            return Array.Empty<string>();
        }
    }

    [WritableValue]
    [DataMember]
    [PropertyClassification(4, "Selection Bus Value", new[] { "Selection Bus" })]
    [PropertyHiddenByValue(nameof(OpenUrl), true, true)]
    public string SelectionBusValue { get; set; }

    [InfoOrWarningEditor(false, null, true)]
    [PropertyClassification(5, "Selection Bus Value Info", new[] { "Selection Bus" })]
    [PropertyHiddenByValue(nameof(OpenUrl), true, true)]
    [System.Text.Json.Serialization.JsonIgnore]
    public string SelectionBusValueNote
    {
        get => "Requires a Selection Bus Name to be configured on the menu configuration.";
        set { }
    }

    [WritableValue]
    [DataMember]
    [PropertyClassification(5, "Action Flow", new[] { "Action" })]
    [ElementRegistrationPickerEditor(new[] { ElementType.Flow }, "DecisionsFramework.Design.Flow.FolderAwareFlowBehavior", null, false, "Pick Action Flow")]
    [PropertyHiddenByValue(nameof(OpenUrl), true, true)]
    public string FlowId { get; set; }

    [InfoOrWarningEditor(false, null, true)]
    [PropertyClassification(6, "Action Flow Info", new[] { "Action" })]
    [PropertyHiddenByValue(nameof(OpenUrl), true, true)]
    [System.Text.Json.Serialization.JsonIgnore]
    public string FlowNote
    {
        get => "Must be of type User Action Flow (Folder Aware). When Open in New Window is enabled, the flow runs automatically on the new page after it loads.";
        set { }
    }

    [WritableValue]
    [DataMember]
    [PropertyClassification(6, "Icon", new[] { "Style" })]
    public ImageInfo Icon { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(7, "Icon Position", new[] { "Style" })]
    public NavMenuIconPosition IconPosition { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(8, "Sub Items", new[] { "Dropdown" })]
    [ArrayTypeEditor(false, false, false, disableMoveUpDown: false, ArraySortOrder.None)]
    public NavigationMenuItemDef[] SubItems
    {
        get
        {
            if (!string.IsNullOrEmpty(ProjectId) && _subItems != null)
                foreach (var item in _subItems)
                {
                    item.ProjectId = ProjectId;
                }
            return _subItems;
        }
        set => _subItems = value ?? Array.Empty<NavigationMenuItemDef>();
    }

    private NavigationMenuItemDef[] _subItems = Array.Empty<NavigationMenuItemDef>();

    [WritableValue]
    [DataMember]
    [PropertyClassification(9, "Style Override", new[] { "Style" })]
    public NavMenuItemStyle StyleOverride { get; set; }

    public override string ToString()
    {
        var label = string.IsNullOrEmpty(Label) ? "(no label)" : Label;
        var count = SubItems?.Length ?? 0;
        return count > 0 ? $"{label} ({count} sub-item{(count == 1 ? "" : "s")})" : label;
    }
}
