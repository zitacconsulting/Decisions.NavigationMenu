using DecisionsFramework.ServiceLayer.Services.Image;

namespace Decisions.NavigationMenu;

/// <summary>
/// Centralised icon definitions for all Navigation Menu folders.
///
/// In Decisions, folder icons are described by an <see cref="ImageInfo"/> object that tells
/// the platform where to find the SVG file. Using <see cref="ImageInfoType.StoredImage"/>
/// means the file lives in the server's image library rather than being a raw URL.
///
/// <para>
/// <b>ImageId format:</b> <c>"folder name|filename.svg"</c><br/>
/// The part before the pipe is a named category in the Decisions image library.
/// For example, <c>"behavior icons|suggestion_list.svg"</c> refers to the file
/// <c>suggestion_list.svg</c> inside the <c>behavior icons</c> category.
/// </para>
///
/// <para>
/// <b>Icon colour:</b> SVG icons render with their native fill colour unless a hex colour
/// string is passed via <see cref="DecisionsFramework.ServiceLayer.Services.Folder.LookAndFeel.Color"/>.
/// When a colour is supplied, the platform replaces every fill and stroke in the SVG with
/// that colour, which tints both the folder label text and the icon simultaneously.
/// </para>
///
/// <para>
/// <b>Why two Theme icons?</b> The system Themes folder uses <c>color_picker.svg</c>
/// (natively gray, matching the other system folders which have no colour override).
/// The project Themes folder uses <c>color_picker-hover.svg</c> (the coloured variant)
/// so it stands out in blue alongside the other blue project folders.
/// </para>
/// </summary>
internal static class NavMenuImages
{
    /// <summary>Icon for the system-level "Navigation Menu" root folder and its project counterpart.</summary>
    public static readonly ImageInfo RootIcon = new ImageInfo
    {
        ImageName = "suggestion_list",
        ImageType = ImageInfoType.StoredImage,
        ImageId = "behavior icons|suggestion_list.svg"
    };

    /// <summary>Icon for the system-level "Configs" folder and its project counterpart.</summary>
    public static readonly ImageInfo ConfigIcon = new ImageInfo
    {
        ImageName = "toolbox_rules",
        ImageType = ImageInfoType.StoredImage,
        ImageId = "behavior icons|toolbox_rules.svg"
    };

    /// <summary>
    /// Icon for the system-level "Themes" folder.
    /// Uses the gray (default) variant of the color picker icon so the folder
    /// renders with its native gray colour — no colour override is needed.
    /// </summary>
    public static readonly ImageInfo ThemeIcon = new ImageInfo
    {
        ImageName = "color_picker",
        ImageType = ImageInfoType.StoredImage,
        ImageId = "form control images|color_picker.svg"
    };

    /// <summary>
    /// Icon for the project-level "Themes" folder.
    /// Uses the hover (coloured) variant of the color picker icon, which renders
    /// in blue when the <c>#21659D</c> colour override is applied by
    /// <see cref="NavMenuThemeProjectFolderBehavior.GetLookAndFeel"/>.
    /// </summary>
    public static readonly ImageInfo ThemeProjectIcon = new ImageInfo
    {
        ImageName = "color_picker-hover",
        ImageType = ImageInfoType.StoredImage,
        ImageId = "form control images|color_picker-hover.svg"
    };
}
