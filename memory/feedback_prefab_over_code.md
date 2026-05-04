---
name: Prefer prefab/asset edits over runtime code workarounds
description: Don't paper over missing or wrong asset configuration with runtime code — fix the asset directly
type: feedback
originSessionId: f08c1f57-ec91-4410-a14e-799c199bad0a
---
Fix assets (prefabs, ScriptableObjects, scene objects) directly rather than compensating in code at runtime.

**Why:** "The path of least resistance is maintaining a quality codebase." Runtime workarounds hide the real problem, add noise, and rot over time.

**How to apply:** If a component shouldn't exist on a prefab, remove it from the prefab. If a value is wrong in an asset, fix it there. Only reach for runtime code when the behavior genuinely needs to be dynamic.
