// $DP is the global Decisions namespace. These lines ensure the namespace objects exist
// before any code tries to attach properties to them.
var $DP = $DP || {};
$DP.Components = $DP.Components || {};
$DP.Components.Page = $DP.Components.Page || {};

// ── Platform bug workaround: color picker does not pre-select the current color ─────────
//
// When a user edits a color property in the React property grid and then re-opens the
// color picker to change it again, the previously selected color should be pre-selected
// in the picker. However, the platform passes the color as a plain hex string (e.g.
// "#ff0000"), while ColorPicker internally expects a DesignerColor object with a
// Type === SolidColor (0) property to know which tab to activate on open.
//
// Because a raw string never satisfies that condition, the picker always opened showing
// no pre-selected color, forcing the user to re-enter it from scratch.
//
// Fix: we replace the platform's ColorPicker class with a subclass that intercepts the
// constructor, detects when pickedColor is a plain string, and converts it to a proper
// DesignerColor via RGBColor.toDesignerColor() before passing it to the original class.
//
// We use "class extends" (ES6 class syntax) rather than the older prototype-delegation
// pattern because ColorPicker is compiled from TypeScript as a native ES6 class.
// ES6 class constructors cannot be called without "new" — the old pattern of doing
// Original.call(this, options) would throw "Class constructors cannot be invoked
// without 'new'" at runtime.
//
// The guard at the top skips the patch if the platform classes are not yet loaded,
// which avoids errors if the script load order ever changes.
(function () {
    if (typeof $DP.ColorDialogEditor === 'undefined' ||
        typeof $DP.ColorDialogEditor.ColorPicker !== 'function' ||
        typeof $DP.ColorDialogEditor.RGBColor !== 'function') {
        return;
    }
    var Original = $DP.ColorDialogEditor.ColorPicker;
    var PatchedColorPicker = class extends Original {
        constructor(options) {
            if (options && typeof options.pickedColor === 'string' && options.pickedColor) {
                options.pickedColor = new $DP.ColorDialogEditor.RGBColor(options.pickedColor).toDesignerColor();
            }
            super(options);
        }
    };
    $DP.ColorDialogEditor.ColorPicker = PatchedColorPicker;
}());

