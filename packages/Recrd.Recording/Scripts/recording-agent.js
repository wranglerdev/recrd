/**
 * recording-agent.js
 * Injected into every frame via BrowserContext.AddInitScriptAsync.
 * Captures 7 DOM event types and sends them to C# via window.__recrdCapture.
 */
(function () {
    'use strict';

    // ── Mode state ──────────────────────────────────────────────────────────
    window.__recrdMode = 'record';

    window.__recrdSetMode = function (mode) {
        window.__recrdMode = mode;
    };

    // ── Inject overlay CSS ──────────────────────────────────────────────────
    (function injectStyles() {
        var style = document.createElement('style');
        style.textContent = [
            '.__recrd__menu {',
            '  position: fixed;',
            '  z-index: 2147483647;',
            '  background: #1a1a1a;',
            '  color: #e5e5e5;',
            '  border: 1px solid #3a3a3a;',
            '  border-radius: 4px;',
            '  font-family: system-ui, -apple-system, "Segoe UI", sans-serif;',
            '  font-size: 14px;',
            '  padding: 4px 0;',
            '  min-width: 160px;',
            '  max-height: 200px;',
            '  overflow-y: auto;',
            '  box-shadow: 0 4px 12px rgba(0,0,0,0.4);',
            '}',
            '.__recrd__menu-item {',
            '  display: block;',
            '  min-height: 32px;',
            '  line-height: 32px;',
            '  padding: 0 12px;',
            '  cursor: pointer;',
            '  white-space: nowrap;',
            '  user-select: none;',
            '}',
            '.__recrd__menu-item:hover {',
            '  background: #2a2a2a;',
            '}',
        ].join('\n');
        if (document.head) {
            document.head.appendChild(style);
        } else {
            document.addEventListener('DOMContentLoaded', function () {
                document.head.appendChild(style);
            });
        }
    })();

    // ── Unique ID generator ─────────────────────────────────────────────────
    function generateId() {
        return 'evt-' + Date.now() + '-' + Math.random().toString(36).slice(2, 8);
    }

    // ── Selector extraction ─────────────────────────────────────────────────
    function __extractSelectors(element) {
        if (!element || element.nodeType !== 1) {
            return { strategies: ['Css', 'XPath'], values: { Css: '*', XPath: '//*' } };
        }

        var strategies = [];
        var values = {};

        // DataTestId
        var testId = element.getAttribute('data-testid');
        if (testId) {
            strategies.push('DataTestId');
            values['DataTestId'] = '[data-testid="' + testId + '"]';
        }

        // Id
        if (element.id) {
            strategies.push('Id');
            values['Id'] = '#' + element.id;
        }

        // Role
        var role = element.getAttribute('role');
        if (!role) {
            var tag = element.tagName.toLowerCase();
            if (tag === 'button') {
                role = 'button';
            } else if (tag === 'a') {
                role = 'link';
            } else if (tag === 'input') {
                var type = (element.type || 'text').toLowerCase();
                if (type === 'checkbox') {
                    role = 'checkbox';
                } else if (type === 'radio') {
                    role = 'radio';
                } else {
                    role = 'textbox';
                }
            } else if (tag === 'select') {
                role = 'combobox';
            } else if (tag === 'textarea') {
                role = 'textbox';
            }
        }
        if (role) {
            strategies.push('Role');
            values['Role'] = role;
        }

        // Css — always available
        var tag = element.tagName.toLowerCase();
        var classes = Array.prototype.slice.call(element.classList, 0, 3);
        var cssSelector = tag + (classes.length > 0 ? '.' + classes.join('.') : '');
        strategies.push('Css');
        values['Css'] = cssSelector;

        // XPath — always available
        var xpath = buildXPath(element);
        strategies.push('XPath');
        values['XPath'] = xpath;

        return { strategies: strategies, values: values };
    }

    function buildXPath(element) {
        if (!element || element === document.documentElement) {
            return '/html';
        }
        if (element === document.body) {
            return '/html/body';
        }

        var path = [];
        var current = element;
        while (current && current.nodeType === 1 && current !== document.documentElement) {
            var tagName = current.tagName.toLowerCase();
            var index = 1;
            var sibling = current.previousSibling;
            while (sibling) {
                if (sibling.nodeType === 1 && sibling.tagName && sibling.tagName.toLowerCase() === tagName) {
                    index++;
                }
                sibling = sibling.previousSibling;
            }
            path.unshift(tagName + '[' + index + ']');
            current = current.parentElement;
        }
        return '/html/' + path.join('/');
    }

    // ── Event dispatch ──────────────────────────────────────────────────────
    function dispatchEvent(type, selectors, payload) {
        if (window.__recrdMode !== 'record') {
            return;
        }
        if (typeof window.__recrdCapture !== 'function') {
            return;
        }
        var id = generateId();
        var eventData = {
            id: id,
            timestamp: performance.now(),
            type: type,
            selectors: selectors,
            payload: payload,
            isPopup: window.opener !== null
        };
        window.__recrdCapture(JSON.stringify(eventData));
    }

    function dispatchSpecialEvent(type, selectors, payload) {
        if (typeof window.__recrdCapture !== 'function') {
            return;
        }
        var id = generateId();
        var eventData = {
            id: id,
            timestamp: performance.now(),
            type: type,
            selectors: selectors,
            payload: payload,
            isPopup: window.opener !== null
        };
        window.__recrdCapture(JSON.stringify(eventData));
    }

    // ── Click ───────────────────────────────────────────────────────────────
    document.addEventListener('click', function (event) {
        var target = event.target;
        if (!target) return;
        var selectors = __extractSelectors(target);
        dispatchEvent('Click', selectors, {});
    }, true);

    // ── Input / Change ──────────────────────────────────────────────────────
    document.addEventListener('input', function (event) {
        var target = event.target;
        if (!target) return;
        var tag = target.tagName ? target.tagName.toLowerCase() : '';
        if (tag === 'input' || tag === 'textarea') {
            var selectors = __extractSelectors(target);
            dispatchEvent('InputChange', selectors, { value: target.value || '' });
        }
    }, true);

    document.addEventListener('change', function (event) {
        var target = event.target;
        if (!target) return;
        var tag = target.tagName ? target.tagName.toLowerCase() : '';

        if (tag === 'select') {
            var selectors = __extractSelectors(target);
            var selectedText = '';
            if (target.options && target.selectedIndex >= 0) {
                selectedText = target.options[target.selectedIndex].text;
            }
            dispatchEvent('Select', selectors, { value: target.value || '', text: selectedText });
        } else if (tag === 'input') {
            var inputType = (target.type || 'text').toLowerCase();
            if (inputType === 'file') {
                var selectors2 = __extractSelectors(target);
                var fileNames = '';
                if (target.files) {
                    fileNames = Array.prototype.map.call(target.files, function (f) { return f.name; }).join(',');
                }
                dispatchEvent('FileUpload', selectors2, { files: fileNames });
            } else {
                var selectors3 = __extractSelectors(target);
                dispatchEvent('InputChange', selectors3, { value: target.value || '' });
            }
        } else if (tag === 'textarea') {
            var selectors4 = __extractSelectors(target);
            dispatchEvent('InputChange', selectors4, { value: target.value || '' });
        }
    }, true);

    // ── Hover (opt-in only via data-recrd-hover="true") ─────────────────────
    document.addEventListener('mouseover', function (event) {
        var target = event.target;
        if (!target) return;
        if (target.getAttribute && target.getAttribute('data-recrd-hover') === 'true') {
            var selectors = __extractSelectors(target);
            dispatchEvent('Hover', selectors, {});
        }
    }, true);

    // ── Navigation ──────────────────────────────────────────────────────────
    window.addEventListener('beforeunload', function () {
        dispatchEvent('Navigation', { strategies: [], values: {} }, { url: window.location.href });
    });

    window.addEventListener('popstate', function () {
        dispatchEvent('Navigation', { strategies: [], values: {} }, { url: window.location.href });
    });

    // ── DragDrop ─────────────────────────────────────────────────────────────
    document.addEventListener('dragend', function (event) {
        var source = event.target;
        if (!source) return;
        var sourceSelectors = __extractSelectors(source);
        var targetSelectors = null;
        if (event.target !== source) {
            targetSelectors = __extractSelectors(event.target);
        } else {
            targetSelectors = { strategies: [], values: {} };
        }
        dispatchEvent('DragDrop', sourceSelectors, {
            targetSelector: JSON.stringify(targetSelectors)
        });
    }, true);

    // ── Right-click context menu overlay ────────────────────────────────────
    var _contextMenuEl = null;
    var _contextMenuTarget = null;

    function removeContextMenu() {
        if (_contextMenuEl && _contextMenuEl.parentNode) {
            _contextMenuEl.parentNode.removeChild(_contextMenuEl);
        }
        _contextMenuEl = null;
        _contextMenuTarget = null;
    }

    document.addEventListener('contextmenu', function (event) {
        var mode = window.__recrdMode;
        if (mode !== 'record' && mode !== 'pause') {
            return;
        }

        event.preventDefault();
        removeContextMenu();

        var target = event.target;
        _contextMenuTarget = target;

        var menu = document.createElement('div');
        menu.className = '__recrd__menu';
        menu.style.left = event.clientX + 'px';
        menu.style.top = event.clientY + 'px';

        // "Tag as Variable" — always visible
        var tagItem = document.createElement('div');
        tagItem.className = '__recrd__menu-item';
        tagItem.textContent = 'Tag as Variable';
        tagItem.addEventListener('click', function () {
            var selectors = __extractSelectors(_contextMenuTarget);
            var payload = {
                value: _contextMenuTarget ? (_contextMenuTarget.value || _contextMenuTarget.textContent || '') : ''
            };
            dispatchSpecialEvent('TagStart', selectors, payload);
            removeContextMenu();
        });
        menu.appendChild(tagItem);

        // "Add Assertion" — only in pause mode
        if (mode === 'pause') {
            var assertItem = document.createElement('div');
            assertItem.className = '__recrd__menu-item';
            assertItem.textContent = 'Add Assertion';
            assertItem.addEventListener('click', function () {
                var selectors = __extractSelectors(_contextMenuTarget);
                var payload = {
                    textContent: _contextMenuTarget ? (_contextMenuTarget.textContent || '') : ''
                };
                dispatchSpecialEvent('AssertStart', selectors, payload);
                removeContextMenu();
            });
            menu.appendChild(assertItem);
        }

        document.body.appendChild(menu);
        _contextMenuEl = menu;
    }, true);

    // Dismiss menu on Escape or outside click
    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            removeContextMenu();
        }
    }, true);

    document.addEventListener('click', function (event) {
        if (_contextMenuEl && !_contextMenuEl.contains(event.target)) {
            removeContextMenu();
        }
    }, false);

    // ── Mode control helpers (called from C# via Page.EvaluateAsync) ─────────
    window.__recrdShowTagDialog = function () {
        // Called from C# on the inspector page; no-op on recording page
    };

    window.__recrdShowAssertDialog = function () {
        // Called from C# on the inspector page; no-op on recording page
    };

})();
