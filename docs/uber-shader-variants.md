# Fast Uber shader builds

All project-owned shader multi-compile declarations now use shader features,
including URP global keywords, UI clipping, fog and instancing. Shortcut pragmas
are expanded to explicit keyword sets. Package includes that inject multi-compile
sets are replaced with explicit feature declarations. Pragma-only includes are removed. Package
sources are unchanged.

UberParticle.shader, UberParticle.hlsl, their metadata and the dedicated Particle
tests are removed. No material referenced that shader. Other particle effects keep
their existing shaders. Uber Sprite is still available for authoring, but no
material currently uses it, so it is no longer forced into the preloaded collection.

The essential collection contains three shaders and 284 variants:

- Post: default and the authored CRT filter (2).
- 3D Object: authored material states and runtime engine hologram, overheat Rim,
  preview/target hologram, solid target fallback and Rocket Wobble (270).
- UI: default, dither, and emission/tint materials with the four runtime
  clip-rectangle/alpha-clip combinations (12).

The engine rows preserve both serialized and drawer-initialized keyword states.
RocketPart can disable hologram while retaining its world-space selector. Unused
sample combinations such as Glitch, Dissolve, glass glow, outline and unused post
filters are excluded from explicit retention.

Lighting retention targets the current PC Forward+ renderer: reflection probes,
no shadows, hard cascades, soft cascades, and soft cascades with additional-light
shadows. Installed URP disables _ADDITIONAL_LIGHTS for Forward+ and enables
_CLUSTER_LIGHT_LOOP; desktop soft shadows use _SHADOWS_SOFT with per-light quality.
The current PC renderer also requires _LIGHT_LAYERS and _SCREEN_SPACE_OCCLUSION.
URP strips forward variants
without these renderer keywords even when the material uses _UNLIT_ON, so preview
materials retain the same renderer profiles. Each PC profile also retains FOG_EXP2
for SkyEnvironment and SimulationTest, alongside fog-off for HappyEndingSequence.
Different renderers, lightmapping, other fog modes, instancing, quality settings or new runtime effects require
reviewing the manifest before shipping.

Remaining shader names, GUIDs, properties, render states and HLSL calculations
are preserved. Rebuild through Tools > Uber Shader > Rebuild Variant Collection.
The generator validates a transient collection before saving and preserves bytes
on a second identical rebuild. The existing preload reference remains intact.

Regression checks cover actual production keyword transitions, runtime GLES3
compilation, the remaining GPU rendering tests, feature declarations, collection
membership and rebuild stability.

The player build also verifies that URP retains the UniversalForward pass.
The earlier all-feature build exposed missing renderer keywords; both light layers
and SSAO are now retained so URP no longer removes the forward pass entirely.

Validated on 2026-09-06: all 67 rendering EditMode tests passed. The final Windows
build succeeded in 24.65 seconds with a populated build cache, zero errors and
488 warnings. After URP stripping, Uber 3D UniversalForward retained eight vertex
and 58 fragment variants, including exponential-squared fog and fog-off states.
Output: Builds/Windows/NHNAI2026.exe (complete output directory: 562.57 MB).
