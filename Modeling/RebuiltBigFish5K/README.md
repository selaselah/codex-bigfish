# Rebuilt BigFish — 4980 triangle desktop LOD

This is a separate low-poly version of the existing reconstructed character. The full-detail assets in `Assets/DesktopFish/Rebuilt/` and `Modeling/RebuiltBigFish/` remain intact.

- **4980 triangles:** Body 4368 + Face 564 + RearHairCards 48.
- **4759 Unity vertices / 4705 GLB records**, 58 original joints, six shared expression controls.
- Body and Face keep the original 1024×2048 atlas. Three curved rear underlayer cards use a separate 1536×1024 alpha atlas; their 48 triangles replace 61 previous LOD triangles.
- Card color and silhouettes are baked only from six groups of our own full-detail mesh (750 triangles), with pixel-center sampling, 2×2 area coverage and RGB-only edge dilation. Main outer hair remains volumetric. Cards use the same rigid Head attachment as the replaced underlayers and double-sided alpha testing at 0.5.
- Remaining Body records and all Face attributes, skin and expressions are exact to the previous LOD. The earlier scalp repair intentionally reuses 85 skull/neck triangles, changes 80 RGB / 59 UV records and outsets 14 interior positions by 2 mm; moving boundaries, all normal fields and all morph deltas are preserved. The full-detail source is unchanged.


The LOD prioritizes fringe edge curves, sampled crown support points, visible side/back hair curves and shoe profiles. It restores the complete white chest panel after reduction to preserve its round buttons and UV mapping. Sampled alpha/depth visibility across 216 views removes occluded and subpixel hair geometry. This sampling is not an all-angle visibility, collision, or performance guarantee. Fine cloth folds, cuff volume, hidden layers and hair highlights still lose detail at this budget.

## Rebuild

With the own full authoring export in `work/rebuilt-geometry.json`, `python3 Tools/hair-cards/build_cards.py` regenerates the checked-in card recipe and texture (NumPy and Pillow required). It reconstructs the preceding scalp overlay from the frozen source-index recipe, verifies the exact native position/index signature, and never reads a reference asset.

Unity menu: **Desktop Fish → 5K → Save Model Variant** creates the native meshes/prefab and `work/rebuilt-geometry-5k.json` from the original saved meshes and the checked-in source-index recipe. Source signatures reject stale recipes when the original geometry or attributes change.

Unity menu: **Desktop Fish → 5K → Build macOS App** creates `Builds/RebuiltBigFish.app` using this LOD. Camera framing is computed from the original model first, preserving character scale. The existing full-detail build entry remains available.

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python-exit-code 1 --python Modeling/RebuiltBigFish5K/build_blend.py
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python-exit-code 1 --python Modeling/RebuiltBigFish5K/verify_saved.py
```

The exporter preserves all four skin slots, including tiny weights normally omitted by Blender's exporter. It restores exact Unity normal vectors using temporary point IDs, then welds only identical complete records. The temporary GPU attributes are removed. The saved project contains shared expression drivers and explicit normal-delta attributes. Verification reopens the project, checks all six expressions at half/full strength against independent LBS, checks every GLB skin record and exact base/morph normals, and renders five views. Existing inherited collapsed-pole exceptions retain their original proof and tolerances.

Local review material: `work/hair-cards-194/` (previous LOD and scalp reviews remain in `work/poly5k-192/` and `work/scalp-cover-193/`). No reference asset or copied reference data is included in this LOD.
