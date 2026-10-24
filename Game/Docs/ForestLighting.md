# Forest stone-face lighting

Based on the Game-view screenshot, the stone facade is present but falls into
the turf overhang's very dark shadow. Scene1 and Scene2 now use a three-colour
ambient gradient to give side-facing and downward-facing surfaces fill light:
sky (0.34,0.36,0.38), equator (0.25,0.27,0.30), ground (0.15,0.16,0.17).
The existing skybox remains visible as the backdrop.

The existing main directional light moves from Euler (50,-30,0) to (38,-32,0),
keeping a sideways angle for bevel contrast while lighting the front more directly.
Its intensity is 1.7 rather than 2 and shadow strength is 0.7 rather than 1.
The front-facing direct-light term remains approximately 1.11 -> 1.14, while the
surface under a full shadow can retain 30% of direct lighting instead of 0%.
This is not a substitute for a rendered Unity test.

The shared neutral stone albedo increases from approximately 0.119 to 0.18;
its shader, normals, roughness and mesh details are unchanged. No extra lights,
shadow maps, runtime code or post effects are added. Tree/grass materials and
camera, mesh, collider and gameplay component records are unchanged.

Validation: only RenderSettings, the existing sun light/transform and the stone
material colour are changed. Unique scene IDs and preserved records checked.
Final Game-view appearance and Unity light-probe/environment updates still need
checking in the Unity editor after import.
