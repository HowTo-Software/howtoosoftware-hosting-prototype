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

    // ── 3b. Order status ─────────────────────────────────────────────────────
    // The payment success page renders the order's state on the server and marks
    // its timeline with data-order-watch. This polls the status endpoint it names
    // and moves the marker; the server decides the state, this only draws it.
    // Nothing here touches Blazor - the page is static and the endpoint is JSON.
    var orderWatches = new Map();

    function paintTimeline(timeline, current, failed) {
        var steps = timeline.querySelectorAll("[data-index]");
        var last = steps.length - 1;

        steps.forEach(function (step) {
            var index = parseInt(step.getAttribute("data-index"), 10);
            step.classList.remove("is-done", "is-live", "is-idle", "is-fault");

            if (current < 0) {
                step.classList.add("is-idle");
            } else if (index < current) {
                step.classList.add("is-done");
            } else if (index === current) {
                step.classList.add(failed ? "is-fault" : (index === last ? "is-done" : "is-live"));
            } else {
                step.classList.add("is-idle");
            }
        });

        timeline.classList.toggle("is-waiting", current < 0);
        timeline.classList.toggle("is-failed", !!failed);
        timeline.setAttribute("data-current", String(current));
    }

    function watchOrders(scope) {
        scope.querySelectorAll("[data-order-watch]").forEach(function (timeline) {
            var url = timeline.getAttribute("data-order-watch");
            if (!url || orderWatches.has(timeline) || timeline.getAttribute("data-terminal") === "true") {
                return;
            }

            var lastStatus = null;
            var delay = 4000;

            function tick() {
                fetch(url, { headers: { "Accept": "application/json" }, cache: "no-store" })
                    .then(function (response) { return response.ok ? response.json() : null; })
                    .then(function (state) {
                        if (!state) {
                            return schedule();
                        }

                        paintTimeline(timeline, state.stageIndex, state.status === "Failed");

                        // The headline copy is rendered by the server for the state it saw. Once
                        // the state has moved on, one reload brings the copy in line with it.
                        if (lastStatus !== null && state.status !== lastStatus) {
                            stopWatching(timeline);
                            window.location.reload();
                            return;
                        }

                        lastStatus = state.status;

                        if (state.isTerminal) {
                            stopWatching(timeline);
                            return;
                        }

                        schedule();
                    })
                    .catch(schedule);
            }

            function schedule() {
                if (!orderWatches.has(timeline)) {
                    return;
                }

                orderWatches.set(timeline, window.setTimeout(tick, delay));
                delay = Math.min(delay + 1000, 15000);
            }

            orderWatches.set(timeline, window.setTimeout(tick, delay));
        });
    }

    function stopWatching(timeline) {
        var handle = orderWatches.get(timeline);
        if (handle) {
            window.clearTimeout(handle);
        }
        orderWatches.delete(timeline);
    }

    function stopAllWatches() {
        orderWatches.forEach(function (handle) { window.clearTimeout(handle); });
        orderWatches.clear();
    }

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

    // ── 5b. Navigation progress ─────────────────────────────────────────────
    //
    // Enhanced navigation swaps the document in place, which is fast and completely
    // silent: a visitor clicks, nothing appears to happen, and then the page is
    // different. The entrance animation alone cannot fix that, because it plays
    // *after* the wait rather than during it.
    //
    // So the bar covers the gap. It starts on the click, creeps while the request is
    // in flight, and completes when the new document lands. What it reports is real -
    // it begins and ends on actual navigation events - it just cannot know the
    // percentage, so it eases toward 90% and never claims to have arrived early.
    var progress = null;
    var progressTimer = null;
    var progressValue = 0;

    function ensureProgress() {
        if (progress) {
            return progress;
        }

        progress = document.createElement("div");
        progress.className = "hts-progress";
        progress.setAttribute("aria-hidden", "true");
        document.body.appendChild(progress);
        return progress;
    }

    function startProgress() {
        var bar = ensureProgress();

        window.clearInterval(progressTimer);
        progressValue = 8;
        bar.classList.remove("is-done");
        bar.classList.add("is-active");
        bar.style.setProperty("--progress", progressValue + "%");

        // Decelerating creep. Each tick closes a fraction of the remaining distance, so
        // it approaches 90 and never reaches it - a bar that hit 100 and then waited
        // would be lying about being finished.
        progressTimer = window.setInterval(function () {
            progressValue += (90 - progressValue) * 0.12;
            bar.style.setProperty("--progress", progressValue.toFixed(1) + "%");
        }, 180);
    }

    function finishProgress() {
        if (!progress) {
            return;
        }

        window.clearInterval(progressTimer);
        progress.style.setProperty("--progress", "100%");
        progress.classList.add("is-done");

        window.setTimeout(function () {
            if (progress) {
                progress.classList.remove("is-active", "is-done");
                progress.style.setProperty("--progress", "0%");
            }
        }, 320);
    }

    // Which clicks are about to become an enhanced navigation. Anything the browser
    // would handle itself - a new tab, a download, an external host, a modifier key -
    // is left alone, because showing progress for a navigation that never happens
    // leaves the bar stuck across the top of the page.
    function onDocumentClick(event) {
        if (event.defaultPrevented || event.button !== 0 ||
            event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
            return;
        }

        var link = event.target.closest ? event.target.closest("a[href]") : null;

        if (!link || link.target === "_blank" || link.hasAttribute("download")) {
            return;
        }

        var url;
        try {
            url = new URL(link.href, window.location.href);
        } catch (e) {
            return;
        }

        if (url.origin !== window.location.origin) {
            return;
        }

        // Same page, different anchor: the browser scrolls, it does not navigate.
        if (url.pathname === window.location.pathname && url.hash) {
            return;
        }

        startProgress();
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

    /*
        Enhanced navigation patches the document from the server's HTML, and that HTML has
        never heard of anything this file did. Every attribute set on <html> at runtime is
        reverted by the patch: the `hts-js` class that gates the entire progressive-
        enhancement layer, the `hts-scrolled` state the header reads, and the theme the
        visitor chose.

        The symptom was that the site worked correctly exactly once. After the first
        navigation the page-entrance animation stopped playing, the scroll reveals stopped
        hiding, the split-text effects stopped running and a chosen light theme snapped back
        to dark - because every selector behind them begins `html.hts-js`, and the class was
        no longer there.

        So the root's runtime state is re-derived on every navigation, before anything else
        looks at it.
    */
    function restoreRootState() {
        if (!prefersReducedMotion.matches && "IntersectionObserver" in window) {
            root.classList.add("hts-js");
        }

        try {
            var theme = localStorage.getItem("hts-theme");
            if (theme === "light" || theme === "dark") {
                root.setAttribute("data-theme", theme);
            }
        } catch (e) {
            // Private mode or storage disabled. The server-rendered default stands.
        }

        applyScrollState();
    }

    /*
        Restoring on `enhancedload` alone was not enough. Blazor patches the document and
        raises the event, but the two are not strictly ordered against every attribute it
        syncs - in practice the first navigation restored correctly and the second did not,
        because the patch landed after our handler had already run.

        Watching the element removes the question. Whenever something strips the runtime
        state off <html>, it goes straight back on. The observer only ever adds what is
        missing, so its own writes do not start a loop.
    */
    function watchRootState() {
        if (!("MutationObserver" in window)) {
            return;
        }

        new MutationObserver(function () {
            if (!root.classList.contains("hts-js")) {
                restoreRootState();
            }
        }).observe(root, { attributes: true, attributeFilter: ["class"] });
    }

    function onEnhancedLoad() {
        stopAllWatches();
        restoreRootState();
        finishProgress();
        clearSpotlight();
        splitScope(document.body);
        scan(document.body);
        watchOrders(document.body);
        replayPageEnter();
    }

    // ── Wiring ──────────────────────────────────────────────────────────────
    function start() {
        watchRootState();
        splitScope(document.body);
        scan(document.body);
        watchOrders(document.body);
        applyScrollState();
    }

    window.addEventListener("scroll", onScroll, { passive: true });

    if (root.classList.contains("hts-js")) {
        document.addEventListener("pointermove", onPointerMove, { passive: true });
        document.addEventListener("pointerleave", clearSpotlight, { passive: true });
        document.addEventListener("click", onDocumentClick, { capture: true, passive: true });

        // A navigation that ends in the browser going somewhere else, or in the back
        // button, still has to clear the bar - otherwise it survives into the page the
        // visitor lands on.
        window.addEventListener("pagehide", finishProgress);
        window.addEventListener("popstate", startProgress);
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
