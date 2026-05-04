---
name: Always diagnose via debug before changing code
description: Never make code changes based on assumptions about runtime behavior — always instrument and read actual data first
type: feedback
originSessionId: f08c1f57-ec91-4410-a14e-799c199bad0a
---
Always diagnose via debug before changing code in this project.

**Why:** I repeatedly changed code based on assumptions about Unity's runtime behavior (API naming, collision ordering, spear orientation) without verifying. Every wrong assumption wasted tokens and broke things. The user had to explicitly tell me to stop multiple times.

**How to apply:** Before any fix to runtime behavior:
1. Add a targeted Debug.Log (or use execute_code to inspect live state) to answer one specific question
2. Read the console/result
3. Only then write code

For Unity physics specifically: never assume which collider is `coll.collider` vs `coll.otherCollider`, which layer something is on, or whether a method is even being called — verify all of it with logs first. A good first question is always "is this code even running?" and "what are the actual values?"

**Also:** Use Unity MCP to run and verify diagnostics autonomously — never ask the user to perform in-game actions I can trigger or observe myself. If Unity MCP is not connected, stop problem solving and escalate immediately.

**MCP self-verification (critical):** After adding any debug log or diagnostic code, use Unity MCP execute_code + read_console to verify it yourself — do NOT ask the user to throw/trigger things. If Unity MCP is disconnected, stop and say so immediately before attempting any more problem solving.
