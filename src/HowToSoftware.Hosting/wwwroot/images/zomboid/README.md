# Project Zomboid imagery

**This folder is empty on purpose, and the site is already built to use it.**

`ZomboidPlate.razor` checks for each file at render time. While a file is missing the plate
draws a composed stand-in and prints the exact path to drop the image at; the moment the file
exists the component serves it instead. **No code change is needed** — add the file and reload.

| File | What it should show | Used by |
|---|---|---|
| `hero-world.webp` | Wide, atmospheric world shot | Hero |
| `world.webp` | A persistent world in play | "Knox County keeps running without you" |
| `workshop.webp` | A heavily modded world | Workshop section |
| `survivors.webp` | Survivors, community scale | Reserved |
| `closing.webp` | Wide shot for the closing call to action | Reserved |

Each `.webp` may be accompanied by a `.jpg` of the same name, emitted as a `<picture>` fallback.

## Why nothing was downloaded

Project Zomboid screenshots and promotional art belong to **The Indie Stone**. Whether a hosting
company may use them in its own marketing is a licensing question with a real answer, and it is
not one to assume from the fact that an image is easy to find. Nothing was scraped, nothing was
generated to look like the game, and no stock photograph is standing in for it.

Before adding a file here, get one of:

- written permission from The Indie Stone, or
- material published under terms that clearly cover this use, or
- screenshots **you** captured from your own licensed copy of the game

Keep a note of which applies to each file. The prototype ships a composed placeholder in the
meantime, which is honest about being one.

## Format

- **WebP**, quality ~80. Aim for under 400 KB at 2000 px wide.
- **Landscape 16:9** or wider. The plates crop with `object-fit: cover`.
- **Long edge 2000 px** is plenty — the widest a plate renders is around 1300 CSS px.
- Everything below the fold loads lazily and reserves its aspect ratio, so nothing reflows.

The plates grade every image down and lay a gradient scrim over it, so bright screenshots settle
into the page rather than glaring out of it. Pick images with somewhere dark for type to sit.

<!--
    © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
-->
