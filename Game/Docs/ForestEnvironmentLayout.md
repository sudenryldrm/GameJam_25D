# Forest environment — scenes 1 and 2

All eight uploaded FBX files are imported unchanged under Assets/Models. Shared URP materials use their embedded diffuse colors; no texture files were included.

Scene1 retains its continuous 100-unit collision ground, mushroom house, seven followers, four optional platforms, elevated mask and witch-house exit. Three grass-topped Ground2 segments cover the base. Pine groups, small grass tufts, rocks and logs fill the background; the start house and exit remain clear.

Scene2 retains its ten platform collision volumes, two modules, witch chase, dead frog and tower exit. Wide islands use Ground2; medium platforms use the small pair from Ground3Small; the narrow platform uses the smallest pair from Ground4smallest. Trees occupy the rear of the deep islands. Low grass and rocks detail the platforms without moving their edges or filling jump gaps.

The smaller exports contain the earlier larger versions too. ForestLevelArt selects only grassGround.001/tasDuvar.001 or grassGround.002/tasDuvar.002. The duplicate Cube objects and other variants are hidden. The original FBX assets remain intact. Ground2 uses grassGround/tasDuvar only.

Each scene has a Level1_ForestArt or Level2_ForestArt hierarchy with Platform_Models, Trees_Background and Grass_Rocks_Logs. All placements are serialized, with initial measured transforms. The ExecuteAlways component fits actual imported mesh bounds once after enable/import, assigns shared materials and keeps decorations at their specified surface height. Use Fit Forest Art after changing placement records. It does no mesh work after a successful fit; missing imports retry at most once per second. Imported art has no generated colliders. The existing simple colliders remain authoritative, including the original upper-platform headroom.

The former Ground_Art in scene1 is inactive and platform blockout renderers/guides are hidden. Their colliders remain unchanged. Decorations do not cast shadows; platforms do. Shared materials support instancing. The preceding movement, attack, camera and rendering tuning is preserved. Scene3 is untouched.

Validation checks scene references, unchanged colliders/spawns/triggers, selected FBX meshes, material references, serialized fit dimensions, art placement footprints and upload hashes. Unity Editor compilation, the imported appearance and actual frame times require play testing. Flat collision planes can differ slightly from the rough art surface.

Grass orientation is resolved before fitting: reset the grass root to an unscaled,
zero-yaw pose, rotate its longest blade axis up if needed, then apply the placement
yaw, uniform height scale and floor alignment. This handles source-axis correction
on either the FBX root or its child mesh and applies in both forest scenes.
Scene1's last two optional upper platforms and their floating decorations have
been removed; the continuous ground, mask route and four earlier platforms remain.
Unity appearance/play-mode verification is still pending.
