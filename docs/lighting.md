# 2D Lighting & Shadow System

URP 2D Renderer pipeline. All lights are `Light2D`, shadows via `ShadowCaster2D`.

## Global Setup

- **Global Light 2D** — white, intensity 1, Additive blend. One per scene. Provides base ambient.
- **Renderer2DData** — URP 2D renderer asset. Sorting layers configured for selective light targeting.

## Room Lighting

`RoomController` (on each room root, `[DefaultExecutionOrder(100)]`) toggles all `Light2D` and `ShadowCaster2D` children when rooms activate/deactivate via `CameraController.RoomChanged`. Caches in Awake + re-caches after one frame (coroutine Start) to catch runtime-spawned children.

## Light Types in Use

### Electric Tile Lights
On `ElectricTileVFX` prefab → `Light` child with Point Light2D (blue `0.4/0.7/1`, intensity 1.5, outerRadius 12). `ElectricLightPulse` syncs intensity to the ElectricPulse shader timing via sine wave + stutter flicker.

### Window Light Shafts
`PointLightShadow` prefab — Freeform Light2D, intensity 0.18, Additive overlap, shadowsEnabled=true, shadowIntensity=0.7. Targets **Background sorting layer only** (id `194564831`). Placed at window positions; light passes through gaps in decoration shadow casters.

## Decoration Shadow System

Lets light shafts cast realistic shadows through scaffold/decoration tiles by auto-generating shadow shapes from sprite alpha.

### Pipeline

1. **Paint** — In editor, select `DecorationShadowController` on room. Press `J` to enter paint mode. Click bgDecoration tiles to toggle shadow assignment. Green overlay + "S" label marks assigned tiles.

2. **Spawn** — At runtime, `DecorationShadowController.Start()` instantiates `TileShadow` prefab at each painted cell position, calling `TileSpriteShadowCaster.Init(tilemap, cell)`.

3. **Shape Generation** — `TileSpriteShadowCaster.Start()`:
   - Reads tile sprite texture (must be `isReadable=true`)
   - Scans rows for horizontal spans of opaque pixels (alpha ≥ threshold)
   - Merges vertically adjacent spans with matching x-range into rectangles
   - **Filters**: only keeps rects touching top edge (`y1==h`) or bottom edge (`y0==0`)
   - Each surviving rect → child GameObject with `ShadowCaster2D`
   - Shape injected via reflection (`m_ShapePath`, `m_ShapePathHash`)
   - Caster toggled `enabled=false/true` to force mesh rebuild

### Key Files

| File | Role |
|------|------|
| `World/TileSpriteShadowCaster.cs` | Sprite→shadow shape decomposition |
| `World/DecorationShadowController.cs` | Serialized painted cells, runtime spawner |
| `Editor/DecorationShadowEditor.cs` | Scene view paint tool (J key) |
| `Prefabs/TileShadow.prefab` | ShadowCaster2D + TileSpriteShadowCaster |

### Why Edge-Touching Filter?

Decoration tiles (scaffolds, greebles) have interior gaps that should let light through. Full-tile shadow would block everything. By only keeping rects that touch top or bottom edges, horizontal beams cast shadows while interior voids remain transparent to light.

### Texture Requirements

Tilesets used with this system need `Read/Write Enabled` in import settings:
- `factoryStairsTileset.png`
- `factoryGreeblesTileset.png`
- `factoryTileset.png`

Non-readable textures fall back to a single full-tile rect.

## Performance Notes

- Room-based toggling prevents offscreen lights from contributing to draw calls
- Window lights target single sorting layer (Background) — avoids multi-layer overhead
- Shadow shapes are simple axis-aligned rects, not complex polygons
- Consider merging adjacent window lights into fewer wider freeforms if draw calls become an issue (room 1-1 has ~23 individual lights)
