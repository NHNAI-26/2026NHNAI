# 3D Uber Texture Blend

`Shader/Uber/3D Object` supports an optional upward-facing texture blend through the
local `_TEXTURE_BLEND_ON` shader-feature keyword. Disabled materials keep the original
single-base-texture path.

The Base Map can use its original mesh UVs or world-space triplanar mapping. Selecting
`Triplanar` enables the local `_BASE_MAP_TRIPLANAR` keyword. `Base Map 3D Tiling`
controls repeats per world unit independently on the X, Y, and Z axes, while
`Base Map 3D Blend Sharpness` controls how tightly the three planar projections meet.

The blend weight is evaluated from the interpolated geometric world normal:

```text
smoothstep(threshold - smoothness, threshold + smoothness, normalWS.y)
```

Upward-facing surfaces therefore receive `_BlendMap`, while surfaces facing farther
away from world up transition smoothly back to `_BaseMap`. `Blend Tiling` independently
scales the blend layer's mesh UVs. `_BlendColor` tints only the blend layer.

`MAT_ground` is the reference setup:

- Base Map: `dirt.png`
- Base Map Mapping: `Triplanar`
- Base Map 3D Tiling: `(1, 1, 1)`
- Blend Map: `grass.png`
- Blend Tiling: `(8, 8)`
- Upward Threshold: `0.62`
- Blend Smoothness: `0.424`

The Forward and Meta passes both apply the blend so the runtime result and baked
albedo stay consistent.
