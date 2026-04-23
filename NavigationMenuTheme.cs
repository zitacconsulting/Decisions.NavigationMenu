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
using DecisionsFramework.ServiceLayer;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Actions.Common;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Utilities;

namespace Decisions.NavigationMenu;

[ORMEntity("nav_menu_theme")]
[DataContract]
[Writable]
[Exportable]
public class NavigationMenuTheme : AbstractFolderEntity, INotifyPropertyChanged
{
    [WritableValue]
    [ORMPrimaryKeyField]
#pragma warning disable CS0169
    private string id;
#pragma warning restore CS0169

    [ORMField(typeof(ORMXmlSerializedFieldConverter))]
    private NavMenuItemStyle topLevelStyle;

    [ORMField(typeof(ORMXmlSerializedFieldConverter))]
    private NavMenuItemStyle subItemStyle;

    [ORMField]
    private string controlBackgroundColor;

    [ORMField]
    private NavMenuOrientation orientation = NavMenuOrientation.Horizontal;

    [ORMField]
    private NavMenuHorizontalJustify horizontalJustify = NavMenuHorizontalJustify.Start;

    [ORMField]
    private NavMenuVerticalAlign verticalAlign = NavMenuVerticalAlign.Top;

    [ORMField]
    private int itemSpacing = 0;

    public NavigationMenuTheme()
    {
        controlBackgroundColor = "#1c1e2e";

        topLevelStyle = new NavMenuItemStyle
        {
            BackgroundColor              = "#1c1e2e",
            HoverBackgroundColor         = "#2a2d42",
            SelectedBackgroundColor      = "#3b4fd8",
            SelectedHoverBackgroundColor = "#4f63e7",
            TextColor                    = "#c8cce8",
            HoverTextColor               = "#ffffff",
            SelectedTextColor            = "#ffffff",
            SelectedHoverTextColor       = "#ffffff",
            FontFamily                   = "Segoe UI",
            FontSize                     = 14,
            FontWeight                   = NavMenuFontWeight.Normal
        };

        subItemStyle = new NavMenuItemStyle
        {
            BackgroundColor              = "#ffffff",
            HoverBackgroundColor         = "#f0f2ff",
            SelectedBackgroundColor      = "#eaedff",
            SelectedHoverBackgroundColor = "#dde1ff",
            TextColor                    = "#2d2f45",
            HoverTextColor               = "#3b4fd8",
            SelectedTextColor            = "#2c3cc9",
            SelectedHoverTextColor       = "#2c3cc9",
            FontFamily                   = "Segoe UI",
            FontSize                     = 13,
            FontWeight                   = NavMenuFontWeight.Normal
        };
    }

    public event PropertyChangedEventHandler PropertyChanged;

    [PropertyClassification(-100, "Name", new[] { "Settings" })]
    public override string EntityName
    {
        get => base.EntityName;
        set { base.EntityName = value; OnPropertyChanged(nameof(EntityName)); }
    }

    [DataMember]
    [WritableValue]
    [PropertyClassification(0, "Control Background Color", new[] { "Style" })]
    [ColorPickerEditor(true, true)]
    public string ControlBackgroundColor
    {
        get => controlBackgroundColor;
        set { controlBackgroundColor = value; OnPropertyChanged(nameof(ControlBackgroundColor)); }
    }

    [DataMember]
    [WritableValue]
    [PropertyClassification(1, "Top-Level Item Style", new[] { "Style" })]
    public NavMenuItemStyle TopLevelStyle
    {
        get => topLevelStyle;
        set { topLevelStyle = value; OnPropertyChanged(nameof(TopLevelStyle)); }
    }

    [DataMember]
    [WritableValue]
    [PropertyClassification(2, "Sub-Item Style", new[] { "Style" })]
    public NavMenuItemStyle SubItemStyle
    {
        get => subItemStyle;
        set { subItemStyle = value; OnPropertyChanged(nameof(SubItemStyle)); }
    }

    [DataMember]
    [WritableValue]
    [PropertyClassification(0, "Orientation", new[] { "Layout" })]
    public NavMenuOrientation Orientation
    {
        get => orientation;
        set { orientation = value; OnPropertyChanged(nameof(Orientation)); }
    }

