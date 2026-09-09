# AgroControl application icons

`agrocontrol.svg` is the canonical source for installable client icons. The PWA manifest references this vector directly, while Tauri generates native PNG/ICO/ICNS assets from it with `npm run desktop:icons` before desktop builds.

Do not hand-edit generated native icon formats. Update the canonical SVG and regenerate them instead.
