# Movement, attacks, camera and rendering tuning

- Shared player: normal movement 4 → 6 units/s, Shift remains 10. Acceleration 32, braking 42; Rigidbody interpolation enabled. Normal-speed acceleration takes ~0.19 seconds, braking ~0.14 seconds.
- Jump: rise gravity 1.6×, fall 3.6×, early-release gravity 4×. Impulses 15.2/14 preserve approximately the earlier jump heights while shortening the airtime. A jump immediately clears grounded/coyote state; an ascending jump cannot refresh grounded jumps from the floor ray.
- Witch: fixed 0.5–0.85 second ballistic throws aim at predicted horizontal player travel. Limited vertical lead aims near the body during jumps. Aim is sampled at release; apples do not home, so a direction change/jump can dodge them. A platform can still intercept an apple and create its poison cloud.
- Kisses: heart visuals grow smoothly from 1× to at most 1.9× based on closest distance to the player. The original collision radius stays 0.22; passing the player does not make a heart shrink suddenly. Lifetime remains five seconds.
- Camera: orthographic 2.5D, fixed depth, 8-degree downward angle, SmoothDamp, vertical dead zone, eased directional look-ahead. Scene1 view size 7.2; Scene2 6.5 with route-edge clamping; tower 7.6, centered horizontally and at least 25.6 units wide even on narrow windows. Camera snaps on player respawn. Profiles apply on Start based on scene name and can be disabled with useSceneProfile.
- Desktop rendering: main shadow atlas 2048 → 1024, cascades 4 → 2, shadow distance 50 → 32, lower soft shadow quality, additional-light shadows off, SSAO off. Native rendering resolution and character textures stay unchanged. Runtime frame limit 60 FPS prevents unrestricted rendering; it does not guarantee 60 FPS.

## Verification and remaining play checks
No Unity Editor/runtime is installed in the editing environment. Numeric ballistic/jump checks and serialized-data integrity checks run here; compilation, actual frame times and game feel require Unity play testing.

After pulling, check all three scenes: normal/Shift movement, short/held/double jumps, optional mask route and tower turns, apple dodges in both directions, heart growth during approach, camera framing and pause/respawn. Compare a standalone build's CPU/GPU frame times with the Unity Profiler if stutter persists. Reduced rendering cost is not a measured FPS claim.
