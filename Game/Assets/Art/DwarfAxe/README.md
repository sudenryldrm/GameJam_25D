# Dwarf axe model and projectile

## Ready-made coloured FBX selected

- **Low Poly Axe — Joseph Riches:** https://josephriches.itch.io/low-poly-axe
- Download package: `Low Poly Axe Model.zip` (FBX model and TIF texture).
- Licence: **CC0 1.0 Universal**; free for personal and commercial projects, attribution appreciated but not required.

The external package is selected as the source model, but its FBX/texture binaries are not included in this repository yet because the download package is hosted on itch.io. Import the FBX and texture into Unity to replace the fallback visual.

## Current behaviour

`Game/Assets/Scripts/DwarfAxeProjectile.cs` spawns a coloured low-poly axe fallback at runtime so the throwing mechanic does not depend on the FBX import. The projectile uses a trigger plus proximity hit detection, aims at the player, spins while flying, and calls the same respawn flow as the witch hazard. Dwarfs start throwing after 2 seconds and repeat every 2.5–3.5 seconds while gameplay is active.
