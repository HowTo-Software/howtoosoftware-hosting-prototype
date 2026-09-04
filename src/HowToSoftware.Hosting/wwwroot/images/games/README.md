# Game banners

The catalogue at `/game-hosting` looks for one file per game, by exact name:

| File | Card |
|---|---|
| `Zomboid_Banner.png` | Project Zomboid |
| `Mine_banner.png` | Minecraft |

Drop the file here and reload; no code change is needed. While a file is missing the card draws
a typographic plate instead. Names are case-sensitive on a Linux host, so keep them exactly as
written (see `Services/GameBannerLibrary.cs`, which owns the list).

- Landscape, 16:10 or wider. The card crops from the centre with `object-fit: cover`; keep the
  subject in the middle.
- About 1600 px on the long edge is plenty; PNG as named, ideally under 600 KB.
- Only artwork the company is entitled to use: your own captures, or material licensed for
  this use. Nothing here is fetched from a studio's press kit.
