# Rebuilt BigFish — 4999 triangle desktop LOD

This is a separate low-poly version of the existing reconstructed character. The full-detail assets in `Assets/DesktopFish/Rebuilt/` and `Modeling/RebuiltBigFish/` remain intact.

- **4999 triangles:** Body 4387 + Face 564 + RearHairCards 48.
- **4883 Unity vertices / 4843 GLB records**, 58 original joints, six shared expression controls.
- Body and Face keep the original 1024×2048 atlas. Three curved rear underlayer cards use a separate 1536×1024 alpha atlas; their 48 triangles replace 61 previous LOD triangles.
- Card color and silhouettes are baked only from six groups of our own full-detail mesh (750 triangles), with pixel-center sampling, 2×2 area coverage and RGB-only edge dilation. Main outer hair remains volumetric. Cards use the same rigid Head attachment as the replaced underlayers and double-sided alpha testing at 0.5.
- The hand/shoe pass restores full shoe straps, hollow buckles, highlights and sock lace from our own full-detail source. Smooth uppers and soles retain a 3 mm simplification bound; 91 sampled hidden sock-interior facets are omitted. Shoes use 556 triangles instead of 236.
- New independently sectioned five-finger hands use 174 triangles each, with joint-aware two-role weights and rounded ten-sided forearm cuffs. Full gold bands are restored and moved 2.5 mm outward to follow the cuff cover. Some palm and cuff shapes remain approximate at this budget.
- Other surfaces fund the change through guarded vertex-update simplification at the existing 3 mm bound and removal of 106 unchanged facets unseen in the 216-view alpha/depth sample. The crown loses only 18 triangles, at a reported 0.484 mm combined error. All 80 scalp-cover records, the complete Face and the rear alpha cards remain exact to version 194. Retained body skin and all six expression streams are copied exactly. The full-detail source is unchanged.


The LOD prioritizes fringe edge curves, sampled crown support points, visible side/back hair curves, shoe details and articulated hands. The complete white chest panel preserves its round buttons and UV mapping. Sampled visibility is not an all-angle visibility, collision, or performance guarantee. Fine cloth folds, hidden layers and hair highlights still lose detail at this budget.

## Rebuild

With the own full authoring export in `work/rebuilt-geometry.json`, `python3 Tools/hair-cards/build_cards.py` regenerates the checked-in card recipe and texture (NumPy and Pillow required). It reconstructs the preceding scalp overlay from the frozen source-index recipe, verifies the exact native position/index signature, and never reads a reference asset.

Unity menu: **Desktop Fish → 5K → Save Model Variant** creates the native meshes/prefab and `work/rebuilt-geometry-5k.json` from the original saved meshes and the checked-in source-index recipe. Source signatures reject stale recipes when the original geometry or attributes change.

The hand/shoe recipe runs after the frozen source-index reduction, scalp coverage and rear-card replacement. Its signature rejects a different baseline. `Tools/hands-shoes/redistribute.py` reproduces the offline recipe from the local 194 snapshot, own full-detail exports, actual skin matrices and visibility samples in `work/hands-shoes-195/`; it never reads Ref assets. `Tools/meshopt-update.cpp` provides the vertex-update reducer, with zero-area and orientation guards. Native rebuilds use the checked-in recipe and need no offline measurement files.

Unity menu: **Desktop Fish → 5K → Build macOS App** creates `Builds/RebuiltBigFish.app` using this LOD. Camera framing is computed from the original model first, preserving character scale. The existing full-detail build entry remains available.

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python-exit-code 1 --python Modeling/RebuiltBigFish5K/build_blend.py
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python-exit-code 1 --python Modeling/RebuiltBigFish5K/verify_saved.py
```

The exporter preserves all four skin slots, including tiny weights normally omitted by Blender's exporter. It restores exact Unity normal vectors using temporary point IDs, then welds only identical complete records. The temporary GPU attributes are removed. The saved project contains shared expression drivers and explicit normal-delta attributes. Verification reopens the project, checks all six expressions at half/full strength against independent LBS, checks every GLB skin record and exact base/morph normals, and renders five views. Existing inherited collapsed-pole exceptions retain their original proof and tolerances.

Local review material: `work/hands-shoes-195/` (earlier reviews remain in `work/hair-cards-194/`, `work/poly5k-192/` and `work/scalp-cover-193/`). No reference asset or copied reference data is included in this LOD.