// ── NavMenu component ────────────────────────────────────────────────────────────────────
//
// This is a self-contained IIFE (Immediately Invoked Function Expression). The IIFE
// pattern creates a private scope so internal helper functions and variables are not
// accessible from outside. Only the object returned at the bottom (initialize,
// _updateActiveItem) becomes part of the public API attached to $DP.Components.Page.NavMenu.
//
// The component is initialised once per control instance on a page. Multiple NavMenu
// controls can coexist on the same page — each gets its own entry in _instances keyed
// by instanceId.
$DP.Components.Page.NavMenu = (function () {
    'use strict';

    // Map of instanceId → options for every NavMenu control currently on the page.
    var _instances = {};

    // Stores the SelectionBusValue of the last clicked item so it survives a
    // "navigationParameterChanged" event that resets the URL parameters.
    // Without this, clicking an item, navigating, and then coming back would lose
    // which bus value was active.
    var _pendingBusValue = null;

    // ── Public API ──────────────────────────────────────────────────────────

    /**
     * Called once by the Decisions form control framework when the NavMenu control is
     * placed on a page. Sets up the DOM, wires event listeners, and handles auto-run.
     *
     * @param {object} opts - Configuration object passed from the C# control renderer.
     *   Key properties:
     *   - instanceId {string}     Unique ID for this control instance on the page.
     *   - holder {jQuery}         The jQuery-wrapped container element for this control.
     *   - items {Array}           The nav items to render (recursive, supports SubItems).
     *   - isDesignMode {boolean}  True when the user is editing the page in the designer.
     *                             Interactions are disabled in design mode.
     *   - selectionBusName {string}  Name of the Decisions navigation parameter ("bus")
     *                             used to communicate the selected item to other controls.
     *   - orientation {string}    'Horizontal' or 'Vertical'.
     *   - topLevelStyle / subItemStyle {object}  Visual style overrides from the config.
     */
    function initialize(opts) {
        _instances[opts.instanceId] = opts;
        _injectStyles(opts.instanceId);
        _render(opts.instanceId);

        // In design mode we render the control but do not attach click handlers or
        // listen for navigation events — the user is editing, not using, the page.
        if (opts.isDesignMode) return;

        // "navigationParameterChanged" is a Decisions platform event fired whenever the
        // URL navigation parameters change (e.g. FolderId, pageName, or any bus value).
        // We namespace the event with the instanceId so we can cleanly remove it later
        // without accidentally removing listeners from other NavMenu instances.
        var ns = '.navmenu-' + opts.instanceId;
        $(document).on('navigationParameterChanged' + ns, function () {
            _updateActiveItem(opts.instanceId);
        });
        _updateActiveItem(opts.instanceId);

        // Auto-run: if the page URL contains ?autoRunFlowId=<id>, run that flow once
        // on page load. This allows menu items to trigger a flow automatically when
        // navigating to a folder from a new-window link.
        //
        // _dpNavMenuAutoRunDone prevents multiple NavMenu instances on the same page
        // from each triggering the same flow.
        //
        // sessionStorage prevents the flow from running again if the user refreshes the
        // page while still on the same flow URL (browser back/forward etc.).
        if (!window._dpNavMenuAutoRunDone) {
            var autoFlowId = new URLSearchParams(window.location.search).get('autoRunFlowId');
            if (autoFlowId) {
                window._dpNavMenuAutoRunDone = true;
                var storageKey = 'dpNavMenuAutoRun_' + autoFlowId;
                if (!sessionStorage.getItem(storageKey)) {
                    sessionStorage.setItem(storageKey, '1');
                    // setTimeout(0) defers execution until after the page has fully
                    // initialised all controls, avoiding race conditions.
                    setTimeout(function () { _runFlow(autoFlowId, null, null); }, 0);
                }
            }
        }
    }

    // ── Active item highlighting ─────────────────────────────────────────────
    //
    // The "active" item is the menu entry that corresponds to the page the user is
    // currently on. We highlight it (and its top-level ancestor if it is nested) using
    // the CSS class dp-navmenu-active, which is styled in navmenu.css.

    /**
     * Reads the current folder and page from the URL query string.
     * Decisions navigation always uses FolderId (and optionally pageName) in the URL.
     * Returns null when we are not inside a Decisions folder page.
     */
    function _getCurrentLocation() {
        var params = new URLSearchParams(window.location.search);
        // Accept both capitalisation variants — Decisions is inconsistent across versions.
        var folderId = params.get('FolderId') || params.get('folderId');
        var pageName = params.get('pageName') || params.get('PageName');
        if (folderId) return { folderId: folderId, pageName: pageName };
        return null;
    }

    /**
     * Writes the current bus value back into the URL using history.replaceState so that
     * sharing or bookmarking the URL preserves the selected bus value without adding a
     * new browser history entry (replaceState vs pushState).
     */
    function _syncBusToUrl(busName, busValue) {
        if (!busName) return;
        var params = new URLSearchParams(window.location.search);
        if (busValue !== null && busValue !== undefined && busValue !== '') {
            params.set(busName, busValue);
        } else {
            params.delete(busName);
        }
        history.replaceState(null, '', window.location.pathname + '?' + params.toString());
    }

    /**
     * Recalculates which menu item matches the current page and updates the
     * dp-navmenu-active CSS class accordingly.
     *
     * Bus values: a "selection bus" is a Decisions navigation parameter used to pass
     * a value between controls on the same page (similar to a query string variable that
     * updates without a full page reload). When a NavMenu item has a SelectionBusValue,
     * clicking it publishes that value to the bus, and other controls react to it.
     */
    function _updateActiveItem(instanceId) {
        var opts = _instances[instanceId];
        if (!opts) return;

        opts.holder.find('.dp-navmenu-item').removeClass('dp-navmenu-active');

        var loc = _getCurrentLocation();
        var busName = opts.selectionBusName || null;

        // Read the current bus value from three sources in priority order:
        // 1. _pendingBusValue — set synchronously when the user clicks an item,
        //    before the navigation event has fired and updated the URL.
        // 2. The live navigation parameters object maintained by the Decisions platform.
        // 3. The URL query string (fallback for page load / direct link scenarios).
        var liveParams = ($DP.Navigation && $DP.Navigation.Helper && $DP.Navigation.Helper.navigationParameters) || {};
        var busValue = _pendingBusValue !== null ? _pendingBusValue
                     : (busName && liveParams[busName] != null) ? String(liveParams[busName])
                     : (busName ? (new URLSearchParams(window.location.search)).get(busName) : null);

        var $match = _findBestMatch(opts.holder, loc, busName, busValue);

        _syncBusToUrl(busName, busValue);

        if (!$match) {
            _updateIconStates(instanceId);
            return;
        }

        $match.addClass('dp-navmenu-active');

        // If the matched item is nested inside a dropdown, also highlight the top-level
        // parent so users can see which section is active even when the dropdown is closed.
        var $top = _getTopLevelParent($match, opts.holder);
        if ($top && $top[0] !== $match[0]) {
            $top.addClass('dp-navmenu-active');
        }

        _updateIconStates(instanceId);
    }

    /**
     * Finds the best-matching menu item for the current location and bus value.
     *
     * Scoring: a folder ID match scores 1. If the item also matches the bus value,
     * the score becomes 2. This means that when multiple items point to the same folder
     * (e.g. one with a bus value and one without), the more specific match wins.
     */
    function _findBestMatch($holder, loc, busName, busValue) {
        var $best = null;
        var bestScore = -1;

        $holder.find('.dp-navmenu-item').each(function () {
            var $li = $(this);
            var folderId     = $li.attr('data-folder-id') || null;
            var pageName     = $li.attr('data-page-name') || null;
            var itemBusValue = $li.attr('data-bus-value') || null;

            if (!folderId || !loc || folderId !== loc.folderId) return;
            if (pageName && loc.pageName && pageName !== loc.pageName) return;

            var score = 1;
            if (busName && itemBusValue !== null && busValue !== null &&
                String(busValue) === String(itemBusValue)) {
                score = 2;
            }

            if (score > bestScore) {
                bestScore = score;
                $best = $li;
            }
        });

        return $best;
    }

    /**
     * Walks up the DOM from a nested menu item to find the top-level list item.
     * Used to highlight the parent item when a child item is active.
     */
    function _getTopLevelParent($li, $holder) {
        var $topList = $holder.find('> nav > .dp-navmenu-list');
        if ($li.parent().is($topList)) return $li;
        var $current = $li;
        while (true) {
            var $parentLi = $current.closest('.dp-navmenu-dropdown').closest('.dp-navmenu-item');
            if (!$parentLi.length) break;
            $current = $parentLi;
            if ($current.parent().is($topList)) break;
        }
        return $current;
    }

    // ── Style injection ──────────────────────────────────────────────────────
    //
    // Visual styles (colours, fonts, spacing) come from the NavMenu config object.
    // Rather than inline-styling every element, we generate a scoped <style> block
    // and inject it into <head> once per instance. Scoping by instanceId means
    // multiple NavMenu controls on the same page do not interfere with each other.

    function _injectStyles(instanceId) {
        var opts = _instances[instanceId];
        var id = 'dp-navmenu-style-' + instanceId;
        // Guard: only inject once even if initialize is somehow called twice.
        if (document.getElementById(id)) return;

        var top = opts.topLevelStyle || {};
        var sub = opts.subItemStyle || {};

        var css = _buildScopedCss('#navmenu-holder-' + instanceId, top, sub, opts.itemSpacing || 0, opts.controlBackgroundColor || '', opts.separatorColor || '', opts.separatorThickness || 1);
        if (!css) return;

        var $style = $('<style>').attr('id', id).text(css);
        $('head').append($style);
    }

    function _buildScopedCss(scope, top, sub, itemSpacing, controlBg, separatorColor, separatorThickness) {
        var rules = '';

        if (controlBg) {
            rules += scope + ' > nav{background-color:' + controlBg + ';}';
        }
        if (itemSpacing > 0) {
            rules += scope + ' > nav > .dp-navmenu-list{gap:' + itemSpacing + 'px;}';
        }

        if (sub && sub.BackgroundColor) {
            rules += scope + ' .dp-navmenu-dropdown{background-color:' + sub.BackgroundColor + ';}';
        }

        var sepColor = separatorColor || 'rgba(128,128,128,0.25)';
        var sepWidth = (separatorThickness > 0 ? separatorThickness : 1) + 'px';
        rules += scope + ' > nav.dp-navmenu-horizontal > .dp-navmenu-list > .dp-navmenu-separator{border-left:' + sepWidth + ' solid ' + sepColor + ';}';
        rules += scope + ' > nav.dp-navmenu-vertical > .dp-navmenu-list > .dp-navmenu-separator{border-top:' + sepWidth + ' solid ' + sepColor + ';}';
        rules += scope + ' .dp-navmenu-dropdown > .dp-navmenu-separator{border-top:' + sepWidth + ' solid ' + sepColor + ';}';

        rules += _itemRules(scope + ' > nav > ul > .dp-navmenu-item > .dp-navmenu-link', top);
        rules += _itemRules(scope + ' .dp-navmenu-dropdown .dp-navmenu-item > .dp-navmenu-link', sub);

        if (top.FontFamily || top.FontSize || top.FontWeight) {
            rules += _fontRules(scope + ' > nav > ul > .dp-navmenu-item > .dp-navmenu-link', top);
        }
        if (sub.FontFamily || sub.FontSize || sub.FontWeight) {
            rules += _fontRules(scope + ' .dp-navmenu-dropdown .dp-navmenu-item > .dp-navmenu-link', sub);
        }

        return rules;
    }

    function _itemRules(sel, s) {
        var css = '';
        if (!s) return css;

        var base = '';
        if (s.BackgroundColor) base += 'background-color:' + s.BackgroundColor + ';';
        if (s.TextColor) base += 'color:' + s.TextColor + ';';
        if (base) css += sel + '{' + base + '}';

        var hover = '';
        if (s.HoverBackgroundColor) hover += 'background-color:' + s.HoverBackgroundColor + ';';
        if (s.HoverTextColor) hover += 'color:' + s.HoverTextColor + ';';
        if (hover) css += sel + ':hover{' + hover + '}';

        var sel2 = sel.replace(' > .dp-navmenu-link', '');
        var active = '';
        if (s.SelectedBackgroundColor) active += 'background-color:' + s.SelectedBackgroundColor + ';';
        if (s.SelectedTextColor) active += 'color:' + s.SelectedTextColor + ';';
        if (active) css += sel2 + '.dp-navmenu-active > .dp-navmenu-link{' + active + '}';

        var activeHover = '';
        if (s.SelectedHoverBackgroundColor) activeHover += 'background-color:' + s.SelectedHoverBackgroundColor + ';';
        if (s.SelectedHoverTextColor) activeHover += 'color:' + s.SelectedHoverTextColor + ';';
        if (activeHover) css += sel2 + '.dp-navmenu-active > .dp-navmenu-link:hover{' + activeHover + '}';

        return css;
    }

    function _fontRules(sel, s) {
        if (!s) return '';
        var css = '';
        var font = '';
        if (s.FontFamily) font += 'font-family:' + s.FontFamily + ',sans-serif;';
        if (s.FontSize && s.FontSize > 0) font += 'font-size:' + s.FontSize + 'px;';
        if (s.FontWeight) {
            var w = s.FontWeight === 'Bold' ? 'bold' : s.FontWeight === 'Light' ? '300' : 'normal';
            font += 'font-weight:' + w + ';';
        }
        if (font) css += sel + '{' + font + '}';
        return css;
    }

    function _itemInlineStyle(style, isActive, isHover) {
        if (!style) return '';
        var css = '';
        var bg = isActive
            ? (isHover ? style.SelectedHoverBackgroundColor : style.SelectedBackgroundColor)
            : (isHover ? style.HoverBackgroundColor : style.BackgroundColor);
        var color = isActive
            ? (isHover ? style.SelectedHoverTextColor : style.SelectedTextColor)
            : (isHover ? style.HoverTextColor : style.TextColor);
        if (bg) css += 'background-color:' + bg + ';';
        if (color) css += 'color:' + color + ';';
        if (style.FontFamily) css += 'font-family:' + style.FontFamily + ',sans-serif;';
        if (style.FontSize && style.FontSize > 0) css += 'font-size:' + style.FontSize + 'px;';
        if (style.FontWeight) {
            var w = style.FontWeight === 'Bold' ? 'bold' : style.FontWeight === 'Light' ? '300' : 'normal';
            css += 'font-weight:' + w + ';';
        }
        return css;
    }

    // ── Icon helpers ─────────────────────────────────────────────────────────

    /**
     * Converts an ImageInfo object (as serialised by the C# platform) into an <img>
     * element. ImageInfo supports four storage types — the numeric values here match
     * the ImageInfoType enum in DecisionsFramework.ServiceLayer.Services.Image.
     *
     * virtualPath is a global string injected by the platform that contains the
     * application root path (e.g. "/Primary"). We prepend it to relative URLs so the
     * component works regardless of which virtual directory Decisions is hosted under.
     */
    function _buildIconElement(icon) {
        if (!icon) return null;
        var base = (typeof virtualPath !== 'undefined' ? virtualPath : '');
        var url = null;

        switch (icon.ImageType) {
            case 1: // Url — a plain HTTP/S address provided by the user
                url = icon.ImageUrl;
                break;
            case 2: // StoredImage — an SVG from the Decisions image library (most common)
                if (icon.ImageId) {
                    url = base + '/Handlers/SvgImage.ashx?svgFile=' + encodeURIComponent(icon.ImageId);
                }
                break;
            case 3: // Document — an image stored as a Decisions Document entity
                if (icon.DocumentId) {
                    url = base + '/Handlers/SvgImage.ashx?documentId=' + encodeURIComponent(icon.DocumentId);
                }
                break;
            case 0: // RawData — a file uploaded directly via the image picker
                if (icon.ImageFileReferenceId) {
                    url = base + '/Primary/API/FileReferenceService/JS/DownloadFile?id=' + encodeURIComponent(icon.ImageFileReferenceId) + '&fileName=' + encodeURIComponent(icon.ImageName || '');
                }
                break;
        }

        if (!url) return null;
        return $('<img>').addClass('dp-navmenu-icon').attr({ src: url, alt: '' });
    }

    /**
     * Builds the four state-specific URLs for a StoredImage SVG icon.
     * Only StoredImage (ImageType === 2) supports color variants.
     *
     * Decisions pre-renders colored icons as separate PNG files with the hex color
     * appended to the stem, e.g. "FOLDER|icon_100_100.png" becomes
     * "FOLDER|icon_100_100_FF0000.png". See _coloredImageId for details.
     *
     * Returns null when no colors are configured so the caller skips wiring entirely.
     * When a state's color is empty the original ImageId is used unchanged, so the
     * icon renders with the color chosen in the Decisions image picker.
     */
    function _buildIconUrls(icon, style) {
        if (!icon || icon.ImageType !== 2 || !icon.ImageId) return null;
        if (!style) return null;

        var useFontColor = !!style.SvgUseFontColor;

        var normal, hover, selected, selectedHover;
        if (useFontColor) {
            normal        = style.TextColor;
            hover         = style.HoverTextColor;
            selected      = style.SelectedTextColor;
            selectedHover = style.SelectedHoverTextColor;
        } else {
            normal        = style.SvgIconColor;
            hover         = style.SvgHoverIconColor;
            selected      = style.SvgSelectedIconColor;
            selectedHover = style.SvgSelectedHoverIconColor;
        }

        if (!normal && !hover && !selected && !selectedHover) return null;

        var base = (typeof virtualPath !== 'undefined' ? virtualPath : '');
        var colorUrl = function (hex) {
            return base + '/Handlers/SvgImage.ashx?svgFile=' + encodeURIComponent(_coloredImageId(icon.ImageId, hex));
        };

        return {
            normal:        colorUrl(normal),
            hover:         colorUrl(hover),
            selected:      colorUrl(selected),
            selectedHover: colorUrl(selectedHover)
        };
    }

    /**
     * Returns an ImageId with the hex color suffix replaced or appended.
     *
     * Decisions stores pre-colored icon variants by appending the 6-char uppercase
     * hex to the filename stem before the extension:
     *   "FOLDER|icon_100_100.png"        → no color (original)
     *   "FOLDER|icon_100_100_37FF92.png" → pre-colored green
     *
     * If color is empty/null the original imageId is returned so the icon falls back
     * to the color chosen when the icon was picked in the Decisions image picker.
     */
    function _coloredImageId(imageId, color) {
        if (!color) return imageId;

        var hex = color.replace('#', '').toUpperCase();
        if (!/^[0-9A-F]{6}$/.test(hex)) return imageId;

        var pipeIdx = imageId.indexOf('|');
        if (pipeIdx === -1) return imageId;

        var folder   = imageId.substring(0, pipeIdx);
        var filename = imageId.substring(pipeIdx + 1);
        var dotIdx   = filename.lastIndexOf('.');
        var ext      = dotIdx > -1 ? filename.substring(dotIdx) : '';
        var stem     = dotIdx > -1 ? filename.substring(0, dotIdx) : filename;
        var parts    = stem.split('_');

        // Decisions filenames are: {name}_{width}_{height}[_{color}]
        // The color suffix can be a hex code (FFFFFF) or a color name (white, black).
        // Find the last pair of consecutive numeric parts — that is the dimension pair.
        // Everything after those two parts is the existing color suffix and should be replaced.
        var dimEnd = -1;
        for (var i = parts.length - 2; i >= 0; i--) {
            if (/^\d+$/.test(parts[i]) && /^\d+$/.test(parts[i + 1])) {
                dimEnd = i + 1;
                break;
            }
        }

        if (dimEnd === -1) {
            parts.push(hex);
        } else {
            parts = parts.slice(0, dimEnd + 1);
            parts.push(hex);
        }

        return folder + '|' + parts.join('_') + ext;
    }

    /**
     * After _updateActiveItem changes which item carries dp-navmenu-active,
     * update each icon's src to match the new selected/normal state.
     * Mouse-hover state is handled separately via the mouseenter/mouseleave
     * handlers wired in _renderItem.
     */
    function _updateIconStates(instanceId) {
        var opts = _instances[instanceId];
        if (!opts) return;
        opts.holder.find('.dp-navmenu-item').each(function () {
            var $li = $(this);
            var urls = $li.data('dp-icon-urls');
            if (!urls) return;
            $li.find('> .dp-navmenu-link > .dp-navmenu-icon')
               .attr('src', $li.hasClass('dp-navmenu-active') ? urls.selected : urls.normal);
        });
    }

    // ── Rendering ───────────────────────────────────────────────────────────

    var _justifyClass = {
        'Start':        'dp-navmenu-justify-start',
        'Center':       'dp-navmenu-justify-center',
        'End':          'dp-navmenu-justify-end',
        'SpaceBetween': 'dp-navmenu-justify-between',
        'SpaceEvenly':  'dp-navmenu-justify-evenly'
    };

    var _valignClass = {
        'Top':     'dp-navmenu-valign-top',
        'Center':  'dp-navmenu-valign-center',
        'Bottom':  'dp-navmenu-valign-bottom',
        'Stretch': 'dp-navmenu-valign-stretch'
    };

    function _render(instanceId) {
        var opts = _instances[instanceId];
        var $nav = $('<nav>').addClass('dp-navmenu');

        if (opts.orientation === 'Vertical') {
            $nav.addClass('dp-navmenu-vertical');
            if (opts.verticalAlign && _valignClass[opts.verticalAlign]) {
                $nav.addClass(_valignClass[opts.verticalAlign]);
            }
        } else {
            $nav.addClass('dp-navmenu-horizontal');
            if (opts.horizontalJustify && _justifyClass[opts.horizontalJustify]) {
                $nav.addClass(_justifyClass[opts.horizontalJustify]);
            }
        }

        if (opts.cssClass) {
            $nav.addClass(opts.cssClass);
        }

        var $ul = $('<ul>').addClass('dp-navmenu-list');
        var topItems = opts.items || [];
        topItems.forEach(function (item, index) {
            $ul.append(_renderItem(instanceId, item, false));
            if (item.SeparatorAfter && index < topItems.length - 1) {
                $ul.append($('<li>').addClass('dp-navmenu-separator'));
            }
        });

        $nav.append($ul);
        opts.holder.empty().append($nav);
    }

    function _renderItem(instanceId, item, isSubItem) {
        var opts = _instances[instanceId];
        var $li = $('<li>').addClass('dp-navmenu-item');

        if (item.FolderId)          $li.attr('data-folder-id', item.FolderId);
        if (item.PageName)          $li.attr('data-page-name',  item.PageName);
        if (item.SelectionBusValue) $li.attr('data-bus-value',  item.SelectionBusValue);

        var hasDropdown = item.SubItems && item.SubItems.length > 0;
        if (hasDropdown) $li.addClass('dp-navmenu-has-dropdown');

        var $a = $('<a>').addClass('dp-navmenu-link').attr('href', '#');
        if (!item.FolderId && !(item.OpenUrl && item.Url)) $a.addClass('dp-navmenu-inert');

        var override = item.StyleOverride;
        if (override) {
            var baseStyle = _itemInlineStyle(override, false, false);
            if (baseStyle) $a.attr('style', baseStyle);

            $a.on('mouseenter', function () {
                $(this).attr('style', _itemInlineStyle(override, $li.hasClass('dp-navmenu-active'), true));
            }).on('mouseleave', function () {
                $(this).attr('style', _itemInlineStyle(override, $li.hasClass('dp-navmenu-active'), false));
            });
        }

        var $label = $('<span>').addClass('dp-navmenu-label').text(item.Label || '');
        var $icon = _buildIconElement(item.Icon);

        if ($icon && item.IconPosition === 'Right') {
            $a.append($label).append($icon);
        } else {
            if ($icon) $a.append($icon);
            $a.append($label);
        }

        // SVG icon color: build four state-specific URLs and swap src on state changes.
        // Only applies to StoredImage SVGs — other icon types render without color tinting.
        if ($icon && item.Icon) {
            var effectiveStyle = item.StyleOverride || (isSubItem ? opts.subItemStyle : opts.topLevelStyle);
            var iconUrls = _buildIconUrls(item.Icon, effectiveStyle);
            if (iconUrls) {
                $icon.attr('src', iconUrls.normal);
                $li.data('dp-icon-urls', iconUrls);
                $a.on('mouseenter.iconcolor', function () {
                    $icon.attr('src', $li.hasClass('dp-navmenu-active') ? iconUrls.selectedHover : iconUrls.hover);
                }).on('mouseleave.iconcolor', function () {
                    $icon.attr('src', $li.hasClass('dp-navmenu-active') ? iconUrls.selected : iconUrls.normal);
                });
            }
        }

        if (hasDropdown) {
            $a.append($('<span>').addClass('dp-navmenu-arrow'));
        }

        $a.on('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            _onItemClick(instanceId, item, $li);
        });

        $li.append($a);

        if (hasDropdown) {
            var $dropdown = $('<ul>').addClass('dp-navmenu-dropdown');
            var subItems = item.SubItems;
            subItems.forEach(function (sub, index) {
                $dropdown.append(_renderItem(instanceId, sub, true));
                if (sub.SeparatorAfter && index < subItems.length - 1) {
                    $dropdown.append($('<li>').addClass('dp-navmenu-separator'));
                }
            });
            $li.append($dropdown);

            (function () {
                var hideTimer;

                var showDropdown = function () {
                    clearTimeout(hideTimer);
                    var rect = $li[0].getBoundingClientRect();
                    if (isSubItem) {
                        $dropdown.css({ top: rect.top, left: rect.right, 'min-width': rect.width });
                    } else {
                        $dropdown.css({ top: rect.bottom, left: rect.left, 'min-width': rect.width });
                    }
                    $dropdown.addClass('dp-navmenu-dropdown-open');
                    $li.addClass('dp-navmenu-item-open');
                };

                var hideDropdown = function () {
                    hideTimer = setTimeout(function () {
                        $dropdown.removeClass('dp-navmenu-dropdown-open');
                        $li.removeClass('dp-navmenu-item-open');
                    }, 80);
                };

                $li.on('mouseenter', showDropdown).on('mouseleave', hideDropdown);
                $dropdown.on('mouseenter', function () { clearTimeout(hideTimer); })
                         .on('mouseleave', hideDropdown);
            }());
        }

        return $li;
    }

    // ── Flow runner ──────────────────────────────────────────────────────────

    /**
     * Launches a Decisions flow and displays its first form step in a modal dialog.
     *
     * Decisions flows are server-side processes. Running one returns HTML for the
     * first interactive form step. The platform's ActionExecutor handles displaying
     * that HTML in a dialog and managing subsequent steps.
     *
     * ignoreMissingInputs: true allows the flow to start even if it expects input
     * data that we are not supplying — the flow itself handles missing inputs.
     */
    function _runFlow(flowId, folderId, pageName) {
        var d = $.Deferred();
        var instanceId = Decisions.GenerateNewGUID();
        var formWrapperId = 'formWrapper_' + instanceId;
        $.ajax({
            type: 'Post',
            contentType: 'application/json; charset=utf-8',
            dataType: 'html',
            data: JSON.stringify({
                flowId: flowId,
                instanceId: instanceId,
                folderId: folderId || Decisions.Navigation.currentFolderId,
                pageName: pageName || null,
                pageContextId: Decisions.Navigation.pageContextId || null,
                inputData: null,
                ignoreMissingInputs: true
            }),
            url: virtualPath + '/Form/RunFlow'
        }).then(function (html) {
            var wrappedForm = '<div id="' + formWrapperId + '" class="run-flow-action-form-wrapper" iscontrol="true" iscontrolloaded="false">' + html + '</div>';
            d.resolve(wrappedForm);
        });
        $DP.Common.Actions.ActionExecutor.runActionInDialog(d, 'Run Flow');
    }

    // ── Click handler ────────────────────────────────────────────────────────

    /**
     * Handles a click on any menu item. Supports four item types:
     *   1. External URL  — opens in same tab or new tab.
     *   2. Folder + Flow — navigates to the folder, then runs a flow once there.
     *   3. Folder only   — navigates to the folder (optionally in a new window).
     *   4. Flow only     — runs a flow in a dialog without changing the folder.
     *
     * Bus publishing happens before navigation so that other controls on the page
     * receive the new bus value at the same time as the folder changes.
     */
    function _onItemClick(instanceId, item, $li) {
        var opts = _instances[instanceId];
        if (opts.isDesignMode) return;

        // External URL items: bypass all Decisions navigation logic.
        if (item.OpenUrl && item.Url) {
            if (item.OpenUrlInNewPage) {
                window.open(item.Url, '_blank');
            } else {
                window.location.href = item.Url;
            }
            return;
        }

        // Publish the item's SelectionBusValue to the navigation parameter bus so
        // other controls on the page (e.g. data grids) can react to the selection.
        // _pendingBusValue is stored here because _updateActiveItem may fire before
        // the URL has been updated with the new bus value.
        var busName = opts.selectionBusName || null;
        if (busName && item.SelectionBusValue !== undefined && item.SelectionBusValue !== null && item.SelectionBusValue !== '') {
            _pendingBusValue = item.SelectionBusValue;
            _publishToBus(busName, item.SelectionBusValue);
        } else {
            _pendingBusValue = null;
            if (busName) _publishToBus(busName, null);
        }

        if (item.FolderId) {
            if (item.OpenInNewWindow) {
                window.open(_buildFolderUrl(item.FolderId, item.PageName, busName, item.SelectionBusValue, item.FlowId, item.HidePortal), '_blank');
            } else {
                if (item.FlowId) {
                    // When a flow must run after navigating to a folder, we first check if
                    // we are already on that folder to avoid a redundant navigation.
                    var loc = _getCurrentLocation();
                    var alreadyThere = loc && loc.folderId === item.FolderId &&
                        (!item.PageName || !loc.pageName || loc.pageName === item.PageName);
                    if (alreadyThere) {
                        _runFlow(item.FlowId, item.FolderId, item.PageName);
                    } else {
                        // We need to navigate first, then run the flow. We listen for
                        // navigationParameterChanged, which fires once the Decisions SPA
                        // has finished navigating. A debounce (150 ms) prevents the flow
                        // from running multiple times if the event fires more than once
                        // during the navigation transition.
                        var targetFolderId = item.FolderId;
                        var targetFlowId   = item.FlowId;
                        var targetPageName = item.PageName;
                        var debounceTimer  = null;
                        var onParamChanged = function () {
                            var current = _getCurrentLocation();
                            if (current && current.folderId === targetFolderId) {
                                clearTimeout(debounceTimer);
                                debounceTimer = setTimeout(function () {
                                    $(document).off('navigationParameterChanged', onParamChanged);
                                    _runFlow(targetFlowId, targetFolderId, targetPageName);
                                }, 150);
                            }
                        };
                        $(document).on('navigationParameterChanged', onParamChanged);
                    }
                }
                Decisions.Navigation.navigateToFolder(item.FolderId, item.PageName || null);
            }
        } else if (item.FlowId) {
            _runFlow(item.FlowId, null, null);
        }
    }

    // ── Selection bus helpers ────────────────────────────────────────────────

    /**
     * Publishes a value to a named Decisions navigation parameter (the "bus").
     * UpdateNavigationParameters broadcasts the change to all controls on the page
     * that listen for navigationParameterChanged, so they can react in real time
     * without a full page reload.
     */
    function _publishToBus(busName, value) {
        var params = {};
        params[busName] = value;
        $DP.Navigation.Helper.UpdateNavigationParameters(params);
    }

    // ── Navigation helper ────────────────────────────────────────────────────

    /**
     * Builds a full URL for navigating to a folder, used when opening items in a
     * new browser window. Includes the bus value and flow ID as query parameters so
     * the target page starts in the right state.
     *
     * chrome=off hides the Decisions portal chrome (header, sidebar) so the page
     * renders as a standalone view — useful for embedding in iframes or kiosk displays.
     */
    function _buildFolderUrl(folderId, pageName, busName, busValue, flowId, hidePortal) {
        var params = new URLSearchParams();
        params.set('FolderId', folderId);
        if (pageName) params.set('pageName', pageName);
        if (busName && busValue != null && busValue !== '') params.set(busName, busValue);
        // autoRunFlowId is picked up by initialize() on the target page to trigger
        // the flow automatically after the page loads (see the auto-run block above).
        if (flowId) params.set('autoRunFlowId', flowId);
        if (hidePortal) params.set('chrome', 'off');
        return window.location.pathname + '?' + params.toString();
    }

    // ── Public surface ───────────────────────────────────────────────────────
    // Only expose what the platform needs to call from outside this module.
    // _updateActiveItem is exposed so the platform can force a highlight refresh
    // if needed; all other functions remain private.

    return {
        initialize: initialize,
        _updateActiveItem: _updateActiveItem
    };
}());
