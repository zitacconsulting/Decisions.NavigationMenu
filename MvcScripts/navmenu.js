var $DP = $DP || {};
$DP.Components = $DP.Components || {};
$DP.Components.Page = $DP.Components.Page || {};

$DP.Components.Page.NavMenu = (function () {
    'use strict';

    var _instances = {};
    var _pendingBusValue = null; // bus value from last clicked item — survives navigation parameter reset

    // ── Public API ──────────────────────────────────────────────────────────

    function initialize(opts) {
        _instances[opts.instanceId] = opts;
        _injectStyles(opts.instanceId);
        _render(opts.instanceId);

        if (opts.isDesignMode) return;

        var ns = '.navmenu-' + opts.instanceId;
        $(document).on('navigationParameterChanged' + ns, function () {
            _updateActiveItem(opts.instanceId);
        });
        $(document).on('decisionsnavigationcomplete' + ns, function () {
            _updateActiveItem(opts.instanceId);
        });
        _updateActiveItem(opts.instanceId);
    }

    // ── Active item highlighting ─────────────────────────────────────────────

    function _getCurrentLocation() {
        var params = new URLSearchParams(window.location.search);
        var folderId = params.get('FolderId') || params.get('folderId');
        var pageName = params.get('pageName') || params.get('PageName');
        if (folderId) return { folderId: folderId, pageName: pageName };
        return null;
    }

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

    function _updateActiveItem(instanceId) {
        var opts = _instances[instanceId];
        if (!opts) return;

        opts.holder.find('.dp-navmenu-item').removeClass('dp-navmenu-active');

        var loc = _getCurrentLocation();
        var busName = opts.selectionBusName || null;
        var liveParams = ($DP.Navigation && $DP.Navigation.Helper && $DP.Navigation.Helper.navigationParameters) || {};
        var busValue = _pendingBusValue !== null ? _pendingBusValue
                     : (busName && liveParams[busName] != null) ? String(liveParams[busName])
                     : (busName ? (new URLSearchParams(window.location.search)).get(busName) : null);

        var $match = _findBestMatch(opts.holder, loc, busName, busValue);

        _syncBusToUrl(busName, busValue);

        if (!$match) return;

        $match.addClass('dp-navmenu-active');
        var $top = _getTopLevelParent($match, opts.holder);
        if ($top && $top[0] !== $match[0]) {
            $top.addClass('dp-navmenu-active');
        }
    }

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

            // Score: +1 for folder match (always here), +1 for bus match
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

    function _getTopLevelParent($li, $holder) {
        var $topList = $holder.find('> nav > .dp-navmenu-list');
        if ($li.parent().is($topList)) return $li;
        // Walk up through dropdown ancestors
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

    function _injectStyles(instanceId) {
        var opts = _instances[instanceId];
        var id = 'dp-navmenu-style-' + instanceId;
        if (document.getElementById(id)) return;

        var top = opts.topLevelStyle || {};
        var sub = opts.subItemStyle || {};

        var css = _buildScopedCss('#navmenu-holder-' + instanceId, top, sub, opts.itemSpacing || 0, opts.controlBackgroundColor || '');
        if (!css) return;

        var $style = $('<style>').attr('id', id).text(css);
        $('head').append($style);
    }

    function _buildScopedCss(scope, top, sub, itemSpacing, controlBg) {
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

    // ── Icon helper ──────────────────────────────────────────────────────────

    function _buildIconElement(icon) {
        if (!icon) return null;
        var base = (typeof virtualPath !== 'undefined' ? virtualPath : '');
        var url = null;

        switch (icon.ImageType) {
            case 1: // Url
                url = icon.ImageUrl;
                break;
            case 2: // StoredImage
                if (icon.ImageId) {
                    url = base + '/Handlers/SvgImage.ashx?svgFile=' + encodeURIComponent(icon.ImageId);
                }
                break;
            case 3: // Document
                if (icon.DocumentId) {
                    url = base + '/Handlers/SvgImage.ashx?documentId=' + encodeURIComponent(icon.DocumentId);
                }
                break;
            case 0: // RawData / File
                if (icon.ImageFileReferenceId) {
                    url = base + '/Primary/API/FileReferenceService/JS/DownloadFile?id=' + encodeURIComponent(icon.ImageFileReferenceId) + '&fileName=' + encodeURIComponent(icon.ImageName || '');
                }
                break;
        }

        if (!url) return null;
        return $('<img>').addClass('dp-navmenu-icon').attr({ src: url, alt: '' });
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
        (opts.items || []).forEach(function (item) {
            $ul.append(_renderItem(instanceId, item, false));
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
        if (!item.FolderId) $a.addClass('dp-navmenu-inert');

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
            item.SubItems.forEach(function (sub) {
                $dropdown.append(_renderItem(instanceId, sub, true));
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

    function _onItemClick(instanceId, item, $li) {
        var opts = _instances[instanceId];
        if (opts.isDesignMode) return;

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
                var url = _buildFolderUrl(item.FolderId, item.PageName);
                window.open(url, '_blank');
                if (item.FlowId) {
                    _runFlow(item.FlowId, item.FolderId, item.PageName);
                }
            } else {
                if (item.FlowId) {
                    $(document).one('decisionsnavigationcomplete', function () {
                        _runFlow(item.FlowId, item.FolderId, item.PageName);
                    });
                }
                Decisions.Navigation.navigateToFolder(item.FolderId, item.PageName || null);
            }
        } else if (item.FlowId) {
            _runFlow(item.FlowId, null, null);
        }
    }

    // ── Selection bus helpers ────────────────────────────────────────────────

    function _publishToBus(busName, value) {
        var params = {};
        params[busName] = value;
        $DP.Navigation.Helper.UpdateNavigationParameters(params);
    }

    // ── Navigation helper ────────────────────────────────────────────────────

    function _buildFolderUrl(folderId, pageName) {
        var base = (typeof virtualPath !== 'undefined' ? virtualPath : '') + '/';
        return base + 'Primary/' + encodeURIComponent(folderId) + (pageName ? '/' + encodeURIComponent(pageName) : '');
    }

    // ── Public surface ───────────────────────────────────────────────────────

    return {
        initialize: initialize,
        _updateActiveItem: _updateActiveItem
    };
}());
