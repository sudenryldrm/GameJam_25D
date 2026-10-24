# Apple visuals for lore items

The unchanged uploaded elma.fbx is imported under Assets/Models. It contains a
red Sphere body and brown Cylinder stem; two shared URP materials reproduce its
embedded diffuse colors without external textures.

BeastMask in scene1, DeadFrog in scene2, and BrokenGlassShoes_LorePoint in scene3
have one persistent Apple_Item_Visual child and ItemAppleVisual component each.
The component fits actual imported mesh bounds to the existing item location and
hides the old placeholder renderers after the apple loads. Heights are 0.8 units
for mask/frog and 0.6 for shoes. All triggers, controller references, titles,
lore text and inspection behavior remain unchanged. No apple colliders are added.

The uploaded binary SHA-256 is
9a3a8d859999d94269e81f60f7d024716785d3de83bae71723e160466b958db4.
Only item GameObject component lists and child lists change in the scenes.
Unity import, compilation and visual Play Mode validation remain pending.
Grass rows await the user's reference and are unchanged in this commit.
