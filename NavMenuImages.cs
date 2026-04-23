using DecisionsFramework.ServiceLayer.Services.Image;

namespace Decisions.NavigationMenu;

internal static class NavMenuImages
{
    public static readonly ImageInfo RootIcon = new ImageInfo
    {
        ImageName = "suggestion_list",
        ImageType = ImageInfoType.StoredImage,
        ImageId = "behavior icons|suggestion_list.svg"
    };

    public static readonly ImageInfo ConfigIcon = new ImageInfo
    {
        ImageName = "toolbox_rules",
        ImageType = ImageInfoType.StoredImage,
        ImageId = "behavior icons|toolbox_rules.svg"
    };

    public static readonly ImageInfo ThemeIcon = new ImageInfo
    {
        ImageName = "color_picker-hover",
        ImageType = ImageInfoType.StoredImage,
        ImageId = "form control images|color_picker-hover.svg"
    };
}
