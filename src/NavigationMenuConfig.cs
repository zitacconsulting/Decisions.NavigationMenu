using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using DecisionsFramework;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.Properties.Attributes;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Actions.Common;
using DecisionsFramework.ServiceLayer;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Utilities;

namespace Decisions.NavigationMenu;

[ORMEntity("nav_menu_config")]
[DataContract]
[Writable]
[Exportable]
[CategoryClassification(0, "Settings")]
[CategoryClassification(1, "Menu")]
[CategoryClassification(2, "Selection Bus")]
[CategoryClassification(3, "Style")]
public class NavigationMenuConfig : AbstractFolderEntity, INotifyPropertyChanged, IValidationSource
{
    [WritableValue]
    [ORMPrimaryKeyField]
#pragma warning disable CS0169
    private string id;
#pragma warning restore CS0169

    [ORMField(typeof(ORMXmlSerializedFieldConverter))]
    private NavigationMenuItemDef[] items = Array.Empty<NavigationMenuItemDef>();

    [ORMField]
    private string themeId;

    [ORMField]
    private string selectionBusName;

    public event PropertyChangedEventHandler PropertyChanged;

    [PropertyClassification(-100, "Name", new[] { "Settings" })]
    public override string EntityName
    {
        get => base.EntityName;
        set { base.EntityName = value; OnPropertyChanged(nameof(EntityName)); }
    }

    [PropertyHidden(true)]
    public override string EntityDescription
    {
        get => base.EntityDescription;
        set => base.EntityDescription = value;
    }

    [DataMember]
    [WritableValue]
    [PropertyClassification(0, "Theme", new[] { "Style" })]
    [EntityPickerEditor(new[] { typeof(NavigationMenuTheme) }, "Pick Navigation Menu Theme")]
    public string ThemeId
    {
        get => themeId;
        set { themeId = value; OnPropertyChanged(nameof(ThemeId)); }
    }

    [DataMember]
    [WritableValue]
    [PropertyClassification(0, "Menu Items", new[] { "Menu" })]
    [ArrayTypeEditor(false, false, false, disableMoveUpDown: false, ArraySortOrder.None)]
    public NavigationMenuItemDef[] Items
    {
        get
        {
            var projectId = GetProjectId();
            if (!string.IsNullOrEmpty(projectId))
                StampProjectId(items, projectId);
            return items;
        }
        set
        {
            items = value ?? Array.Empty<NavigationMenuItemDef>();
            OnPropertyChanged(nameof(Items));
        }
    }

    private static void StampProjectId(NavigationMenuItemDef[] items, string projectId)
    {
        if (items == null) return;
        foreach (var item in items)
        {
            item.ProjectId = projectId;
            StampProjectId(item.SubItems, projectId);
        }
    }

    [DataMember]
    [WritableValue]
    [PropertyClassification(0, "Selection Bus Name", new[] { "Selection Bus" })]
    public string SelectionBusName
    {
        get => selectionBusName;
        set { selectionBusName = value; OnPropertyChanged(nameof(SelectionBusName)); }
    }

    [InfoOrWarningEditor(false, null, true)]
    [PropertyClassification(1, "Selection Bus Name Info", new[] { "Selection Bus" })]
    public string SelectionBusNameNote
    {
        get => "The name of the selection bus channel shared with other components on the page. Required if any menu item has a Selection Bus Value configured.";
        set { }
    }

    public override BaseActionType[] GetActions(AbstractUserContext userContext, EntityActionType[] types)
    {
        var actions = new List<BaseActionType>(base.GetActions(userContext, types));

        actions.Add(new EditEntityAction(typeof(NavigationMenuConfig), "Edit", "Edit navigation menu configuration")
        {
            DisplayType = ActionDisplayType.Primary
        });

        actions.Add(new GetStringAction(
            "Copy", "Create a copy of this configuration", null,
            (ctx, entityId, newName) =>
            {
                if (string.IsNullOrWhiteSpace(newName)) return;
                var original = new ORM<NavigationMenuConfig>().Fetch(entityId);
                if (original == null) return;
                var copy = new NavigationMenuConfig
                {
                    EntityName       = newName,
                    EntityFolderID   = original.EntityFolderID,
                    Items            = original.Items,
                    ThemeId          = original.ThemeId,
                    SelectionBusName = original.SelectionBusName
                };
                new ORM<NavigationMenuConfig>().Store(copy);
            },
            "Copy Configuration", "Name for the copy",
            $"Copy of {EntityName}", GetTextType.ShortText)
        { EnforceNotEmptyRule = true, GroupName = "Manage" });

        actions.Add(new GetStringAction(
            "Export JSON", "Copy this configuration as JSON", "JSON",
            (ctx, entityId, _) => { },
            "Export Configuration", "JSON (copy to clipboard)",
            ToJson(), GetTextType.LongText, showCopyText: true)
        { DialogWidth = 700, DialogHeight = 500 });

        actions.Add(new GetStringAction(
            "Import JSON", "Paste JSON to replace this configuration", "JSON",
            (ctx, entityId, json) =>
            {
                var imported = FromJson(json);
                if (imported == null) return;
                Items            = imported.Items;
                ThemeId          = imported.ThemeId;
                SelectionBusName = imported.SelectionBusName;
                new ORM<NavigationMenuConfig>().Store(this);
            },
            "Import Configuration", "Paste JSON here",
            string.Empty, GetTextType.LongText)
        { DialogWidth = 700, DialogHeight = 500 });

        return actions.ToArray();
    }

    private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private string ToJson()
    {
        var dto = new NavMenuConfigDto
        {
            Items            = items,
            ThemeId          = themeId,
            SelectionBusName = selectionBusName
        };
        return JsonSerializer.Serialize(dto, _jsonOpts);
    }

    private static NavMenuConfigDto FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<NavMenuConfigDto>(json, _jsonOpts); }
        catch { return null; }
    }

    public ValidationIssue[] GetValidationIssues()
    {
        if (string.IsNullOrWhiteSpace(selectionBusName) && AnyItemHasBusValue(items))
            return new[]
            {
                new ValidationIssue(this, "Selection Bus Name is required",
                    "One or more menu items have a Selection Bus Value configured. Set a Selection Bus Name on this config.",
                    BreakLevel.Fatal, nameof(SelectionBusName))
            };
        return Array.Empty<ValidationIssue>();
    }

    private static bool AnyItemHasBusValue(NavigationMenuItemDef[] items)
    {
        if (items == null) return false;
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.SelectionBusValue)) return true;
            if (AnyItemHasBusValue(item.SubItems)) return true;
        }
        return false;
    }

    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

internal sealed class NavMenuConfigDto
{
    [JsonPropertyName("Items")]
    public NavigationMenuItemDef[] Items { get; set; }

    [JsonPropertyName("ThemeId")]
    public string ThemeId { get; set; }

    [JsonPropertyName("SelectionBusName")]
    public string SelectionBusName { get; set; }
}
