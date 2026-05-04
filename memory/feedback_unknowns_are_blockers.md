# Unknowns Are Blockers, Not Cues to Guess

When diagnosing a visual/runtime bug, **naming an unknown is not the same as resolving it.**

The moment I write or think "I don't know X," the next action must be a tool call that answers X — not a code change, not a proposal, not a guess.

## Rule
**If the fix requires knowing a fact, know the fact first.**

## Common unknowns → correct next actions (Unity)
| Unknown | Tool call |
|---|---|
| Sprite pivot direction | `execute_code` → print `sprite.pivot / sprite.rect.size` |
| Why bounds are larger than visible content | `execute_code` → print `CharacterInfo.minY/maxY` per glyph |
| Where a child is relative to parent | `execute_code` → print `localPosition`, `localScale`, `bounds.min/max` |
| Whether code is even running | `read_console` before touching anything |

## What went wrong (DialogueBubble)
- Identified unknown: sprite pivot direction
- Instead of measuring: guessed "top pivot" → wrong
- Made 3 code changes in a row, all wrong in the same direction
- Didn't run a single diagnostic until after 3 failures
- Root cause (font ascent metrics ≠ glyph extents) was a separate unknown that also required measurement, not inference

## Anti-pattern to avoid
> "The pivot must be X because the glyph is appearing Y" → change code

## Correct pattern
> "I don't know the pivot" → `execute_code: print sprite.pivot normalized` → now I know → fix
