# Infrastructure photography

Real photographs of the physical hosting hardware. **Nothing in this folder is stock imagery,
and nothing in it may be** — the infrastructure page presents these as our own machines, so a
stock photo here would be a false claim about what a customer is buying.

## Drop the files here

`HardwarePhoto.razor` checks for each file at render time. While a file is missing the page
renders a labelled placeholder naming the exact path; the moment the file exists the component
serves it instead. **No code change is needed** — add the file and reload.

| File | What it should show | Used by |
|---|---|---|
| `rack-front.webp` | The rack standing in the room, doors closed — the establishing shot | Infrastructure hero |
| `rack-elevation.webp` | The rack straight on, doors closed | Beside the drawn rack elevation |
| `rack-open.webp` | The rack with the door open, machines visible | Lead frame of "the machines" |
| `rack-interior.webp` | Inside: switching, cabling, the mounted machines | Gallery strip |
| `rack-detail-top.webp` | Close view of the upper rack | Gallery strip |
| `rack-detail-bottom.webp` | Close view of the lower rack | Gallery strip |

**Caption what the photograph actually shows.** Both nodes sit in the same cabinet and no supplied
frame distinguishes them, so no image is captioned "node 01" — the node specifications are rendered
beside the photographs, not on top of them. If a photograph of one machine on its own arrives
later, add a slot for it rather than relabelling one of these.

Every `.webp` may be accompanied by a `.jpg` of the same name. When present it is emitted as a
`<picture>` fallback for browsers without WebP; when absent the WebP is used on its own.

## Format

- **WebP**, quality ~82. A 3000×4000 phone photo lands around 300–600 KB.
- **Portrait 3:4** is the default frame, because a rack is photographed standing up. The layout
  reserves the box before the image decodes and crops with `object-fit: cover`; a landscape photo
  in a portrait slot is cropped hard through the middle. Pass `CssClass="is-landscape"` (3:2) or
  `is-wide` (16:9) on the component for a slot that is not portrait.
- **Long edge 2000 px** is plenty. The largest rendered size is ~760 CSS px.
- Keep the originals somewhere outside the repository. These are the web derivatives.

## Converting

With ImageMagick:

```bash
magick input.jpg -auto-orient -strip -resize 2000x2000\> -quality 82 rack-open.webp
magick input.jpg -auto-orient -strip -resize 2000x2000\> -quality 84 rack-open.jpg
```

With `cwebp`:

```bash
cwebp -q 82 -metadata none -resize 2000 0 input.jpg -o rack-open.webp
```

## Before you publish a photo

Check the frame for things that should not be on a public marketing page:

- monitors or laptops showing panel sessions, IP addresses, hostnames or credentials
- labels and asset tags carrying serial numbers
- anything identifying the exact street address
- other people, without their agreement

Also strip the metadata. A phone photo carries the GPS coordinates it was taken at; `-strip` and
`-metadata none` above remove it, and the derivatives in this folder were written without an EXIF
block at all.

Cropping is cheaper than a retraction.

<!--
    © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
-->
