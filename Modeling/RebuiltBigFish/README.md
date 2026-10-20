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

The next volume pass replaces five front-facing rear strips with nine radial locks. These follow the authored scalp surface, keep a consistent cross-section through curled tips, and cover the side/back down to the hem. The underlying mantle is shortened to reveal individual ends. The roots now begin at the crown and share angular boundaries on the scalp. Their skin weights blend gradually from the head to the moving hair bones; both influences are preserved by the Blender/GLB exporter. A rear view is included in `RenderAngles()` for checking those junctions. The lower locks still need more varied silhouettes and less uniform paint.

Below the crown, the radial locks now sweep toward the back by an amount that decreases from the sides to the center rear. The full cross-sections and curled tips follow that deformation; the scalp anchors stay fixed. The underlying mantle follows with a slightly stronger sweep. This exposes the sleeves and skirt in side views while keeping the shorter front locks and the frontal volume. The change adds no vertices or triangles.

The separate outer S-shaped locks now turn their cross-sections gradually toward the sides and gain modest thickness through their middle sections. A small rearward sweep keeps them near the side/back hair volume, while narrower roots tuck beneath the crown. This lets a side view show their surfaces instead of only thin vertical edges. Chest curls receive a gentler turn and thickness adjustment. The existing topology, paint layout and bone set are retained.

The nine radial rear locks retain their shared crown anchors but use staggered lower lengths, radial layers and curl sizes. Their midsections narrow earlier to reduce neighboring-sheet intersections; near a curl facing directly outward, the cross-section follows the previous frame smoothly instead of flipping toward a nearly parallel direction. Modest sideways tip offsets and a sharper taper separate the ends. The underlying mantle extends farther down and narrows inward toward its hem. These changes keep the same vertex and triangle counts. The rear silhouette is less uniform, though it still needs more natural painted variation and finer tip shapes.

The fringe now keeps its width longer before tapering, with inward-swept side tips and forehead clearance across each sheet. Three independently painted highlight layouts replace the repeated square glints; the fourth atlas strip supplies the longer blue/cyan locks. The shader classifies pale cloth using the textured surface color, so the hair's neutral vertex colors no longer flatten its directional shading.

The upper fringe follows the headband's inner arc with a gradual radial fit into the side locks. A narrow sewn binding joins that hairline to the pleats, and the outer ruffle is slightly narrower. This removes the squared root tabs without cutting steps into the sides or increasing the overall accessory silhouette. The binding uses 400 triangles.

Facial layers now follow the curved head with concentric mesh rings and smaller depth offsets. Closed eyelids use a rounded cross-section instead of flattened open-eye vertices; mouth expressions retain their surface offset. Round wrist volumes bridge the cuffs and palms during hand rotation. `work/rebuilt-joints-and-face.png` shows idle, both wave extremes and closed eyes at 35 degrees for depth inspection, in addition to the front gesture sheet and 150-pose bounds checks.

The eyes now have a slightly narrower and shorter footprint, with the iris, pupil and highlights scaled together. A flatter upper white contour follows the eyelid; the upper lash tapers toward the inner corner and extends into a small curved outer wing. Closed-eye geometry uses the same tapered cross-sections and stays fitted to the face. The mouth, brows and five morph names are retained. The outer wings add 120 triangles; these are modest proportion changes, not a complete facial redesign.

Each sleeve is now one continuous surface from the rounded shoulder to the cuff, with a small amount of gathered volume. Two-bone weights blend across the shoulder and elbow; the cuff and its gold rings follow the forearm together. Finger rings blend between the palm and neighboring joints instead of switching bones abruptly. These weights are preserved in both editable exports. The sleeve refinement adds 576 triangles; finger topology and the 51-bone rig are unchanged. `work/rebuilt-working.png` adds 35-degree and side views of the working pose to inspect these transitions.

The side fins and the two tail lobes use curved leading/trailing contours with a thin closed volume. The ear fins retain a blue border and a pale inner area; the tail ends in two dark-blue lobes with a visible notch. Small gold buckles follow the shoe surfaces. These accessories remain bound to the existing head, tail and foot bones.

The apron has a straight waist edge, softly rounded lower corners and shallow gathered folds. Its cloth, flat seam and U-shaped lace follow the skirt curvature. The large whale is rebuilt as surface artwork, with its belly, eye and water strokes on separate depth layers; mirrored artwork is reprojected onto the asymmetric cloth folds. This removes 896 triangles compared with the previous rounded apron and sphere-based artwork, without shrinking the emblem or removing the lace.

The gold hem and bow embroidery now use broad surface bands instead of flattened tubes. The main hem band is wider than the secondary line; bow loops have distinct knots and curved tails, with larger stars and paired leaf accents. Artwork projects onto the frontmost segment of the same pleated skirt surface, including folds near its sides. The motifs sit above the hem with visible spacing. This pass removes 912 triangles while retaining and enlarging the gold detail; front previews are also compared at 240×320 pixels to check desktop-size readability.

## Reproduce

1. Run Unity batch method `DesktopFish.Editor.FishRebuiltModel.ReviewAndBuild`. This creates the Unity assets, previews, geometry/idle-pose JSON under `work/`, and `Builds/RebuiltBigFish.app`.
2. Run Blender in background with `--factory-startup --python Modeling/RebuiltBigFish/build_blend.py` from the project root. This regenerates both editable exports from that JSON, with the idle pose applied to the rig.

For geometry iteration without rebuilding the app, run `DesktopFish.Editor.FishRebuiltModel.Review`. The Blender exporter checks the resulting GLB's packed image, UVs, authored colors, joints, unlit face material and neutral facial weights.

The normal legacy build remains separate in `Builds/BigFatBlueFish.app`. Generated Unity assets and exported Blender/GLB files should be regenerated together after changing model dimensions or paint.
