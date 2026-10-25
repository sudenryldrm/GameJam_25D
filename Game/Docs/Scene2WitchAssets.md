# Scene 2 witch asset integration

The PoisonApple prefab now uses the supplied elma.fbx and the same body/stem materials as the lore apples. Projectile art is 0.4 world units high, versus 0.8 for a lore apple. Idle bob and tilt are disabled on projectiles. Existing Rigidbody, SphereCollider, PoisonApple collision/respawn/cloud logic and WitchBossAI throw timing are preserved.

The uploaded `witch Flying(1).fbx` contains one skinned geometry and an animation stack, but zero Material, Texture or LayerElementMaterial records. Its UVs occupy one tile (U approximately -1..0, V 0..1). The uploaded skin, teeth, eye and three clothing texture sets therefore have no preserved face/material assignments in this FBX. Correct assignment of all maps requires a source FBX/BLEND preserving those assignments and UV tiles; it cannot be recovered exactly from filenames alone. The witch replacement is pending that source; existing witch gameplay is retained.

Input FBX SHA-256: `4be5e4c1891068a4652130f6239dbcb7c96983195317cc6b3da289ed4c1aa6aa`.

Validation: projectile physics/gameplay YAML documents are unchanged; model/material GUIDs match existing lore apple assets. Unity Play Mode validation is pending because Unity is unavailable in this environment.
