# Memory Index

- [feedback_surface_session_tasks.md](feedback_surface_session_tasks.md) — Surface pending task entries from memory before acting on them
- [feedback_prefab_over_code.md](feedback_prefab_over_code.md) — Fix assets (prefabs, scenes) directly rather than compensating with runtime code
- [feedback_debug_first.md](feedback_debug_first.md) — Always diagnose via debug before changing code; never assume runtime behavior
- [feedback_unity_mcp.md](feedback_unity_mcp.md) — Do Unity editor work via Unity MCP yourself; only surface decisions worth user awareness
- [feedback_unknowns_are_blockers.md](feedback_unknowns_are_blockers.md) — An unknown is a blocker, not a cue to guess; measure the fact before writing the fix
- [feedback_oneway_ceiling.md](feedback_oneway_ceiling.md) — UnitController ceiling cast kills upward velocity on one-way platforms; must exclude OneWayPlatformBehaviour from ceiling dampening
- [feedback_prototype_vscode.md](feedback_prototype_vscode.md) — Prototype new systems as plain C# in VS Code/dotnet before porting to Unity MonoBehaviour architecture
- [feedback_verify_yourself.md](feedback_verify_yourself.md) — Verify edits/wiring via Unity MCP myself; never ask the user to confirm setup
- [feedback_ask_before_diagnosing.md](feedback_ask_before_diagnosing.md) — Ask the user the one thing you need before launching a multi-step diagnostic loop
- [feedback_main_not_worktree.md](feedback_main_not_worktree.md) — Always work in main project path `/mouse-platformer/Assets/`; never the worktree path
- [feedback_doc_before_asking.md](feedback_doc_before_asking.md) — Document systems in docs/ immediately after designing them; never make the user re-explain their own game
- [feedback_runtime_bugs.md](feedback_runtime_bugs.md) — Never fix runtime bugs without gathering info and a debug plan first; Unity physics are too complex to guess at
- [feedback_just_execute.md](feedback_just_execute.md) — Don't ask permission before acting — pick the best approach and execute it
