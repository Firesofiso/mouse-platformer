---
name: Do Unity editor work via Unity MCP, not by handing off
description: For Unity projects, perform asset/scene/prefab/menu work yourself via Unity MCP — only surface things to the user when they need to be aware
type: feedback
originSessionId: c185ee41-7ee7-4ff0-9f7e-8beb3ac2745b
---
When working in this Unity project, do editor-side tasks (creating prefabs, ScriptableObject assets, scene wiring, inspector references, layer/tag setup, etc.) yourself via the Unity MCP server rather than writing a "here's what to do in the editor" checklist.

**Why:** The user prefers not to do tedious editor work that the agent can perform. Handing off mechanical setup wastes their time.

**How to apply:**
- Default: do it via Unity MCP.
- Surface to the user only:
  - Architectural/design decisions worth them seeing
  - Steps that demonstrate something non-obvious about the system you built
  - Authoring choices (sprite art, particle tuning, copy) that need their taste
- Ask "do you want to be aware of anything, or is it mostly tedium?" when unsure — don't assume the user wants a writeup.
