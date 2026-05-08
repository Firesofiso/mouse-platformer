---
name: Surface pending session tasks explicitly
description: If a memory entry describes a specific pending task to pick up, surface it explicitly to the user rather than silently acting on it
type: feedback
---

If any memory entry describes a specific pending task (e.g. "these 3 scene objects need re-wiring"), call it out explicitly to the user at the start of the session before acting on it.

**Why:** User wants awareness of what I'm fixating on, not silent execution.
**How to apply:** After unity-session-start, scan memory for actionable task entries and state them plainly — one sentence, what needs doing and why.
