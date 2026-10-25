# Scene 2 witch visual

Scene2_WitchChase now uses a colored witch model and its Idle animation. The saved `Witch_ImportedVisual` prefab instance is a child of `WitchBoss/Witch_ArtAnchor`. The boss's purple capsule renderer is disabled. `WitchBossAI`, `PoisonApple`, the projectile prefab, the throw point and the chase controller were not edited.

The new visual is `Assets/Models/QuaterniusWitch/QuaterniusWitchVisual.prefab`. The included FBX was converted from the original GLB. Six source colors are assigned to explicit URP materials, so the visual does not depend on missing external textures. `WitchFlightVisual` plays the model's own Generic Idle clip, avoids root motion and cleans up its PlayableGraph when disabled. The boss's existing hover and chase movement make the witch fly through the scene.

Source and license: ["Witch" by Quaternius on Poly Pizza](https://poly.pizza/m/QBEOV9ZUT8), [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/). Credit Quaternius in the game's credits. Original GLB and converted FBX SHA-256 values can be checked against the asset files.

Unity 6000.3.10f1 imported the FBX and saved the prefab. In an isolated validation project it loaded a copy of Scene2_WitchChase, found five colored renderers and a valid Generic avatar, confirmed the capsule hidden, and measured approximately 0.98 degrees of arm movement from the Idle clip. The isolated check does not cover full game Play Mode, camera framing or projectile gameplay.

In the actual project, reopen Scene2_WitchChase and test Play Mode: the colored witch should be visible at the boss position, float and follow the player, face the correct direction, continue its Idle animation, and throw poison apples with the same timing and effects. Check that no old purple capsule or old WitchVisual model appears and that the player can finish or restart the scene as before. If an already open Unity Editor cached the old scene, close and reopen the scene before testing.
