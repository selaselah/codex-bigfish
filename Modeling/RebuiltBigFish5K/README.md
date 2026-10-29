# Rebuilt BigFish — 4993 triangle desktop LOD

This is a separate low-poly version of the existing reconstructed character. The full-detail assets in `Assets/DesktopFish/Rebuilt/` and `Modeling/RebuiltBigFish/` remain intact.

- **4993 triangles:** Body 4429 + Face 564.
- **4750 Unity vertices**, 58 original joints, six shared expression controls.
- The same 1024×2048 atlas, palette, generator animation and equipment remain in use.
- All retained positions, normals, UVs, RGBA, bone indices/weights and expression records are copied exactly from the full-detail model. Eyelid, eyebrow and mouth topology remains intact. Eye-white and iris interiors are reduced while preserving their outer boundaries.

The LOD prioritizes fringe edge curves, sampled crown support points, visible side/back hair curves and shoe profiles. It restores the complete white chest panel after reduction to preserve its round buttons and UV mapping. Sampled alpha/depth visibility across 216 views removes occluded and subpixel hair geometry. This sampling is not an all-angle visibility, collision, or performance guarantee. Fine cloth folds, cuff volume, hidden layers and hair highlights still lose detail at this budget.

## Rebuild

Unity menu: **Desktop Fish → 5K → Save Model Variant** creates the native meshes/prefab and `work/rebuilt-geometry-5k.json` from the original saved meshes and the checked-in source-index recipe. Source signatures reject stale recipes when the original geometry or attributes change.

Unity menu: **Desktop Fish → 5K → Build macOS App** creates `Builds/RebuiltBigFish.app` using this LOD. Camera framing is computed from the original model first, preserving character scale. The existing full-detail build entry remains available.

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python-exit-code 1 --python Modeling/RebuiltBigFish5K/build_blend.py
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python-exit-code 1 --python Modeling/RebuiltBigFish5K/verify_saved.py
```

The exporter preserves all four skin slots, including tiny weights normally omitted by Blender's exporter. It restores exact Unity normal vectors using temporary point IDs, then welds only identical complete records. The temporary GPU attributes are removed. The saved project contains shared expression drivers and explicit normal-delta attributes. Verification reopens the project, checks all six expressions at half/full strength against independent LBS, checks every GLB skin record and exact base/morph normals, and renders five views. Existing inherited collapsed-pole exceptions retain their original proof and tolerances.

Local review material: `work/poly5k-192/`. No reference asset or copied reference data is included in this LOD.
