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
    [PropertyClassification(1, "Target Folder", new[] { "Navigation" })]
    [ProjectFolderPickerEditor]
    public string FolderId { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(2, "Page Name", new[] { "Navigation" })]
    [SelectStringEditor("PageNameList", SelectStringEditorType.DropdownList, true)]
    [PropertyHiddenByValue(nameof(FolderId), null, true)]
    public string PageName { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(3, "Open in New Window", new[] { "Navigation" })]
    [PropertyHiddenByValue(nameof(FolderId), null, true)]
    public bool OpenInNewWindow { get; set; }

    [PropertyHidden(true)]
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
    public string SelectionBusValue { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(5, "Action Flow", new[] { "Action" })]
    [ElementRegistrationPickerEditor(new[] { ElementType.Flow }, "DecisionsFramework.Design.Flow.FolderAwareFlowBehavior", null, false, "Pick Action Flow")]
    public string FlowId { get; set; }

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
