# Stone platform detail correction

The original Ground3Small FBX selected pair measures approximately
18.493 x 7.498 x 2.538 metres. Fitting its 7.498-unit vertical relief into a
0.65-unit strip flattened the authored stone courses; no mesh detail was absent
from the source. Source FBX geometry, imported normals and materials remain intact.

Scene1 upper-platform art/collision depths are now 1.05, 2.4, 2.8 and 2.4 units.
Scene2 raised platforms grow from 0.8 to 1.9, and narrow platforms from 0.7 to
1.0. Ground island depths remain unchanged. Collision bodies extend downward to
match the visible rock, preserving every top-surface height, width and depth.
The lowest Scene1 underside is Y=2.65, above the grounded player head at Y=2.5.
Scene2 raised-platform undersides are Y=2.8, also leaving the lower walking lane open.

Both forest camera profiles now face the brick facade at pitch 0 degrees.
SampleScene retains orthographic size 6.6 and Scene2 retains 6.5. Follow offset Y
is 1.1 with minimum camera Y=2.6, revealing more wall below the walking surface.
The ground top previously projected 26*sin(6 degrees)=2.718 units vertically;
at zero pitch that top-surface projection disappears. Source geometry remains
upright. The tower keeps its original 8-degree profile.

Scene camera prefab overrides and CM_Gameplay transforms/lenses match the runtime
profile, so editor framing also shows the vertical wall. The first intro camera
faces forward too, with Y moving from 3.6 to 2.6. Its 15-second duration, left-to-right
X pan, 7-to-5.8 slow zoom and return-to-gameplay blend are unchanged.

An accidental literal backslash-n outside a string in ForestLevelArt's Pine case
was corrected; it would prevent C# compilation. Escape/delimiter checks were run.
Source geometry was rendered before/after thickness fitting. Scene IDs, retained
components, unchanged landing surfaces, source aspect ratios and walking-lane
clearances were checked numerically. Unity compilation, collision/jump play tests
and final rendering remain pending in the Unity editor.
