import type { Cell } from "./types.js";

// ─── Behaviors ────────────────────────────────────────────────────────────────

export type DirectionModifier =
  | { kind: "passthrough" }
  | { kind: "rotate"; degrees: 45 | 90 | 135 | 180 | 225 | 270 | 315 };

export type CellModifier =
  | { kind: "allOther" }   // all non-hit cells
  | { kind: "opposite" };  // cell furthest from hit cell along input direction

export type Behavior =
  | { kind: "relay"; direction: DirectionModifier; exit: CellModifier };

// ─── Item Definition ──────────────────────────────────────────────────────────

export type TriggerMode = "cooldown" | "signal";

export interface ItemDef {
  id: string;
  name: string;
  cells: Cell[];
  triggerModes: TriggerMode[];
  cooldownTicks: number;
  behaviors?: Behavior[];
}

export const CALL_TO_ACTION: ItemDef = {
  id: "call_to_action",
  name: "Call to Action",
  cells: [{ col: 0, row: 0 }, { col: 1, row: 0 }], // 2×1
  triggerModes: ["cooldown"],
  cooldownTicks: 32,
};

export const REFRESH_BUTTON: ItemDef = {
  id: "refresh_button",
  name: "Refresh Button",
  cells: [{ col: 0, row: 0 }],                      // 1×1
  triggerModes: ["cooldown"],
  cooldownTicks: 32,
};

export const CUT_PASTE: ItemDef = {
  id: "cut_paste",
  name: "Cut/Paste",
  cells: [{ col: 0, row: 0 }, { col: 1, row: 1 }], // diagonal
  triggerModes: ["signal"],
  cooldownTicks: 0,
  behaviors: [{ kind: "relay", direction: { kind: "passthrough" }, exit: { kind: "allOther" } }],
};
