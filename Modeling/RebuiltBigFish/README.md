# Rebuilt BigFish — editable model

`BigFish.blend` is the editable Blender model; `BigFish.glb` contains the portable rigged mesh. Unity uses `Assets/DesktopFish/Rebuilt/BigFish.prefab` and its two mesh assets.

This version authors mesh positions, topology, vertex colors, bones and five facial morphs in `Assets/DesktopFish/Editor/FishRebuiltModel.cs`. The generator does not read the previous 3D model, textures or skeleton. The character appearance is informed by the existing 2D pet and previous 3D screenshots; this is geometry provenance, not a claim of original character design.

Mesh density is recorded in `Assets/DesktopFish/Rebuilt/provenance.json`. The model uses two material slots, one generated 1024×1024 paint atlas, 51 bones and five expression morphs. Blender materials approximate the Unity toon shader rather than reproducing it exactly.

## Local visual reference

`Assets/DesktopFish/Generated/Ref.asset` is the retained local reference mesh (30,033 vertices, 43,815 triangles, 38 material slots). `DesktopFish.Editor.FishReferenceReview.Render` renders it with the compatible local legacy rig/materials into `work/reference-review/`, at front, 35-degree and side views. It leaves the reference file unchanged and does not place reference assets into the rebuilt exports.

The first comparison pass reduces the rebuilt eyes, raises the central fringe, shortens the chest curls and widens the skirt. These changes follow the reference silhouette; the reference mesh topology and texture data are not copied into the rebuilt generator.

The subsequent structure pass replaces the rounded hair tubes with thin ridged panels, adds a curved hair mantle for side/back volume, shortens the lower face, and rebuilds the bib, collar, ribbon loops, apron lace and skirt folds. Iris shading and cheek gradients are newly authored vertex colors. Wave/cheer wrist targets are adjusted for the rebuilt hair volume. `FishRebuiltModel.RenderAngles` generates front, 35-degree and side previews under `work/`.

This remains a work in progress. The broad garment structure and hair volume have been rebuilt, but the reference's painted hair detail, fine hair silhouette and expression range are not yet matched. Pose/frustum validation checks deformation and framing; it does not establish visual fidelity.

The latest visual pass separates the lower hair into S-shaped locks, reduces the inner hair volume toward the hem, and adds UV paint for strands, highlights and blue/cyan tips. The headband is widened to remain visible over the hair. The body below the neck is shortened by 12%, with matching joint and garment positions. Eyes use layered iris gradients; fingers are continuous rounded meshes. Gold trim follows skirt pleats, and apron/hem layers are positioned to prevent missing sections. Detail is still being refined, especially side/back hair, hand gestures and facial shape.

`HairPaint.png` is generated alongside the Unity meshes. The Blender file packs this image; the GLB includes the image, UVs and the named `Color` vertex attribute as `COLOR_0`, preserving skin/cloth/eye colors in viewers.

The next volume pass replaces five front-facing rear strips with nine radial locks. These follow the authored scalp surface, keep a consistent cross-section through curled tips, and cover the side/back down to the hem. The underlying mantle is shortened to reveal individual ends. Upper root junctions and the wrist/cuff connection still need further visual refinement.

## Reproduce

1. Run Unity batch method `DesktopFish.Editor.FishRebuiltModel.ReviewAndBuild`. This creates the Unity assets, previews, geometry/idle-pose JSON under `work/`, and `Builds/RebuiltBigFish.app`.
2. Run Blender in background with `--factory-startup --python Modeling/RebuiltBigFish/build_blend.py` from the project root. This regenerates both editable exports from that JSON, with the idle pose applied to the rig.

For geometry iteration without rebuilding the app, run `DesktopFish.Editor.FishRebuiltModel.Review`. The Blender exporter checks the resulting GLB's packed image, UVs, authored colors, joints, unlit face material and neutral facial weights.

The normal legacy build remains separate in `Builds/BigFatBlueFish.app`. Generated Unity assets and exported Blender/GLB files should be regenerated together after changing model dimensions or paint.
