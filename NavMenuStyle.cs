using System.Runtime.Serialization;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.Properties.Attributes;

namespace Decisions.NavigationMenu;

public enum NavMenuFontWeight
{
    Normal,
    Bold,
    Light
}

public enum NavMenuHorizontalJustify
{
    Start,
    Center,
    End,
    SpaceBetween,
    SpaceEvenly
}

public enum NavMenuVerticalAlign
{
    Top,
    Center,
    Bottom,
    Stretch
}

[Writable]
[DataContract]
public class NavMenuItemStyle
{
    [WritableValue]
    [DataMember]
    [PropertyClassification(0, "Background Color", new[] { "Colors" })]
    [ColorPickerEditor(true, true)]
    public string BackgroundColor { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(1, "Hover Background Color", new[] { "Colors" })]
    [ColorPickerEditor(true, true)]
    public string HoverBackgroundColor { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(2, "Selected Background Color", new[] { "Colors" })]
    [ColorPickerEditor(true, true)]
    public string SelectedBackgroundColor { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(3, "Selected Hover Background Color", new[] { "Colors" })]
    [ColorPickerEditor(true, true)]
    public string SelectedHoverBackgroundColor { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(4, "Text Color", new[] { "Colors" })]
    [ColorPickerEditor(true, true)]
    public string TextColor { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(5, "Hover Text Color", new[] { "Colors" })]
    [ColorPickerEditor(true, true)]
    public string HoverTextColor { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(6, "Selected Text Color", new[] { "Colors" })]
    [ColorPickerEditor(true, true)]
    public string SelectedTextColor { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(7, "Selected Hover Text Color", new[] { "Colors" })]
    [ColorPickerEditor(true, true)]
    public string SelectedHoverTextColor { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(10, "Font Family", new[] { "Font" })]
    [SelectStringEditor("FontFamilyList", SelectStringEditorType.DropdownList, true)]
    public string FontFamily { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(11, "Font Size (px)", new[] { "Font" })]
    public int FontSize { get; set; }

    [WritableValue]
    [DataMember]
    [PropertyClassification(12, "Font Weight", new[] { "Font" })]
    public NavMenuFontWeight FontWeight { get; set; }

    [PropertyHidden]
    [System.Text.Json.Serialization.JsonIgnore]
    public string[] FontFamilyList => new[]
    {
        "Arial", "Arial Black", "Comic Sans MS", "Courier New", "Georgia",
        "Impact", "Lucida Console", "Lucida Sans Unicode", "Palatino Linotype",
        "Tahoma", "Times New Roman", "Trebuchet MS", "Verdana",
        "Helvetica", "Open Sans", "Roboto", "Segoe UI"
    };

    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrEmpty(BackgroundColor)) parts.Add($"BG: {BackgroundColor}");
        if (!string.IsNullOrEmpty(TextColor)) parts.Add($"Text: {TextColor}");
        var font = string.IsNullOrEmpty(FontFamily) ? "" : FontFamily;
        if (FontSize > 0) font += $" {FontSize}px";
        font = font.Trim();
        if (!string.IsNullOrEmpty(font)) parts.Add(font);
        return parts.Count > 0 ? string.Join(" | ", parts) : "(default)";
    }
}
