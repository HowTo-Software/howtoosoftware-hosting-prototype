/*
 * HowToSoftware Hosting - progressive enhancement
 * ---------------------------------------------------------------------------
 * Deliberately small and dependency-free. Blazor and CSS own the interface;
 * this file only covers browser facts neither can observe on its own:
 *
 *   1. Reveal elements the first time they scroll into view.
 *   2. Flag the header once the page has scrolled past the top.
 *   3. Report which provisioning stage is centred in the viewport.
 *   4. Trigger CSS text masks without changing Razor's heading content.
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

    if (window.htsSiteInitialized) {
        return;
    }
    window.htsSiteInitialized = true;

    var root = document.documentElement;
    root.classList.add("hts-script");
    var prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
    var hoverPointer = window.matchMedia("(hover: hover) and (pointer: fine)");
    var visualFrame = null;
    var scrollDirty = false;
    var sceneDirty = false;

    if (!prefersReducedMotion.matches && "IntersectionObserver" in window) {
        root.classList.add("hts-js");
    }

    // ── 1. Scroll reveal ────────────────────────────────────────────────────
    var observer = null;
    var observedReveals = new WeakSet();

    if ("IntersectionObserver" in window) {
        observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) {
                    return;
                }

                entry.target.classList.add("is-revealed");
                observer.unobserve(entry.target);
                observedReveals.delete(entry.target);
            });
        }, { rootMargin: "0px 0px -10% 0px", threshold: 0.05 });
    }

    var REVEALS = "[data-reveal], [data-text-effect]";

    function observe(node) {
        if (!observer || !node.matches) {
            return;
        }

        // Text masks share the reveal class, including headings outside a reveal wrapper.
        if (!node.matches(REVEALS) || node.classList.contains("is-revealed")
            || observedReveals.has(node)) {
            return;
        }

        observedReveals.add(node);
        observer.observe(node);
    }

    function scan(scope) {
        if (!observer || !root.classList.contains("hts-js")) {
            return;
        }

        observe(scope);

        if (scope.querySelectorAll) {
            scope.querySelectorAll(REVEALS).forEach(observe);
        }
    }

    // ── 1c. Section choreography ──────────────────────────────────────────
    //
    // `data-reveal` is intentionally a small, individual entrance. Larger parts of the site
    // need a different grammar: label, line, title, copy, visual and then details assemble in
    // that order. Keeping that contract in one observer means pages declare *what* a piece is
    // instead of each page inventing a slightly different animation.
    //
    // No JavaScript, no hidden content: the hts-js gate remains the only start state.
    var motionObserver = null;
    var motionVisibilityObserver = null;
    var activeScenes = new Set();
    var activeParallax = new Set();
    var sceneStates = new WeakMap();
    var parallaxStates = new WeakMap();
    var countAnimations = new Map();

    if ("IntersectionObserver" in window) {
        motionObserver = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) {
                    return;
                }

                var section = entry.target;
                section.classList.add("is-motion-active");
                section.querySelectorAll("[data-count]").forEach(startCount);
                motionObserver.unobserve(section);
            });
        }, { rootMargin: "0px 0px -14% 0px", threshold: 0.08 });

        // Only sections near the viewport participate in continuous scroll work. This keeps
        // the finished motion identical while avoiding layout reads for the rest of the page.
        motionVisibilityObserver = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                var node = entry.target;
                node.classList.toggle("is-motion-near", entry.isIntersecting);

                if (node.hasAttribute("data-scene")) {
                    if (entry.isIntersecting) {
                        activeScenes.add(node);
                    } else {
                        activeScenes.delete(node);
                    }
                }

                if (node.hasAttribute("data-parallax")) {
                    if (entry.isIntersecting) {
                        activeParallax.add(node);
                    } else {
                        activeParallax.delete(node);
                    }
                }
            });

            scheduleScenes();
        }, { rootMargin: "30% 0px 30% 0px", threshold: 0 });
    }

    function startCount(node) {
        if (node.dataset.counted === "true" || prefersReducedMotion.matches) {
            return;
        }

        var countText = node.firstChild;
        if (!countText || countText.nodeType !== Node.TEXT_NODE) {
            return;
        }

        var target = Number(node.getAttribute("data-count"));
        if (!isFinite(target)) {
            return;
        }

        node.dataset.counted = "true";

        var decimals = Math.max(0, Math.min(2, Number(node.getAttribute("data-count-decimals")) || 0));
        var prefix = node.getAttribute("data-count-prefix") || "";
        var suffix = node.getAttribute("data-count-suffix") || "";
        var duration = 560;
        var started = null;
        var animation = { frame: null, finish: finish };

        function finish() {
            if (node.isConnected && countText.parentNode === node) {
                countText.nodeValue = prefix + target.toFixed(decimals) + suffix;
            }
            countAnimations.delete(node);
        }

        function paint(timestamp) {
            if (!node.isConnected || countText.parentNode !== node
                || document.hidden || prefersReducedMotion.matches) {
                finish();
                return;
            }

            if (started === null) {
                started = timestamp;
            }

            var progress = Math.min(1, (timestamp - started) / duration);
            // Fast at the start, then settles without a bounce.
            var eased = 1 - Math.pow(1 - progress, 4);
            countText.nodeValue = prefix + (target * eased).toFixed(decimals) + suffix;

            if (progress < 1) {
                animation.frame = window.requestAnimationFrame(paint);
            } else {
                countAnimations.delete(node);
            }
        }

        if (document.hidden) {
            finish();
            return;
        }

        countAnimations.set(node, animation);
        animation.frame = window.requestAnimationFrame(paint);
    }

    function observeParallax(node) {
        if (!motionVisibilityObserver || node.dataset.parallaxObserved === "true") {
            return;
        }

        node.dataset.parallaxObserved = "true";
        motionVisibilityObserver.observe(node);
    }

    function observeMotion(node) {
        if (!motionObserver || !node || node.dataset.motionObserved === "true") {
            return;
        }

        node.dataset.motionObserved = "true";
        motionObserver.observe(node);

        if (node.hasAttribute("data-scene") && node.dataset.sceneObserved !== "true") {
            node.dataset.sceneObserved = "true";
            motionVisibilityObserver.observe(node);
        }

    }

    function scanMotion(scope) {
        if (!motionObserver || !scope || !root.classList.contains("hts-js")) {
            return;
        }

        if (scope.matches && scope.matches("[data-motion]")) {
            observeMotion(scope);
        }
        if (scope.matches && scope.matches("[data-parallax]")) {
            observeParallax(scope);
        }

        if (scope.querySelectorAll) {
            scope.querySelectorAll("[data-motion]").forEach(observeMotion);
            scope.querySelectorAll("[data-parallax]").forEach(observeParallax);
        }
    }

    function paintScenes() {
        var viewport = window.innerHeight || 1;
        var writes = [];

        // Read every rectangle before changing styles or classes.
        activeScenes.forEach(function (scene) {
            if (!scene.isConnected) {
                activeScenes.delete(scene);
                return;
            }

            var rect = scene.getBoundingClientRect();
            var progress = Math.max(0, Math.min(1, (viewport - rect.top) / (viewport + rect.height)));
            writes.push({ node: scene, progress: progress.toFixed(3),
                leaving: rect.bottom < viewport * 0.48 && rect.bottom > 0 });
        });

        activeParallax.forEach(function (parallax) {
            if (!parallax.isConnected) {
                activeParallax.delete(parallax);
                return;
            }

            var rect = parallax.getBoundingClientRect();
            var intensity = Number(parallax.getAttribute("data-parallax")) || 12;
            var centerDistance = (rect.top + rect.height / 2 - viewport / 2) / viewport;
            var shift = Math.max(-intensity, Math.min(intensity, -centerDistance * intensity));
            writes.push({ node: parallax, shift: shift.toFixed(2) + "px" });
        });

        writes.forEach(function (write) {
            if (write.shift !== undefined) {
                if (parallaxStates.get(write.node) !== write.shift) {
                    write.node.style.setProperty("--motion-parallax-y", write.shift);
                    parallaxStates.set(write.node, write.shift);
                }
                return;
            }

            var previous = sceneStates.get(write.node);
            if (!previous || previous.progress !== write.progress) {
                write.node.style.setProperty("--motion-scene-progress", write.progress);
            }
            if (!previous || previous.leaving !== write.leaving) {
                write.node.classList.toggle("is-scene-leaving", write.leaving);
            }
            sceneStates.set(write.node, write);
        });
    }

    function paintVisualFrame() {
        visualFrame = null;
        if (document.hidden) {
            return;
        }

        var scrolled = scrollDirty ? window.scrollY > 12 : null;
        scrollDirty = false;
        if (sceneDirty && !prefersReducedMotion.matches) {
            paintScenes();
        }
        sceneDirty = false;
        if (scrolled !== null) {
            applyScrollState(scrolled);
        }
    }

    function queueVisualFrame() {
        if (visualFrame !== null || document.hidden) {
            return;
        }
        visualFrame = window.requestAnimationFrame(paintVisualFrame);
    }

    function scheduleScenes() {
        if (document.hidden || prefersReducedMotion.matches
            || (activeScenes.size === 0 && activeParallax.size === 0)) {
            return;
        }

        sceneDirty = true;
        queueVisualFrame();
    }

    // Razor owns text nodes. Replacing them breaks Blazor's separately cached logical
    // children during enhanced navigation, even when the visible DOM looks correct.
    // CSS masks animate the existing element; this module only changes reveal classes.
    // https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Components/Web.JS/src/Rendering/LogicalElements.ts
    // https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-10.0#interaction-with-the-dom

    // ── 2. Header scroll state ──────────────────────────────────────────────
    function applyScrollState(scrolled) {
        root.classList.toggle("hts-scrolled",
            typeof scrolled === "boolean" ? scrolled : window.scrollY > 12);
    }

    function onScroll() {
        scrollDirty = true;
        scheduleScenes();
        queueVisualFrame();
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
                if (!orderWatches.has(timeline) || !timeline.isConnected) {
                    stopWatching(timeline);
                    return;
                }
                fetch(url, { headers: { "Accept": "application/json" }, cache: "no-store" })
                    .then(function (response) { return response.ok ? response.json() : null; })
                    .then(function (state) {
                        if (!orderWatches.has(timeline) || !timeline.isConnected) {
                            return;
                        }
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
    var spotFrame = null;

    function paintSpotlight() {
        spotFrame = null;
        var pending = spotQueued;
        spotQueued = null;

        if (!pending || !pending.target.isConnected || document.hidden
            || prefersReducedMotion.matches || !hoverPointer.matches) {
            return;
        }

        var rect = pending.target.getBoundingClientRect();
        if (rect.width <= 0 || rect.height <= 0 || rect.bottom <= 0
            || rect.top >= window.innerHeight || rect.right <= 0 || rect.left >= window.innerWidth) {
            return;
        }
        pending.target.style.setProperty("--spot-x", ((pending.x - rect.left) / rect.width * 100).toFixed(2) + "%");
        pending.target.style.setProperty("--spot-y", ((pending.y - rect.top) / rect.height * 100).toFixed(2) + "%");
    }

    function onPointerMove(event) {
        if (event.pointerType === "touch" || document.hidden
            || prefersReducedMotion.matches || !hoverPointer.matches) {
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

        spotQueued = { target: target, x: event.clientX, y: event.clientY };

        if (spotFrame === null) {
            spotFrame = window.requestAnimationFrame(paintSpotlight);
        }
    }

    function clearSpotlight() {
        if (spotlit) {
            spotlit.classList.remove("is-spotlit");
            spotlit = null;
        }

        spotQueued = null;
        if (spotFrame !== null) {
            window.cancelAnimationFrame(spotFrame);
            spotFrame = null;
        }
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
    var progressFinishTimer = null;
    var progressValue = 0;

    function ensureProgress() {
        if (progress && progress.isConnected) {
            return progress;
        }

        progress = document.querySelector("[data-navigation-progress]");
        return progress;
    }

    function stopProgressTimer() {
        window.clearInterval(progressTimer);
        progressTimer = null;
    }

    function startProgressTimer() {
        if (progressTimer !== null || document.hidden || prefersReducedMotion.matches) {
            return;
        }

        progressTimer = window.setInterval(function () {
            if (!progress || !progress.isConnected) {
                stopProgressTimer();
                return;
            }
            progressValue += (90 - progressValue) * 0.12;
            progress.style.setProperty("--progress", progressValue.toFixed(1) + "%");
        }, 180);
    }

    function startProgress() {
        var bar = ensureProgress();
        if (!bar) {
            return;
        }

        stopProgressTimer();
        window.clearTimeout(progressFinishTimer);
        progressFinishTimer = null;
        progressValue = 8;
        bar.classList.remove("is-done");
        bar.classList.add("is-active");
        bar.style.setProperty("--progress", progressValue + "%");

        // Decelerating creep. Each tick closes a fraction of the remaining distance, so
        // it approaches 90 and never reaches it - a bar that hit 100 and then waited
        // would be lying about being finished.
        startProgressTimer();
    }

    function finishProgress() {
        stopProgressTimer();
        if (!progress) {
            return;
        }

        window.clearTimeout(progressFinishTimer);
        progress.style.setProperty("--progress", "100%");
        progress.classList.add("is-done");

        progressFinishTimer = window.setTimeout(function () {
            progressFinishTimer = null;
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
        if (document.hidden || prefersReducedMotion.matches) {
            return;
        }

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
        root.classList.add("hts-script");
        if (!prefersReducedMotion.matches && "IntersectionObserver" in window) {
            root.classList.add("hts-js");
        } else {
            root.classList.remove("hts-js");
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
            if (!root.classList.contains("hts-script")
                || (!prefersReducedMotion.matches && "IntersectionObserver" in window
                    && !root.classList.contains("hts-js"))) {
                restoreRootState();
            }
        }).observe(root, { attributes: true, attributeFilter: ["class"] });
    }

    function onEnhancedLoad() {
        stopAllWatches();
        clearMutationQueue();
        pruneDetachedScopes();
        sceneStates = new WeakMap();
        parallaxStates = new WeakMap();
        restoreRootState();
        finishProgress();
        clearSpotlight();
        activeScenes.forEach(function (node) {
            if (!node.isConnected) {
                activeScenes.delete(node);
            }
        });
        activeParallax.forEach(function (node) {
            if (!node.isConnected) {
                activeParallax.delete(node);
            }
        });
        scan(document.body);
        scanMotion(document.body);
        scheduleScenes();
        watchOrders(document.body);
        replayPageEnter();
    }

    function pauseVisualWork() {
        if (visualFrame !== null) {
            window.cancelAnimationFrame(visualFrame);
            visualFrame = null;
        }
        clearSpotlight();
        stopProgressTimer();
        countAnimations.forEach(function (animation) {
            window.cancelAnimationFrame(animation.frame);
            animation.finish();
        });
        if (mutationFrame !== null) {
            window.cancelAnimationFrame(mutationFrame);
            mutationFrame = null;
        }
    }

    function resumeVisualWork() {
        onScroll();
        queueMutationScan();
        if (progress && progress.isConnected && progress.classList.contains("is-active")
            && !progress.classList.contains("is-done")) {
            startProgressTimer();
        }
    }

    function onMotionPreferenceChange() {
        restoreRootState();
        if (prefersReducedMotion.matches) {
            pauseVisualWork();
            clearMutationQueue();
            return;
        }
        scan(document.body);
        scanMotion(document.body);
        resumeVisualWork();
    }

    // ── Wiring ──────────────────────────────────────────────────────────────
    function start() {
        watchRootState();
        scan(document.body);
        scanMotion(document.body);
        scheduleScenes();
        watchOrders(document.body);
        applyScrollState();
    }

    window.addEventListener("scroll", onScroll, { passive: true });
    window.addEventListener("resize", onScroll, { passive: true });
    document.addEventListener("pointermove", onPointerMove, { passive: true });
    document.addEventListener("pointerleave", clearSpotlight, { passive: true });
    document.addEventListener("click", onDocumentClick, { capture: true, passive: true });
    window.addEventListener("pagehide", function () {
        pauseVisualWork();
        finishProgress();
    });
    window.addEventListener("pageshow", resumeVisualWork);
    window.addEventListener("popstate", startProgress);
    document.addEventListener("visibilitychange", function () {
        if (document.hidden) {
            pauseVisualWork();
        } else {
            resumeVisualWork();
        }
    });
    if (typeof prefersReducedMotion.addEventListener === "function") {
        prefersReducedMotion.addEventListener("change", onMotionPreferenceChange);
        hoverPointer.addEventListener("change", clearSpotlight);
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

    var mutationRoots = new Set();
    var mutationFrame = null;
    var ENHANCEMENTS = REVEALS + ", [data-motion], [data-parallax]";

    function visitScope(scope, selector, visit) {
        if (scope.matches && scope.matches(selector)) {
            visit(scope);
        }
        if (scope.querySelectorAll) {
            scope.querySelectorAll(selector).forEach(visit);
        }
    }

    function releaseScope(scope) {
        if (scope.isConnected) {
            return;
        }
        visitScope(scope, ENHANCEMENTS, function (node) {
            if (observer) {
                observer.unobserve(node);
                observedReveals.delete(node);
            }
            if (motionObserver) {
                motionObserver.unobserve(node);
                motionVisibilityObserver.unobserve(node);
            }
            activeScenes.delete(node);
            activeParallax.delete(node);
            sceneStates.delete(node);
            parallaxStates.delete(node);
            delete node.dataset.motionObserved;
            delete node.dataset.sceneObserved;
            delete node.dataset.parallaxObserved;
        });
        countAnimations.forEach(function (animation, node) {
            if (!node.isConnected) {
                window.cancelAnimationFrame(animation.frame);
                animation.finish();
            }
        });
    }

    function pruneDetachedScopes() {
        stageScopes.forEach(function (stageObserver, scope) {
            if (!scope.isConnected) {
                stageObserver.disconnect();
                stageScopes.delete(scope);
            }
        });
        orderWatches.forEach(function (_, timeline) {
            if (!timeline.isConnected) {
                stopWatching(timeline);
            }
        });
    }

    function clearMutationQueue() {
        if (mutationFrame !== null) {
            window.cancelAnimationFrame(mutationFrame);
            mutationFrame = null;
        }
        mutationRoots.clear();
    }

    function flushMutationScan() {
        mutationFrame = null;
        if (document.hidden) {
            return;
        }
        mutationRoots.forEach(function (node) {
            if (node.isConnected) {
                scan(node);
                scanMotion(node);
            }
        });
        mutationRoots.clear();
        scheduleScenes();
    }

    function queueMutationScan() {
        if (!root.classList.contains("hts-js")) {
            clearMutationQueue();
            return;
        }
        if (!document.hidden && mutationRoots.size > 0 && mutationFrame === null) {
            mutationFrame = window.requestAnimationFrame(flushMutationScan);
        }
    }

    function queueMutationRoot(node) {
        if (!node.matches(ENHANCEMENTS) && !node.querySelector(ENHANCEMENTS)) {
            return;
        }
        var covered = false;
        mutationRoots.forEach(function (existing) {
            if (existing.contains(node)) {
                covered = true;
            } else if (node.contains(existing)) {
                mutationRoots.delete(existing);
            }
        });
        if (!covered) {
            mutationRoots.add(node);
        }
    }

    // Scan only new enhancement roots and keep Razor's child-node structure intact.
    if ("MutationObserver" in window) {
        new MutationObserver(function (mutations) {
            var removed = false;
            mutations.forEach(function (mutation) {
                mutation.removedNodes.forEach(function (node) {
                    if (node.nodeType === 1 && !node.isConnected) {
                        releaseScope(node);
                        removed = true;
                    }
                });
                mutation.addedNodes.forEach(function (node) {
                    if (node.nodeType === 1) {
                        queueMutationRoot(node);
                    }
                });
            });
            if (removed) {
                pruneDetachedScopes();
            }
            queueMutationScan();
        }).observe(document.documentElement, { childList: true, subtree: true });
    }
})();
