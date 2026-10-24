# Dwarf model source files

These lossless Base64 parts contain the original uploaded FBX and both PNGs.
They were split to fit the connected GitHub upload service's request limit.

Open the Game project in Unity. DwarfSourceImporter automatically restores
GnomeRun.fbx, GnomeDiffuse.png and GnomeNormal.png under Assets/Resources/Dwarfs,
verifies their SHA-256 checksums, and refreshes the Asset Database. Their tracked
.meta files preserve the import settings. No external downloads are needed.

If necessary, run Tools > Dwarfs > Restore Uploaded Model Sources.
The generated binary files are ignored by Git because their exact source data
is already tracked here. Keep this ModelSources folder when copying the project.

All seven scene followers use the same material and model. The visual is built
under each Art_Anchor in the editor and during play. Their movement uses only
the main Ground collider surface, so player jumps and raised platforms do not
make the dwarfs jump. Movement pauses the run clip when a follower stops.

Validation: seven scene component bindings and unique scene object IDs checked;
all reconstructed source bytes match the uploaded originals. Unity is not
installed in the editing environment, so import, visual alignment and play mode
still require checking in the Unity editor.

## Repair white or frozen dwarfs

After pulling, stop Play mode and let Unity finish compiling. The importer now
reimports the textures, reconnects the material using actual imported assets,
imports the real FBX take range, and creates GnomeRunAnimation.anim for runtime
playback. Existing generated visuals are rebuilt after repair or script reload.
If needed, run Tools > Dwarfs > Repair Materials and Run Animation, then enter Play.
A successful repair prints a Dwarfs confirmation to the Console; errors include
the missing source, texture, or animation details. Generated source checksums
are still verified before importing. Unity play-mode verification remains pending.
