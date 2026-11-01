# Rebuilt BigFish — 4999 triangle desktop LOD

This is a separate low-poly version of the existing reconstructed character. The full-detail assets in `Assets/DesktopFish/Rebuilt/` and `Modeling/RebuiltBigFish/` remain intact.

- **4999 triangles:** Body 4387 + Face 564 + RearHairCards 48.
- **4856 Unity vertices / 4841 GLB records**, 58 original joints, six shared expression controls.
- Body and Face keep the original 1024×2048 atlas. Three curved rear underlayer cards use a separate 1536×1024 alpha atlas; their 48 triangles replace 61 previous LOD triangles.
- Card color and silhouettes are baked only from six groups of our own full-detail mesh (750 triangles), with pixel-center sampling, 2×2 area coverage and RGB-only edge dilation. Main outer hair remains volumetric. Cards keep their original rigid Head skin and double-sided alpha testing at 0.5. Working follow-through is applied to a private runtime mesh, described below.
- The hand/shoe pass restores full shoe straps, hollow buckles, highlights and sock lace from our own full-detail source. Smooth uppers and soles retain a 3 mm simplification bound; 91 sampled hidden sock-interior facets are omitted. Shoes use 556 triangles instead of 236.
- New independently sectioned five-finger hands use 174 triangles each, with joint-aware two-role weights and rounded ten-sided forearm cuffs. Full gold bands are restored and moved 2.5 mm outward to follow the cuff cover. Some palm and cuff shapes remain approximate at this budget.
- Other surfaces fund the change through guarded vertex-update simplification at the existing 3 mm bound and removal of 106 unchanged facets unseen in the 216-view alpha/depth sample. The crown loses only 18 triangles, at a reported 0.484 mm combined error. In that pass all 80 scalp-cover records, the complete Face and the rear alpha cards remained exact to version 194. Retained body skin and all six expression streams were copied exactly. The full-detail source is unchanged.


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


## Crown and working hair follow-through (212)

The large crown/back cap is redistributed within the same **272-triangle** budget. Its 44 lower boundary records keep their exact positions, normals, UVs, colors and skin; 115 interior points use the existing own authored crown surface, with constrained triangulation in its angular/elevation chart. This replaces 186 old cap records with 159, without changing the surface functions, head scale or atlas. Body remains 4387 triangles; Face 564 and rear cards 48 are unchanged. All 4156 retained Body records, their six expression streams and 4115 oriented faces match 195 exactly, as do the complete Face, cards and 58-joint rig. The original reduction inputs and 3 mm guard remain unchanged; the replacement cap is retained in full after that reduction. This is not a new reference-fit error claim. The side crown shows less faceted shading; the small desktop-size change is modest and the side curls remain angular.

`FishHairSmooth5K.json` is the checked-in native authoring input. `FishHairSmooth5K.Apply` runs after the existing hand/shoe replacement, rejects a different input position/index signature, validates every new triangle and preserves the exact lower seam. It does not run a global reduction or change hands, shoes, skin or expressions.

In version 212, `FishHairFollow` evaluated a crank-synchronized travelling shear on complete hair domains and the three rear cards. The roots above Y=.80 remain fixed; displacement grows toward the ends and phase lag increases with length. This provides delayed follow-through while the original body, grip and all bone poses remain unchanged. The spatial field preserves Y (Jacobian determinant one); normals use the analytic inverse-transpose derivative. It edits per-instance mesh copies only, restores the exact neutral streams as work fades out, and disposes its meshes with the companion. Native meshes/GLB retain the neutral geometry and original skin; the Unity procedural motion is not baked into the portable files.

Matched 240-frame front/rear working sequences verify every non-hair posed position and all joint/model transforms, finite geometry and no new geometric triangle reversal/degeneracy. The diagnostic also records 221 triangle-frame facing transitions against inherited near-perpendicular stylized normals; these are not reported as zero or as geometric flips. No exporter checks or existing tolerances were changed. This is not a full collision or native performance test. Reopened Blender independently verifies 13 neutral/expression LBS cases, exact base/morph GLB normals and all skin records; five saved-project views and paired 7-angle/180×240/static isolated views were reviewed.

Review: `work/hair-smooth-212/Review.json`. Visual comparison and silent 8-second working clip: `outputs/BigFish-Hair-212/` (local review media).


## Inertial rear hair (213, Unity runtime)

Three virtual spring chains, each with five particles, now drive the large rear locks and their alpha underlayers. Real head movement excites persistent velocity, elastic return and damping; advancing time with a stationary head produces no waving. The chain roots are fixed below the crown seam at Y=.74. The front side curls retain the accepted 212 motion and are not attached to the rear controllers. The three old rear clock-driven joint offsets are disabled once physics takes over; all other joint/model poses, hands, shoes and facial expressions retain their previous response.

The controller uses substeps no larger than 1/120 second, distance and 32-degree bending constraints, plus analytic head-sphere/neck-and-shoulder capsule proxies. Cubic interpolation transfers particle offsets to private mesh copies. Transfer strain limits and positional orientation/area constraints protect the existing sparse triangles. Normals use the interpolation Jacobian followed by local rotation transport for the mesh correction. Teleports and long frame gaps reset safely, and a separate unsafe-field fallback is monitored by the regression tests. The checked neutral meshes, atlas, UVs, all skin/morph data, source rig/default pose and Blender/GLB files remain byte-identical to 212: **4999 triangles, 4856 Unity vertices, 4841 GLB records, 58 joints, six expressions**. The virtual particles do not add serialized bones or rendered triangles.

`FishHairPhysicsTests` checks stationary behavior, a motion-and-stop response, decay, 30/60/120fps agreement, sphere/capsule projection, analytic field derivatives, discontinuity recovery and mesh disposal. A 6cm fixture translation gives about 3.65cm peak lag, about 1.30cm residual 0.3s after the move ends, and about 5micrometers after settling; cross-frame-rate difference is about .071mm. These are fixture results, not whole-model accuracy measures. A matched 20-second sequence covers idle, two working intervals, two stops and a bow; all non-hair/side-curl posed positions and all joints except the three deliberately replaced rear drivers match 212, with no new geometric degeneration/reversal and no safety fallback.

This is a bounded virtual-chain effect, not strand-by-strand simulation. Collision proxies are approximate and do not provide full hair/cloth/self-collision. The sparse silhouette and some fold edges remain angular. Editor timing is recorded as a diagnostic, not a native performance guarantee. The portable GLB retains its neutral rig and does not embed this Unity runtime solver.

Review and failed-trial evidence: `work/hair-physics-213/Review.json`. Local 20-second comparison: `outputs/BigFish-Hair-Physics-213/Hair-Physics-Comparison.mp4` (left212, right213).
