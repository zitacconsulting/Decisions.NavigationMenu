# Navigation Menu

> ⚠️ **Important:** Use this module at your own risk. See the **Disclaimer** section below.

A custom page component module for the [Decisions](https://decisions.com) platform that provides a fully configurable navigation menu with dropdown support, theming, selection bus integration, and permission-aware item filtering.

## Features

- **Flexible navigation** — each menu item can navigate to a target folder and page, trigger an action flow, publish a value to the selection bus, or any combination of these. For example, an item can navigate to a folder and also publish a selection bus value at the same time.
- **Dropdown sub-items** — items support nested sub-items rendered as dropdowns, with recursive permission filtering applied at every level.
- **Theming** — visual appearance is controlled by a reusable theme entity covering orientation, alignment, item spacing, background color, and independent style settings for top-level items and sub-items (colors, hover colors, selected colors, font family, size, and weight).
- **Icon support** — each item can display an icon with configurable left or right positioning.
- **Selection bus integration** — items can publish a value to a named selection bus channel, enabling coordination with other components on the same page.
- **Action flow support** — items can trigger a User Action Flow (Folder Aware) in a dialog, with full page context passed automatically.
- **Permission-aware filtering** — the menu automatically hides items the current user cannot access: items with a Target Folder require `CanView` on that folder; items with an Action Flow require `CanUse` on the flow's folder. Items that only have a Selection Bus Value configured (no folder or flow) are always visible regardless of permissions.
- **Export/Import JSON** — both menu configurations and themes support JSON export and import for easy backup and transfer between environments.

## Requirements

- Decisions 9.21 or later

## Installation

### Option 1: Install Pre-built Module
1. Download [Decisions.NavigationMenu.zip](Decisions.NavigationMenu.zip) — click the link, then click the **Download** button on the file page
2. Log into the Decisions Portal
3. Navigate to **System > Administration > Features**
4. Click **Install Module**
5. Upload the `.zip` file
6. Restart the Decisions service

> **Note:** A service restart is required for the module to load correctly.

### Option 2: Build from Source
See the [Building from Source](#building-from-source) section below.

## Configuration

### Step 1 — Create a Theme (optional)

Navigate to **Navigation Menu > Themes** and create a theme to control the visual appearance.

| Property | Description |
|---|---|
| Orientation | `Horizontal` or `Vertical` — controls the layout direction of top-level items. |
| Horizontal Justify | Alignment of items along the horizontal axis (Start, Center, End, SpaceBetween, SpaceEvenly). Only applies when Orientation is Horizontal. |
| Vertical Align | Alignment of items along the vertical axis (Top, Center, Bottom, Stretch). Only applies when Orientation is Vertical. |
| Item Spacing | Pixel gap between top-level menu items. |
| Control Background Color | Background color of the entire menu control. |
| Top-Level Item Style | Colors (background, hover, selected, selected-hover) and font settings for top-level items. |
| Sub-Item Style | Colors and font settings for dropdown sub-items. |

Themes can be exported and imported as JSON using the **Export JSON** and **Import JSON** actions on the theme entity.

### Step 2 — Create a Menu Configuration

Navigate to **Navigation Menu > Configurations** and create a configuration.

| Property | Description |
|---|---|
| Theme | The theme to apply to this menu. |
| Menu Items | The list of items to display. |
| Selection Bus Name | Required if any item uses a Selection Bus Value. Identifies the bus channel. |

Menu configurations can also be exported and imported as JSON.

### Step 3 — Configure Menu Items

Each item in the **Menu Items** list supports:

| Property | Section | Description |
|---|---|---|
| Label | Item | The display text of the item. |
| Sub Items | Dropdown | Nested items shown in a dropdown when the parent is hovered or clicked. |
| Target Folder | Navigation | Navigates to this folder when clicked. |
| Page Name | Navigation | The page within the target folder to navigate to. |
| Open in New Window | Navigation | Opens the navigation target in a new browser tab. |
| Action Flow | Action | A User Action Flow (Folder Aware) to run in a dialog when clicked. |
| Selection Bus Value | Selection Bus | Value to publish to the Selection Bus Name defined on the config. |
| Icon | Style | An image to display alongside the label. |
| Icon Position | Style | Whether the icon appears to the left or right of the label. |
| Style Override | Style | Per-item style that overrides the theme's top-level or sub-item style. |

**Target Folder, Action Flow, and Selection Bus Value are independent** — you can configure any combination on the same item. For example, an item can navigate to a folder and also run a flow, or navigate and publish a selection bus value simultaneously.

**Permission filtering** applies per field: if a Target Folder is set, the user must have `CanView` on it for the item to appear. If an Action Flow is set, the user must have `CanUse` on the flow's folder. Both conditions must pass if both are configured. Items that only have a Selection Bus Value (no folder or flow) are always shown.

### Step 4 — Add the Component to a Page

1. Open a page in the Decisions designer.
2. Add the **Navigation Menu** component from the component toolbox.
3. Select the **Menu Configuration** to use in the component's settings.

## Building from Source

### Prerequisites
- .NET 10.0 SDK or higher
- `CreateDecisionsModule` Global Tool (installed automatically during build)
- Decisions Platform SDK (NuGet package: `DecisionsSDK`)

### Build Steps

#### On Linux/macOS:
```bash
chmod +x build_module.sh
./build_module.sh
```

#### Manual Build:
```bash
# 1. Publish the main project
dotnet publish ./src/Decisions.NavigationMenu.csproj --self-contained false --output ./obj-main -c Release

# 2. Publish the Razor views project
dotnet restore ./src/Decisions.NavigationMenu.Views.csproj
dotnet publish ./src/Decisions.NavigationMenu.Views.csproj --self-contained false --output ./obj-views -c Release --no-restore

# 3. Create the module package
dotnet msbuild build.proj -t:build_module
```

### Build Output
The build creates `Decisions.NavigationMenu.zip` in the root directory. Upload it directly to Decisions via **System > Administration > Features**.

## Disclaimer

This module is provided "as is" without warranties of any kind. Use it at your own risk. The authors, maintainers, and contributors disclaim all liability for any direct, indirect, incidental, special, or consequential damages, including data loss or service interruption, arising from the use of this software.

**Important Notes:**
- Always test in a non-production environment first
- This module is not officially supported by Decisions

## License

[MIT](LICENSE)
