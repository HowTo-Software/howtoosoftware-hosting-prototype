# Fonts shipped with this site

Both families are served from this origin rather than from a font CDN. A stylesheet link to a
third party is a render-blocking request to a host we do not control, and it discloses every
visitor's IP address to that host.

| Family         | Designer     | Licence                        | Axes         |
| -------------- | ------------ | ------------------------------ | ------------ |
| Archivo        | Omnibus-Type | SIL Open Font License 1.1      | wdth, wght   |
| JetBrains Mono | JetBrains    | SIL Open Font License 1.1      | wght         |

The OFL permits redistribution with the software, which is what this is. Full text in `OFL.txt`;
it covers both families, which carry identical terms.

Subsets: `latin` and `latin-ext` only. Every accent English and Brazilian Portuguese need lives
in those two ranges, and the browser fetches whichever the page actually uses.
