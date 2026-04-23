using System;
using System.Linq;
using Decisions.Silverlight.UI.Controls;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.ServiceLayer.Services.ConfigurationStorage;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.Properties.Attributes;
using DecisionsFramework.ServiceLayer.Services.Accounts;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Utilities;
using DecisionsFramework.Utilities;

namespace Decisions.NavigationMenu;

public class NavigationMenuPart : SilverPart
{
    private string menuConfigId;

    [WritableValue]
    [PropertyClassification(0, "Menu Configuration", new[] { "Settings" })]
    [EntityPickerEditorAttribute(new[] { typeof(NavigationMenuConfig) }, NavMenuConfigFolderBehavior.FOLDER_ID, "Pick Navigation Menu Config")]
    public string MenuConfigId
    {
        get => menuConfigId;
        set { menuConfigId = value; OnPropertyChanged(nameof(MenuConfigId)); }
    }

    public NavigationMenuPart()
    {
        Title = "Navigation Menu";
    }

    private NavigationMenuConfig FetchConfig()
    {
        if (string.IsNullOrEmpty(menuConfigId)) return null;
        return new ORM<NavigationMenuConfig>().Fetch(menuConfigId);
    }

    public NavigationMenuItemDef[] GetItems()
    {
        var config = FetchConfig();
        if (config == null) return Array.Empty<NavigationMenuItemDef>();

        var account = UserContextHolder.GetCurrent()?.GetAccount();
        return FilterByAccess(config.Items, account);
    }

    private static NavigationMenuItemDef[] FilterByAccess(NavigationMenuItemDef[] items, Account account)
    {
        if (items == null) return Array.Empty<NavigationMenuItemDef>();
        return items
            .Where(item =>
                (string.IsNullOrEmpty(item.FolderId)
                    || account == null
                    || FolderPermissionHelper.VerifyAccountPermission(item.FolderId, account, FolderPermission.CanView, throwExceptionOnMissingFolder: false))
                && (string.IsNullOrEmpty(item.FlowId)
                    || account == null
                    || HasFlowAccess(item.FlowId, account)))
            .Select(item => new NavigationMenuItemDef
            {
                Label             = item.Label,
                OpenUrl           = item.OpenUrl,
                Url               = item.Url,
                OpenUrlInNewPage  = item.OpenUrlInNewPage,
                FolderId          = item.FolderId,
                PageName          = item.PageName,
                OpenInNewWindow   = item.OpenInNewWindow,
                HidePortal        = item.HidePortal,
                SelectionBusValue = item.SelectionBusValue,
                FlowId            = item.FlowId,
                Icon              = item.Icon,
                IconPosition      = item.IconPosition,
                SeparatorAfter    = item.SeparatorAfter,
                SubItems          = FilterByAccess(item.SubItems, account)
            })
            .ToArray();
    }

    public (NavMenuItemStyle topLevel, NavMenuItemStyle subItem, string controlBg, string separatorColor, int separatorThickness) GetStyles()
    {
        var config = FetchConfig();
        if (config == null || string.IsNullOrEmpty(config.ThemeId))
            return (null, null, null, null, 1);
        var theme = new ORM<NavigationMenuTheme>().Fetch(config.ThemeId);
        return (theme?.TopLevelStyle, theme?.SubItemStyle, theme?.ControlBackgroundColor, theme?.SeparatorColor, theme?.SeparatorThickness ?? 1);
    }

    private static bool HasFlowAccess(string flowId, Account account)
    {
        var reg = ElementRegistrationUtils.Fetch(flowId);
        if (reg == null) return false;
        return FolderPermissionHelper.VerifyAccountPermission(reg.EntityFolderID, account, FolderPermission.CanUse, throwExceptionOnMissingFolder: false);
    }

    public (NavMenuOrientation orientation, NavMenuHorizontalJustify horizontalJustify, NavMenuVerticalAlign verticalAlign, int itemSpacing, bool enableHighlighting, string selectionBusName) GetLayout()
    {
        var config = FetchConfig();
        if (config == null)
            return (NavMenuOrientation.Horizontal, NavMenuHorizontalJustify.Start, NavMenuVerticalAlign.Top, 0, true, null);

        NavigationMenuTheme theme = null;
        if (!string.IsNullOrEmpty(config.ThemeId))
            theme = new ORM<NavigationMenuTheme>().Fetch(config.ThemeId);

        return (
            theme?.Orientation       ?? NavMenuOrientation.Horizontal,
            theme?.HorizontalJustify ?? NavMenuHorizontalJustify.Start,
            theme?.VerticalAlign     ?? NavMenuVerticalAlign.Top,
            theme?.ItemSpacing       ?? 0,
            true,
            config.SelectionBusName
        );
    }
}

public enum NavMenuOrientation
{
    Horizontal,
    Vertical
}
