/*
 * Vector Wordmark — Originkit.
 * Adapted from the source supplied by the HTS project owner for this application.
 * The original GLSL, red/green glyph atlas, grid snapping and drift equations are retained.
 * React effects are replaced with a bounded, visibility-aware DOM lifecycle.
 */
(function () {
    "use strict";
    if (window.htsVectorWordmarkInitialized) return;
    window.htsVectorWordmarkInitialized = true;

    var VERT = "\nattribute vec2 aPos;\nvarying vec2 vUv;\nvoid main() {\n    vUv = aPos * 0.5 + 0.5;\n    gl_Position = vec4(aPos, 0.0, 1.0);\n}";
    var FRAG = "\nprecision highp float;\n\nuniform sampler2D uMap;\nuniform vec2 uRes;\nuniform vec2 uAtlas;\nuniform vec2 uPtr;\nuniform float uReach;\nuniform vec3 uText;\nuniform vec3 uShade;\nuniform vec4 uAccent;\nuniform vec2 uV0;\nuniform vec2 uV1;\nuniform vec2 uV2;\nuniform float uHalf;\n\nvarying vec2 vUv;\n\nfloat hash(vec2 p) {\n    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);\n}\n\nvec2 blurRG(vec2 uv, float e) {\n    vec4 sum = vec4(0.0);\n    for (int i = 0; i < 6; i++) {\n        float fi = float(i);\n        float th = radians(fi / 6.0 * 360.0);\n        vec2 dir = vec2(cos(th), sin(th));\n        vec2 off = dir * (hash(vec2(fi, uv.x + uv.y)) + e);\n        sum += texture2D(uMap, uv + off * e);\n    }\n    return (sum / 6.0).rg;\n}\n\nvec2 segment(vec2 p, vec2 a, vec2 b) {\n    vec2 ab = b - a;\n    vec2 ap = p - a;\n    float t = clamp(dot(ap, ab) / max(dot(ab, ab), 1e-8), 0.0, 1.0);\n    return vec2(length(ap - ab * t), t);\n}\n\nfloat stroke(float d, float lw, float px) {\n    return 1.0 - smoothstep(lw, lw + px, d);\n}\n\nfloat dashedLine(vec2 p, vec2 a, vec2 b, float lw, float px) {\n    vec2 s = segment(p, a, b);\n    float dash = step(0.5, fract(s.y * length(b - a) * 100.0));\n    return stroke(s.x, lw, px) * dash;\n}\n\nfloat boxEdge(vec2 p, vec2 c, float h, float lw, float px) {\n    vec2 q = abs(p - c) - vec2(h);\n    float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);\n    return stroke(abs(d), lw, px);\n}\n\nvoid main() {\n    float aspect = uRes.x / uRes.y;\n\n    vec2 E = (vUv * uRes - (uRes - uAtlas) * 0.5) / uAtlas;\n    float inside = step(0.0, E.x) * step(E.x, 1.0) * step(0.0, E.y) * step(E.y, 1.0);\n    vec2 safeUv = clamp(E, 0.0, 1.0);\n\n    float b = clamp(1.0 - E.y * 3.5, 0.0, 1.0) * 0.008;\n    vec2 soft = blurRG(safeUv, b);\n    vec2 sharp = blurRG(safeUv, b * 0.1);\n\n    float d = length((vUv - uPtr) / vec2(1.0, aspect));\n    float k = 1.0 - pow(smoothstep(0.0, max(uReach, 1e-4), d), 3.0);\n\n    float mask = mix(soft.r, sharp.g, k) * inside;\n    vec3 fill = mix(uShade, uText, smoothstep(0.0, 1.0, E.y));\n\n    vec2 P = vec2(vUv.x * aspect, vUv.y);\n    float px = 1.0 / uRes.y;\n    float lw = px * 0.2;\n    float lines = max(\n        max(dashedLine(P, uV0, uV1, lw, px), dashedLine(P, uV1, uV2, lw, px)),\n        dashedLine(P, uV2, uV0, lw, px)\n    );\n    float boxes = max(\n        max(boxEdge(P, uV0, uHalf, lw, px), boxEdge(P, uV1, uHalf, lw, px)),\n        boxEdge(P, uV2, uHalf, lw, px)\n    );\n    float A = max(lines, boxes) * uAccent.a * (1.0 - vUv.y);\n\n    vec4 card = vec4(fill * mask, mask);\n    vec4 comp = vec4(uAccent.rgb * A, A) + card * (1.0 - A);\n\n    gl_FragColor = comp * pow(clamp(E.y, 0.0, 1.0), 0.7);\n}";

    var REF_WIDTH = 1200;
    var HANDLES = 3;
    var CELL_ASPECT = 0.6;
    var DRIFT_X = 0.08;
    var DRIFT_Y = 0.04;
    var DRIFT_RATE = 1.3;
    var DRIFT_RATE_Y = 1.3 * 1.3;
    var SWEEP_RATE = 0.5;
    var SWEEP_BAND = 0.28;
    var RESNAP = 0.2;
    var DAMP_REF = 20;
    var DOT_DIAMETER = 4 / 440;
    var DOT_PITCH = 12 / 440;
    var INTRO_SECONDS = 4.4;
    var selector = "[data-vector-wordmark]";
    var instances = new Map();
    var reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
    var mobileViewport = window.matchMedia("(max-width: 700px), (pointer: coarse)");
    var root = document.documentElement;

    function clamp(value, min, max) {
        return value < min ? min : value > max ? max : value;
    }

    function fract(value) {
        return value - Math.floor(value);
    }

    function parseColor(input, fallback) {
        var value = String(input || "").trim();
        if (value.charAt(0) === "#") {
            var hex = value.slice(1);
            if (hex.length === 3 || hex.length === 4) {
                hex = hex.split("").map(function (character) { return character + character; }).join("");
            }
            if (hex.length === 6) hex += "ff";
            if (hex.length === 8 && !/[^0-9a-f]/i.test(hex)) {
                return [0, 2, 4, 6].map(function (offset) {
                    return parseInt(hex.slice(offset, offset + 2), 16) / 255;
                });
            }
        }
        var match = value.match(/^(rgba?|hsla?)\(([^)]*)\)$/i);
        if (!match) return fallback;
        var parts = match[2].split(/[\s,/]+/).filter(Boolean);
        if (parts.length < 3) return fallback;
        function number(part, scale) {
            var parsed = parseFloat(part);
            return Number.isFinite(parsed) ? (part.indexOf("%") >= 0 ? parsed * scale / 100 : parsed) : 0;
        }
        var alpha = parts.length > 3 ? clamp(number(parts[3], 1), 0, 1) : 1;
        if (match[1].slice(0, 3).toLowerCase() === "rgb") {
            return [clamp(number(parts[0], 255) / 255, 0, 1),
                clamp(number(parts[1], 255) / 255, 0, 1),
                clamp(number(parts[2], 255) / 255, 0, 1), alpha];
        }
        var hue = fract(parseFloat(parts[0]) / 360);
        var saturation = clamp(number(parts[1], 1), 0, 1);
        var lightness = clamp(number(parts[2], 1), 0, 1);
        var q = lightness < 0.5 ? lightness * (1 + saturation)
            : lightness + saturation - lightness * saturation;
        var p = 2 * lightness - q;
        function channel(t) {
            var normalized = fract(t);
            if (normalized < 1 / 6) return p + (q - p) * 6 * normalized;
            if (normalized < 1 / 2) return q;
            if (normalized < 2 / 3) return p + (q - p) * (2 / 3 - normalized) * 6;
            return p;
        }
        return [channel(hue + 1 / 3), channel(hue), channel(hue - 1 / 3), alpha];
    }

    function compileProgram(gl) {
        var vertex = null;
        var fragment = null;
        var program = null;
        function shader(type, source) {
            var handle = gl.createShader(type);
            if (!handle) return null;
            gl.shaderSource(handle, source);
            gl.compileShader(handle);
            if (!gl.getShaderParameter(handle, gl.COMPILE_STATUS)) {
                gl.deleteShader(handle);
                return null;
            }
            return handle;
        }
        try {
            vertex = shader(gl.VERTEX_SHADER, VERT);
            var precision = gl.getShaderPrecisionFormat(gl.FRAGMENT_SHADER, gl.HIGH_FLOAT);
            fragment = shader(gl.FRAGMENT_SHADER, precision && precision.precision > 0
                ? FRAG : FRAG.replace("precision highp float;", "precision mediump float;"));
            if (!vertex || !fragment) return null;
            program = gl.createProgram();
            if (!program) return null;
            gl.attachShader(program, vertex);
            gl.attachShader(program, fragment);
            gl.bindAttribLocation(program, 0, "aPos");
            gl.linkProgram(program);
            if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
                gl.deleteProgram(program);
                program = null;
            }
            return program;
        } finally {
            if (vertex) {
                if (program) gl.detachShader(program, vertex);
                gl.deleteShader(vertex);
            }
            if (fragment) {
                if (program) gl.detachShader(program, fragment);
                gl.deleteShader(fragment);
            }
        }
    }

    function fontString(font, pixels) {
        return font.style + " " + font.weight + " " + pixels + "px " + font.family;
    }

    function buildAtlas(text, font, drawFontPx, dpr, maxTextureSize) {
        var probe = document.createElement("canvas").getContext("2d");
        if (!probe) return null;
        function setFont(context, pixels) {
            context.font = fontString(font, pixels);
            if ("letterSpacing" in context) {
                try { context.letterSpacing = font.letterSpacing; } catch (_) { }
            }
        }
        function measure(pixels) {
            setFont(probe, pixels);
            var metrics = probe.measureText(text);
            return { width: Math.max(1, metrics.width),
                ascent: metrics.actualBoundingBoxAscent || pixels * 0.8,
                descent: Number.isFinite(metrics.actualBoundingBoxDescent)
                    ? Math.max(0, metrics.actualBoundingBoxDescent) : pixels * 0.22 };
        }
        var fontPixels = Math.max(8, drawFontPx * dpr);
        var metrics = measure(fontPixels);
        var padding = fontPixels * 0.12;
        var overflow = Math.max((metrics.width + padding * 2) / maxTextureSize,
            (metrics.ascent + metrics.descent + padding * 2) / maxTextureSize);
        if (overflow > 1) {
            fontPixels = Math.max(8, fontPixels / overflow);
            metrics = measure(fontPixels);
            padding = fontPixels * 0.12;
        }
        var atlas = document.createElement("canvas");
        atlas.width = Math.max(1, Math.ceil(metrics.width + padding * 2));
        atlas.height = Math.max(1, Math.ceil(metrics.ascent + metrics.descent + padding * 2));
        var context = atlas.getContext("2d");
        if (!context) return null;
        context.fillStyle = "#000000";
        context.fillRect(0, 0, atlas.width, atlas.height);
        setFont(context, fontPixels);
        context.textBaseline = "alphabetic";
        context.textAlign = "left";
        context.globalCompositeOperation = "lighter";
        context.fillStyle = "#ff0000";
        context.fillText(text, padding, padding + metrics.ascent);
        var block = metrics.ascent + metrics.descent;
        context.strokeStyle = "#00ff00";
        context.lineCap = "round";
        context.lineJoin = "round";
        context.lineWidth = Math.max(1, block * DOT_DIAMETER);
        context.setLineDash([0, Math.max(2, block * DOT_PITCH)]);
        context.strokeText(text, padding, padding + metrics.ascent);
        var cssPerPixel = drawFontPx / fontPixels;
        return { canvas: atlas, cssWidth: atlas.width * cssPerPixel, cssHeight: atlas.height * cssPerPixel };
    }

    function createWordmark(host) {
        var canvas = host.querySelector(".wordmark-canvas");
        var labels = host.querySelectorAll(".wordmark-coordinate");
        if (!canvas) return null;
        var alive = true;
        var failed = false;
        var visible = !("IntersectionObserver" in window);
        var gl = null;
        var program = null;
        var texture = null;
        var quad = null;
        var uniforms = null;
        var maxTextureSize = 4096;
        var rect = host.getBoundingClientRect();
        var width = Math.max(1, rect.width);
        var height = Math.max(1, rect.height);
        var dpr = 1;
        var font = null;
        var textColor = null;
        var shadeColor = null;
        var accentColor = null;
        var atlasWidth = 1;
        var atlasHeight = 1;
        var paletteDirty = true;
        var sizeDirty = true;
        var atlasDirty = true;
        var needsDraw = true;
        var hasPointer = false;
        var pointerDirty = false;
        var pointerX = 0;
        var pointerY = 0;
        var frameHandle = 0;
        var lastFrame = 0;
        var remaining = INTRO_SECONDS;
        var sweepClock = 0;
        var driftTime = 0;
        var target = { x: -0.5, y: 0.5 };
        var eased = { x: -0.5, y: 0.5 };
        var cells = [];
        var vertices = [];
        var lastLabels = [];
        var text = host.getAttribute("data-wordmark-text") || "HTS";
        var referenceFontSize = Number(host.getAttribute("data-wordmark-font-size")) || 500;
        for (var i = 0; i < HANDLES; i += 1) {
            cells.push({ x: -0.5, y: 0.5 });
            vertices.push({ x: -0.5, y: 0.5 });
        }

        function stop() {
            if (frameHandle) window.cancelAnimationFrame(frameHandle);
            frameHandle = 0;
            lastFrame = 0;
        }

        function showFallback() {
            host.classList.remove("is-ready");
            labels.forEach(function (label) { label.style.opacity = "0"; });
        }

        function releaseGpu(loseContext) {
            if (!gl) return;
            if (texture) gl.deleteTexture(texture);
            if (quad) gl.deleteBuffer(quad);
            if (program) gl.deleteProgram(program);
            if (loseContext) {
                var extension = gl.getExtension("WEBGL_lose_context");
                if (extension) extension.loseContext();
            }
            texture = null;
            quad = null;
            program = null;
            uniforms = null;
            gl = null;
        }

        function initializeGpu() {
            if (gl) return true;
            try {
                gl = canvas.getContext("webgl", {
                    alpha: true, antialias: false, depth: false, stencil: false,
                    premultipliedAlpha: true, powerPreference: "low-power"
                });
                if (!gl) return false;
                program = compileProgram(gl);
                if (!program) {
                    releaseGpu(false);
                    return false;
                }
                uniforms = {};
                ["Map", "Res", "Atlas", "Ptr", "Reach", "Text", "Shade", "Accent",
                    "V0", "V1", "V2", "Half"].forEach(function (name) {
                    uniforms[name] = gl.getUniformLocation(program, "u" + name);
                });
                quad = gl.createBuffer();
                texture = gl.createTexture();
                if (!quad || !texture) {
                    releaseGpu(false);
                    return false;
                }
                gl.bindBuffer(gl.ARRAY_BUFFER, quad);
                gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]), gl.STATIC_DRAW);
                gl.enableVertexAttribArray(0);
                gl.vertexAttribPointer(0, 2, gl.FLOAT, false, 0, 0);
                gl.disable(gl.BLEND);
                gl.bindTexture(gl.TEXTURE_2D, texture);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
                maxTextureSize = Math.min(4096, gl.getParameter(gl.MAX_TEXTURE_SIZE));
                return true;
            } catch (_) {
                releaseGpu(false);
                return false;
            }
        }

        function drawFontPx() {
            return Math.max(8, referenceFontSize * width / REF_WIDTH);
        }

        function refreshPalette() {
            var style = window.getComputedStyle(host);
            font = { family: style.fontFamily || "Archivo, sans-serif",
                weight: style.fontWeight || "750", style: style.fontStyle || "normal",
                letterSpacing: "-0.025em" };
            textColor = parseColor(style.getPropertyValue("--ht-ink"), [0.9, 0.91, 0.93, 1]);
            shadeColor = parseColor(style.getPropertyValue("--ht-ink-muted"), [0.3, 0.32, 0.36, 1]);
            accentColor = parseColor(style.getPropertyValue("--ht-accent"), textColor.slice());
            accentColor[3] = Math.min(accentColor[3], 0.55);
            paletteDirty = false;
        }

        function sync() {
            if (paletteDirty) refreshPalette();
            if (sizeDirty) {
                dpr = Math.min(mobileViewport.matches ? 1 : 1.5, window.devicePixelRatio || 1);
                var bufferWidth = Math.max(1, Math.round(width * dpr));
                var bufferHeight = Math.max(1, Math.round(height * dpr));
                if (canvas.width !== bufferWidth || canvas.height !== bufferHeight) {
                    canvas.width = bufferWidth;
                    canvas.height = bufferHeight;
                }
                sizeDirty = false;
            }
            if (atlasDirty) {
                var pixels = drawFontPx();
                var atlas = buildAtlas(text, font, pixels, dpr, maxTextureSize);
                if (!atlas) return false;
                atlasWidth = atlas.cssWidth;
                atlasHeight = atlas.cssHeight;
                gl.bindTexture(gl.TEXTURE_2D, texture);
                gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, true);
                gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, atlas.canvas);
                gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, false);
                atlasDirty = false;
            }
            return true;
        }

        function snap(x, y, cellWidth, cellHeight) {
            var cx = Math.floor(x / cellWidth);
            var cy = Math.floor(y / cellHeight);
            var nearby = [];
            for (var i = -1; i <= 1; i += 1) {
                for (var j = -1; j <= 1; j += 1) {
                    var px = (cx + i + 0.5) * cellWidth;
                    var py = (cy + j + 0.5) * cellHeight;
                    nearby.push({ x: px, y: py, distance: Math.hypot(px - x, py - y) });
                }
            }
            nearby.sort(function (a, b) { return a.distance - b.distance; });
            for (var handle = 0; handle < HANDLES; handle += 1) {
                cells[handle].x = nearby[handle + 1].x;
                cells[handle].y = nearby[handle + 1].y;
            }
        }

        function step(dt) {
            var cellWidth = 0.27;
            var cellHeight = cellWidth * CELL_ASPECT;
            var aspect = width / height;
            if (pointerDirty) {
                var bounds = host.getBoundingClientRect();
                if (bounds.width > 0 && bounds.height > 0) {
                    target.x = clamp((pointerX - bounds.left) / bounds.width, 0, 1);
                    target.y = 1 - clamp((pointerY - bounds.top) / bounds.height, 0, 1);
                }
                pointerDirty = false;
            }
            if (!hasPointer) {
                var band = atlasHeight / height;
                target.x += dt * SWEEP_RATE;
                target.y = (1 - band) / 2 + SWEEP_BAND * band;
                if (target.x > 1.5) {
                    target.x = -0.5;
                    eased.x = -0.5;
                }
                sweepClock += dt;
                if (sweepClock >= RESNAP) {
                    sweepClock = 0;
                    snap(target.x * aspect, target.y, cellWidth, cellHeight);
                }
            } else {
                snap(target.x * aspect, target.y, cellWidth, cellHeight);
            }
            var damping = clamp(0.6 * DAMP_REF * dt, 0, 1);
            eased.x += (target.x - eased.x) * damping;
            eased.y += (target.y - eased.y) * damping;
            driftTime += dt;
            for (var handle = 0; handle < HANDLES; handle += 1) {
                var cell = cells[handle];
                var sx = Math.round(cell.x / cellWidth - 0.5);
                var sy = Math.round(cell.y / cellHeight - 0.5);
                var hashX = fract(Math.sin(sx * 127.1 + sy * 311.7) * 43758.5453);
                var hashY = fract(Math.sin(sx * 269.5 + sy * 183.3) * 43758.5453);
                vertices[handle].x = cell.x + DRIFT_X * cellWidth *
                    Math.sin(driftTime * DRIFT_RATE + hashX * Math.PI * 2);
                vertices[handle].y = cell.y + DRIFT_Y * cellHeight *
                    Math.sin(driftTime * DRIFT_RATE_Y + hashY * Math.PI * 2);
            }
        }

        function writeLabels() {
            var aspect = width / height;
            var half = 109 * width / REF_WIDTH / 2;
            labels.forEach(function (label, index) {
                var x = vertices[index].x / aspect;
                var y = vertices[index].y;
                var coordinate = Math.round(clamp(x * 100, 0, 100)) + ", " +
                    Math.round(clamp(y * 100, 0, 100));
                label.style.transform = "translate(" + (x * width - half).toFixed(2) + "px, " +
                    ((1 - y) * height - half).toFixed(2) + "px)";
                label.style.opacity = x > 0 && x < 1 && y > 0 && y < 1 ? "0.6" : "0";
                if (lastLabels[index] !== coordinate) {
                    // Keep the SSR text node: Blazor tracks its identity during page updates.
                    if (label.firstChild && label.firstChild.nodeType === 3) {
                        label.firstChild.nodeValue = coordinate;
                    }
                    lastLabels[index] = coordinate;
                }
            });
        }

        function draw() {
            gl.viewport(0, 0, canvas.width, canvas.height);
            gl.useProgram(program);
            gl.uniform1i(uniforms.Map, 0);
            gl.activeTexture(gl.TEXTURE0);
            gl.bindTexture(gl.TEXTURE_2D, texture);
            gl.uniform2f(uniforms.Res, width, height);
            gl.uniform2f(uniforms.Atlas, atlasWidth, atlasHeight);
            gl.uniform2f(uniforms.Ptr, eased.x, eased.y);
            gl.uniform1f(uniforms.Reach, 290 / REF_WIDTH);
            gl.uniform3f(uniforms.Text, textColor[0], textColor[1], textColor[2]);
            gl.uniform3f(uniforms.Shade, shadeColor[0], shadeColor[1], shadeColor[2]);
            gl.uniform4f(uniforms.Accent, accentColor[0], accentColor[1], accentColor[2], accentColor[3]);
            gl.uniform2f(uniforms.V0, vertices[0].x, vertices[0].y);
            gl.uniform2f(uniforms.V1, vertices[1].x, vertices[1].y);
            gl.uniform2f(uniforms.V2, vertices[2].x, vertices[2].y);
            gl.uniform1f(uniforms.Half, 109 * width / REF_WIDTH / 2 / height);
            gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
            writeLabels();
            host.classList.add("is-ready");
        }

        function frame(now) {
            frameHandle = 0;
            if (!canRun()) return;
            var interval = 1000 / 30;
            if (lastFrame && now - lastFrame < interval - 1) {
                frameHandle = window.requestAnimationFrame(frame);
                return;
            }
            var dt = lastFrame ? Math.min(0.1, (now - lastFrame) / 1000) : 0;
            lastFrame = now;
            try {
                if (!initializeGpu() || !sync()) {
                    failed = true;
                    showFallback();
                    releaseGpu(false);
                    return;
                }
                step(dt);
                draw();
            } catch (_) {
                failed = true;
                showFallback();
                releaseGpu(false);
                return;
            }
            remaining = Math.max(0, remaining - dt);
            needsDraw = false;
            if (remaining > 0 || pointerDirty) {
                frameHandle = window.requestAnimationFrame(frame);
            } else {
                lastFrame = 0;
            }
        }

        function canRun() {
            return alive && !failed && visible && host.isConnected && !document.hidden && !reducedMotion.matches;
        }

        function wake() {
            if (canRun() && !frameHandle && (needsDraw || remaining > 0)) {
                frameHandle = window.requestAnimationFrame(frame);
            }
        }

        function replay() {
            if (reducedMotion.matches) return;
            hasPointer = false;
            pointerDirty = false;
            target.x = -0.5;
            eased.x = -0.5;
            remaining = INTRO_SECONDS;
            needsDraw = true;
            wake();
        }

        function onPointerMove(event) {
            if (event.pointerType === "touch" || reducedMotion.matches) return;
            hasPointer = true;
            pointerDirty = true;
            pointerX = event.clientX;
            pointerY = event.clientY;
            remaining = 1.2;
            needsDraw = true;
            wake();
        }

        function onPointerLeave() {
            if (hasPointer) replay();
        }

        function onContextLost(event) {
            event.preventDefault();
            stop();
            showFallback();
            releaseGpu(false);
            failed = true;
        }

        function onContextRestored() {
            failed = false;
            sizeDirty = true;
            atlasDirty = true;
            needsDraw = true;
            wake();
        }

        var resizeObserver = null;
        if ("ResizeObserver" in window) {
            resizeObserver = new ResizeObserver(function (entries) {
                var box = entries[0].contentRect;
                if (box.width <= 0 || box.height <= 0 || (width === box.width && height === box.height)) return;
                width = box.width;
                height = box.height;
                sizeDirty = true;
                atlasDirty = true;
                needsDraw = true;
                wake();
            });
            resizeObserver.observe(host);
        }
        host.addEventListener("pointermove", onPointerMove, { passive: true });
        host.addEventListener("pointerleave", onPointerLeave, { passive: true });
        canvas.addEventListener("webglcontextlost", onContextLost);
        canvas.addEventListener("webglcontextrestored", onContextRestored);

        return {
            isValid: function () {
                return host.matches(selector) && host.querySelector(".wordmark-canvas") === canvas;
            },
            visibility: function (isVisible) {
                visible = isVisible;
                if (visible) wake(); else stop();
            },
            pause: stop,
            refresh: function (rebuildFont) {
                if (rebuildFont) {
                    atlasDirty = true;
                    paletteDirty = true;
                }
                sizeDirty = true;
                needsDraw = true;
                if (reducedMotion.matches) {
                    stop();
                    showFallback();
                } else {
                    wake();
                }
            },
            resume: wake,
            dispose: function () {
                alive = false;
                stop();
                if (resizeObserver) resizeObserver.disconnect();
                host.removeEventListener("pointermove", onPointerMove);
                host.removeEventListener("pointerleave", onPointerLeave);
                canvas.removeEventListener("webglcontextlost", onContextLost);
                canvas.removeEventListener("webglcontextrestored", onContextRestored);
                releaseGpu(!canvas.isConnected);
                showFallback();
            }
        };
    }

    var visibilityObserver = "IntersectionObserver" in window
        ? new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                var instance = instances.get(entry.target);
                if (instance) instance.visibility(entry.isIntersecting);
            });
        }, { threshold: 0.01 }) : null;

    function scan(scope) {
        if (!scope || !scope.querySelectorAll) return;
        var nodes = [];
        if (scope.matches && scope.matches(selector)) nodes.push(scope);
        scope.querySelectorAll(selector).forEach(function (node) { nodes.push(node); });
        nodes.forEach(function (node) {
            if (instances.has(node) || !node.isConnected) return;
            var instance = createWordmark(node);
            if (!instance) return;
            instances.set(node, instance);
            if (visibilityObserver) visibilityObserver.observe(node);
            else instance.visibility(true);
        });
    }

    function prune() {
        instances.forEach(function (instance, node) {
            if (!node.isConnected || !instance.isValid()) {
                if (visibilityObserver) visibilityObserver.unobserve(node);
                instance.dispose();
                instances.delete(node);
            }
        });
    }

    function start() {
        scan(document.body);
        if (window.Blazor && typeof window.Blazor.addEventListener === "function") {
            window.Blazor.addEventListener("enhancedload", function () {
                prune();
                scan(document.body);
                instances.forEach(function (instance) { instance.refresh(false); });
            });
        }
        if (document.fonts) {
            document.fonts.ready.then(function () {
                instances.forEach(function (instance) { instance.refresh(true); });
            }, function () { });
        }
    }

    if ("MutationObserver" in window) {
        new MutationObserver(function (mutations) {
            var removed = mutations.some(function (mutation) {
                return Array.prototype.some.call(mutation.removedNodes, function (node) {
                    return node.nodeType === 1;
                });
            });
            if (removed) prune();
            mutations.forEach(function (mutation) {
                mutation.addedNodes.forEach(function (node) {
                    if (node.nodeType === 1 && (node.matches(selector) || node.querySelector(selector))) scan(node);
                });
                if (mutation.target.nodeType === 1 && mutation.target.matches(selector)) scan(mutation.target);
            });
        }).observe(root, { childList: true, subtree: true });
        new MutationObserver(function () {
            instances.forEach(function (instance) { instance.refresh(true); });
        }).observe(root, { attributes: true, attributeFilter: ["data-theme"] });
    }

    document.addEventListener("visibilitychange", function () {
        instances.forEach(function (instance) {
            if (document.hidden) instance.pause(); else instance.resume();
        });
    });
    window.addEventListener("pagehide", function () {
        instances.forEach(function (instance) { instance.pause(); });
    });
    window.addEventListener("pageshow", function () {
        instances.forEach(function (instance) { instance.resume(); });
    });
    if (typeof reducedMotion.addEventListener === "function") {
        reducedMotion.addEventListener("change", function () {
            instances.forEach(function (instance) { instance.refresh(false); });
        });
        mobileViewport.addEventListener("change", function () {
            instances.forEach(function (instance) { instance.refresh(true); });
        });
    }
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", start, { once: true });
    } else {
        start();
    }
})();