    [DataMember]
    [WritableValue]
    [PropertyClassification(1, "Horizontal Justify", new[] { "Layout" })]
    [PropertyHiddenByValue(nameof(Orientation), NavMenuOrientation.Horizontal, false)]
    public NavMenuHorizontalJustify HorizontalJustify
    {
        get => horizontalJustify;
        set { horizontalJustify = value; OnPropertyChanged(nameof(HorizontalJustify)); }
    }

    [DataMember]
    [WritableValue]
    [PropertyClassification(2, "Vertical Align", new[] { "Layout" })]
    [PropertyHiddenByValue(nameof(Orientation), NavMenuOrientation.Vertical, false)]
    public NavMenuVerticalAlign VerticalAlign
    {
        get => verticalAlign;
        set { verticalAlign = value; OnPropertyChanged(nameof(VerticalAlign)); }
    }

    [DataMember]
    [WritableValue]
    [PropertyClassification(3, "Item Spacing (px)", new[] { "Layout" })]
    public int ItemSpacing
    {
        get => itemSpacing;
        set { itemSpacing = value; OnPropertyChanged(nameof(ItemSpacing)); }
    }

    public override BaseActionType[] GetActions(AbstractUserContext userContext, EntityActionType[] types)
    {
        var actions = new List<BaseActionType>(base.GetActions(userContext, types));

        actions.Add(new EditEntityAction(typeof(NavigationMenuTheme), "Edit", "Edit navigation menu theme")
        {
            DisplayType = ActionDisplayType.Primary
        });

        actions.Add(new GetStringAction(
            "Export JSON", "Copy this theme as JSON", "JSON",
            (ctx, entityId, _) => { },
            "Export Theme", "JSON (copy to clipboard)",
            ToJson(), GetTextType.LongText, showCopyText: true)
        { DialogWidth = 700, DialogHeight = 500 });

        actions.Add(new GetStringAction(
            "Import JSON", "Paste JSON to replace this theme", "JSON",
            (ctx, entityId, json) =>
            {
                var imported = FromJson(json);
                if (imported == null) return;
                TopLevelStyle          = imported.TopLevelStyle;
                SubItemStyle           = imported.SubItemStyle;
                ControlBackgroundColor = imported.ControlBackgroundColor;
                Orientation            = imported.Orientation;
                HorizontalJustify      = imported.HorizontalJustify;
                VerticalAlign          = imported.VerticalAlign;
                ItemSpacing            = imported.ItemSpacing;
                new ORM<NavigationMenuTheme>().Store(this);
            },
            "Import Theme", "Paste JSON here",
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
        var dto = new NavMenuThemeDto
        {
            TopLevelStyle          = topLevelStyle,
            SubItemStyle           = subItemStyle,
            ControlBackgroundColor = controlBackgroundColor,
            Orientation            = orientation,
            HorizontalJustify      = horizontalJustify,
            VerticalAlign          = verticalAlign,
            ItemSpacing            = itemSpacing
        };
        return JsonSerializer.Serialize(dto, _jsonOpts);
    }

    private static NavMenuThemeDto FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<NavMenuThemeDto>(json, _jsonOpts); }
        catch { return null; }
    }

    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

internal sealed class NavMenuThemeDto
{
    [JsonPropertyName("TopLevelStyle")]
    public NavMenuItemStyle TopLevelStyle { get; set; }

    [JsonPropertyName("SubItemStyle")]
    public NavMenuItemStyle SubItemStyle { get; set; }

    [JsonPropertyName("ControlBackgroundColor")]
    public string ControlBackgroundColor { get; set; }

    [JsonPropertyName("Orientation")]
    public NavMenuOrientation Orientation { get; set; }

    [JsonPropertyName("HorizontalJustify")]
    public NavMenuHorizontalJustify HorizontalJustify { get; set; }

    [JsonPropertyName("VerticalAlign")]
    public NavMenuVerticalAlign VerticalAlign { get; set; }

    [JsonPropertyName("ItemSpacing")]
    public int ItemSpacing { get; set; }
}
