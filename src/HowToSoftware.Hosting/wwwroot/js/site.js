/*
 * HowToSoftware Hosting - progressive enhancement
 * ---------------------------------------------------------------------------
 * Deliberately small and dependency-free. Blazor and CSS own the interface;
 * this file only covers browser facts neither can observe on its own:
 *
 *   1. Reveal elements the first time they scroll into view.
 *   2. Flag the header once the page has scrolled past the top.
 *   3. Report which provisioning stage is centred in the viewport.
 *   4. Split headline text into characters so CSS can animate them.
 *
 * Note the split of responsibilities in (3): JavaScript observes, Blazor
 * decides. The observer calls [JSInvokable] SetActiveStage and every visual
 * consequence - stage styling, diagram state, the step counter - is rendered
 * from C#. No application state lives here.
 *
 * Everything degrades: without JavaScript the `hts-js` class is never added,
 * so all `[data-reveal]` content stays fully visible and the provisioning
 * diagram simply rests on its first stage.
 */
(function () {
    "use strict";

    var root = document.documentElement;
    var prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");

    if (!prefersReducedMotion.matches && "IntersectionObserver" in window) {
        root.classList.add("hts-js");
    }

    // ── 1. Scroll reveal ────────────────────────────────────────────────────
    var observer = null;

    if (root.classList.contains("hts-js")) {
        observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) {
                    return;
                }

                entry.target.classList.add("is-revealed");
                observer.unobserve(entry.target);
            });
        }, { rootMargin: "0px 0px -10% 0px", threshold: 0.05 });
    }

    var REVEALS = "[data-reveal], [data-text-effect]";

    function observe(node) {
        if (!observer || !node.matches) {
            return;
        }

        // Split text animates off the same class as a scroll reveal, so an element carrying
        // only [data-text-effect] has to be watched too. The hero headline is one: it sits
        // outside any [data-reveal] wrapper, and before this it never became visible at all.
        if (!node.matches(REVEALS) || node.classList.contains("is-revealed")) {
            return;
        }

        observer.observe(node);
    }

    function scan(scope) {
        if (!observer) {
            return;
        }

        observe(scope);

        if (scope.querySelectorAll) {
            scope.querySelectorAll(REVEALS).forEach(observe);
        }
    }

    // ── 1b. Character split ───────────────────────────────────────────────
    //
    // CSS can animate an element. It cannot animate the letters inside one, because
    // there is nothing in the DOM to address. This wraps each character in a span
    // carrying its index, and the stylesheet does the rest - what the animation
    // looks like is not decided here.
    //
    // Accessibility: the original string is put on the element as aria-label and the
    // split spans are hidden from assistive technology, so a screen reader reads one
    // sentence rather than a stream of single letters.
    var SPLIT_LIMIT = 220;

    function splitTextNode(textNode, offset) {
        var words = textNode.nodeValue.split(/\s+/).filter(Boolean);

        if (!words.length) {
            return offset;
        }

        // One wrapper, not a run of loose nodes. The parent may be a flex container -
        // several of these kickers are - and a flex container drops whitespace-only
        // anonymous items, which ate the spaces between the words.
        var wrapper = document.createElement("span");
        wrapper.className = "split";

        // Whether this text node touched a sibling across a space. "keeps <span>running</span>"
        // is two nodes, and trimming the first one's trailing space closed the gap between the
        // words - the headline rendered as "keepsrunning".
        var raw = textNode.nodeValue;

        if (/^[\s]/.test(raw)) {
            wrapper.appendChild(document.createTextNode(" "));
        }

        var index = offset;

        for (var w = 0; w < words.length; w += 1) {
            if (w > 0) {
                // A real space inside normal inline layout, so the line can still break here.
                wrapper.appendChild(document.createTextNode(" "));
            }

            // Characters are inline-block, and a browser will break a line between any two
            // of them. Wrapping each word keeps the break points where words are.
            var word = document.createElement("span");
            word.className = "word";

            for (var i = 0; i < words[w].length; i += 1) {
                var span = document.createElement("span");
                span.className = "char";
                span.style.setProperty("--char-index", index);
                span.textContent = words[w].charAt(i);
                word.appendChild(span);
                index += 1;
            }

            wrapper.appendChild(word);
        }

        if (/[\s]$/.test(raw)) {
            wrapper.appendChild(document.createTextNode(" "));
        }

        textNode.parentNode.replaceChild(wrapper, textNode);
        return index;
    }

    function splitText(node) {
        if (node.dataset.textSplit === "done") {
            return;
        }

        node.dataset.textSplit = "done";

        var label = node.textContent.replace(/\s+/g, " ").trim();

        // A long paragraph is not worth hundreds of spans, and an empty element has
        // nothing to split. Both are left exactly as they are.
        if (!label || label.length > SPLIT_LIMIT) {
            return;
        }

        // Walk the text nodes rather than rewriting the element, so a heading keeps
        // its <br> and its accent <span> and the stagger still runs continuously
        // across all of them.
        var walker = document.createTreeWalker(node, NodeFilter.SHOW_TEXT, null);
        var pending = [];
        var found;

        while ((found = walker.nextNode())) {
            if (found.nodeValue.trim()) {
                pending.push(found);
            }
        }

        var index = 0;

        for (var i = 0; i < pending.length; i += 1) {
            index = splitTextNode(pending[i], index);
        }

        if (!index) {
            return;
        }

        node.setAttribute("aria-label", label);
        node.style.setProperty("--char-count", index);
    }

    function splitScope(scope) {
        if (!root.classList.contains("hts-js") || !scope.querySelectorAll) {
            return;
        }

        scope.querySelectorAll("[data-text-effect]").forEach(splitText);
    }

    // ── 2. Header scroll state ──────────────────────────────────────────────
    var scrollQueued = false;

    function applyScrollState() {
        scrollQueued = false;
        root.classList.toggle("hts-scrolled", window.scrollY > 12);
    }

    function onScroll() {
        if (scrollQueued) {
            return;
        }

        scrollQueued = true;
        window.requestAnimationFrame(applyScrollState);
    }

    // ── 3. Provisioning stage reporter ──────────────────────────────────────
    // One observer per section, watching a narrow band across the middle of the
    // viewport. Whichever stage crosses it becomes the active stage.
    var stageScopes = new Map();

    window.htsProvisioning = {
        observe: function (scope, dotNetRef) {
            if (!scope || stageScopes.has(scope)) {
                return;
            }

            var stages = scope.querySelectorAll("[data-stage-index]");
            if (!stages.length || !("IntersectionObserver" in window)) {
                return;
            }

            var stageObserver = new IntersectionObserver(function (entries) {
                entries.forEach(function (entry) {
                    if (!entry.isIntersecting) {
                        return;
                    }

                    var index = parseInt(entry.target.getAttribute("data-stage-index"), 10);
                    if (!isNaN(index)) {
                        dotNetRef.invokeMethodAsync("SetActiveStage", index);
                    }
                });
            }, { rootMargin: "-48% 0px -48% 0px", threshold: 0 });

            stages.forEach(function (stage) {
                stageObserver.observe(stage);
            });

            stageScopes.set(scope, stageObserver);
        },

        dispose: function (scope) {
            var stageObserver = stageScopes.get(scope);
            if (stageObserver) {
                stageObserver.disconnect();
                stageScopes.delete(scope);
            }
        }
    };

    // ── 4. Theme ────────────────────────────────────────────────────────────
    // Storage and the html attribute only. Which theme is active, and the button
    // that changes it, are owned by the ThemeToggle Blazor component.
    window.htsTheme = {
        isLight: function () {
            return document.documentElement.getAttribute("data-theme") === "light";
        },

        set: function (theme) {
            document.documentElement.setAttribute("data-theme", theme);
            try {
                localStorage.setItem("hts-theme", theme);
            } catch (e) {
                // Private mode or storage disabled; the choice just will not persist.
            }
        }
    };

    // ── 5. Pointer spotlight ────────────────────────────────────────────────
    // Where the pointer is, in element-local coordinates. CSS cannot ask that
    // question and neither can C#, so the only job here is to publish the answer
    // as two custom properties and let the stylesheet decide what to do with it.
    //
    // One delegated listener for the whole document rather than one per panel,
    // and only for a pointer that can actually hover - a touch "hover" would
    // light a panel up and leave it lit.
    var spotlit = null;
    var spotQueued = null;

    function paintSpotlight() {
        var pending = spotQueued;
        spotQueued = null;

        if (!pending) {
            return;
        }

        var rect = pending.target.getBoundingClientRect();
        pending.target.style.setProperty("--spot-x", ((pending.x - rect.left) / rect.width * 100).toFixed(2) + "%");
        pending.target.style.setProperty("--spot-y", ((pending.y - rect.top) / rect.height * 100).toFixed(2) + "%");
    }

    function onPointerMove(event) {
        if (event.pointerType === "touch") {
            return;
        }

        var target = event.target.closest ? event.target.closest("[data-spotlight]") : null;

        if (target !== spotlit) {
            if (spotlit) {
                spotlit.classList.remove("is-spotlit");
            }

            spotlit = target;

            if (spotlit) {
                spotlit.classList.add("is-spotlit");
            }
        }

        if (!target) {
            return;
        }

        var alreadyQueued = spotQueued !== null;
        spotQueued = { target: target, x: event.clientX, y: event.clientY };

        if (!alreadyQueued) {
            window.requestAnimationFrame(paintSpotlight);
        }
    }

    function clearSpotlight() {
        if (spotlit) {
            spotlit.classList.remove("is-spotlit");
            spotlit = null;
        }

        spotQueued = null;
    }

    // ── 6. Route entrance ───────────────────────────────────────────────────
    // Blazor's enhanced navigation patches the existing DOM instead of replacing
    // it, and a patched element never replays its CSS animation. Removing and
    // re-adding the attribute restarts it, which is the whole of the page
    // transition: the CSS owns what it looks like.
    function replayPageEnter() {
        var main = document.querySelector("[data-page-enter]");

        if (!main) {
            return;
        }

        main.removeAttribute("data-page-enter");
        // Reading a layout property between the two flushes the style change, so
        // the browser sees a genuine removal rather than a no-op.
        void main.offsetWidth;
        main.setAttribute("data-page-enter", "");
    }

    function onEnhancedLoad() {
        clearSpotlight();
        splitScope(document.body);
        scan(document.body);
        applyScrollState();
        replayPageEnter();
    }

    // ── Wiring ──────────────────────────────────────────────────────────────
    function start() {
        splitScope(document.body);
        scan(document.body);
        applyScrollState();
    }

    window.addEventListener("scroll", onScroll, { passive: true });

    if (root.classList.contains("hts-js")) {
        document.addEventListener("pointermove", onPointerMove, { passive: true });
        document.addEventListener("pointerleave", clearSpotlight, { passive: true });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", start, { once: true });
    } else {
        start();
    }

    // Enhanced navigation fires this after each same-document page swap.
    if (window.Blazor && typeof window.Blazor.addEventListener === "function") {
        window.Blazor.addEventListener("enhancedload", onEnhancedLoad);
    }

    // Blazor renders interactive islands after the initial paint, so pick up any
    // `[data-reveal]` nodes those components add to the DOM.
    if ("MutationObserver" in window) {
        new MutationObserver(function (mutations) {
            mutations.forEach(function (mutation) {
                mutation.addedNodes.forEach(function (node) {
                    if (node.nodeType === 1) {
                        splitScope(node);
                        scan(node);
                    }
                });
            });
        }).observe(document.documentElement, { childList: true, subtree: true });
    }
})();
