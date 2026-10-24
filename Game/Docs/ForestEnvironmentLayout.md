# Forest environment

Scene1 uses the supplied reference as a composition guide: overlapping foreground
vegetation, three receding tree layers, and small flowers around the route.
The existing 3D art style is retained; this is not a traced copy of the 2D image.

- 348 grass clusters overlap across three full-length ground bands and the
  front/back edges of the four upper platforms. Width is fitted independently
  of height, preventing the old isolated-tuft appearance. Each band repeats
  long, short, long, long, long, short, with lower foreground and taller back rows.
- 62 trees form far, middle and near bands, plus two foreground framing trees
  beyond the playable ends. There are 38 broadleaf trees and 24 existing pines.
  Distant foliage is pale; nearer foliage becomes darker. Far and framing trees
  use complete silhouettes, while nearer trees retain brown trunks.
- 59 flower clusters alternate cream and pink petals with yellow centres and
  green stems/leaves. They decorate the main ground and the four upper platforms.

ReferenceFlowers.obj is derived from the existing LevelKit/flowers-tall.fbx;
ReferenceCanopyTree.obj is derived from LevelKit/tree.fbx. Their original vertices
and polygon topology are retained, converted from centimetres to metres, and
split into named mesh groups using their authored atlas regions. This removes
the dependency on the missing colormap.png and allows shared URP materials.
The original FBX files remain unchanged. No external model download is required.

ForestLevelArt preserves its original serialized kind numbers (0–4) and appends
Flower=5. Optional per-placement tint, silhouette and width default to their
original behavior for other scenes. New grass rows use zero yaw for width fitting.
Reference materials enable GPU instancing. Decorative renderers do not cast
shadows, generate colliders, or perform mesh fitting after layout succeeds.

All platform top surfaces, spawns, masks/apple visuals, followers, exits, Timeline
and camera bindings are preserved. See StonePlatformDetail.md for the later
stone-body depth and collider-alignment correction. Only the forest layout component and its two
decoration child lists change among retained scene objects. Scene3 is unchanged; Scene2 retains its original layout with the later stone-body
depth correction described in StonePlatformDetail.md.

Validation: unique scene IDs, removal references, decoration-parent bindings,
overlapping row widths, mesh face groups and model orientation were checked.
The two derived models were rendered in a geometry preview. Unity compilation,
scene import, gameplay visibility and frame rate still need editor verification.
