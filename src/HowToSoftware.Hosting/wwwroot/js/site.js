/*
 * HowToSoftware Hosting - progressive enhancement
 * ---------------------------------------------------------------------------
 * Deliberately small and dependency-free. Blazor and CSS own the interface;
 * this file only covers browser facts neither can observe on its own:
 *
 *   1. Reveal elements the first time they scroll into view.
 *   2. Flag the header once the page has scrolled past the top.
 *   3. Report which provisioning stage is centred in the viewport.
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

    function observe(node) {
        if (!observer || !node.hasAttribute || !node.hasAttribute("data-reveal")) {
            return;
        }

        if (node.classList.contains("is-revealed")) {
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
            scope.querySelectorAll("[data-reveal]:not(.is-revealed)").forEach(observe);
        }
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
        scan(document.body);
        applyScrollState();
        replayPageEnter();
    }

    // ── Wiring ──────────────────────────────────────────────────────────────
    function start() {
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
                        scan(node);
                    }
                });
            });
        }).observe(document.documentElement, { childList: true, subtree: true });
    }
})();
