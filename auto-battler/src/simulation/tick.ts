import type { BattleBoard, BattleItem, InFlightSignal, ItemOutput, SimState } from "./state.js";
import { resolveOutputTarget } from "./state.js";
import type { Behavior, CellModifier, DirectionModifier } from "./items.js";
import type { Cell, Direction } from "./types.js";
import { rotateDirection } from "./types.js";
import type { BattleEvent } from "../events/types.js";

export const TICK_MS = 62.5;    // 16 ticks/sec
export const MAX_TICKS = 960;   // 1 minute

export interface TickResult {
  tick: number;
  events: BattleEvent[];
  done: boolean;
}

// ─── Behavior Handlers ────────────────────────────────────────────────────────

function applyDirectionModifier(mod: DirectionModifier, dir: Direction): Direction {
  if (mod.kind === "passthrough") return dir;
  return rotateDirection(dir, mod.degrees);
}

function applyExitModifier(mod: CellModifier, cells: Cell[], hitRel: Cell, inputDir: Direction): Cell[] {
  if (mod.kind === "allOther") {
    return cells.filter(c => !(c.col === hitRel.col && c.row === hitRel.row));
  }
  // "opposite": cell furthest from hitRel along the input direction
  const offset = { col: inputDir.includes("right") ? 1 : inputDir.includes("left") ? -1 : 0,
                   row: inputDir.includes("down")  ? 1 : inputDir.includes("up")   ? -1 : 0 };
  let best: Cell = hitRel;
  let bestDist = 0;
  for (const c of cells) {
    if (c.col === hitRel.col && c.row === hitRel.row) continue;
    const dist = (c.col - hitRel.col) * offset.col + (c.row - hitRel.row) * offset.row;
    if (dist > bestDist) { best = c; bestDist = dist; }
  }
  return best === hitRel ? [] : [best];
}

type BehaviorHandler = (b: Behavior, item: BattleItem, hitRel: Cell, signal: InFlightSignal) => ItemOutput[];

const BEHAVIOR_HANDLERS: Record<Behavior["kind"], BehaviorHandler> = {
  relay: (b, item, hitRel, signal) => {
    if (b.kind !== "relay") return [];
    const outDir = applyDirectionModifier(b.direction, signal.direction);
    const exitCells = applyExitModifier(b.exit, item.def.cells, hitRel, signal.direction);
    return exitCells.map(c => ({ cell: c, direction: outDir }));
  },
};

function applyBehaviors(item: BattleItem, hitRel: Cell, signal: InFlightSignal): ItemOutput[] {
  const outputs: ItemOutput[] = [];
  for (const b of item.def.behaviors ?? []) {
    const handler = BEHAVIOR_HANDLERS[b.kind];
    outputs.push(...handler(b, item, hitRel, signal));
  }
  // fall back to configured outputs if no behaviors
  if (outputs.length === 0 && (item.def.behaviors ?? []).length === 0) {
    outputs.push(...(item.outputs ?? []));
  }
  return outputs;
}

// ─── Tick ─────────────────────────────────────────────────────────────────────

export function advanceTick(state: SimState): TickResult {
  state.tick++;
  const events: BattleEvent[] = [];

  for (const board of [state.playerBoard, state.opponentBoard] as BattleBoard[]) {
    const nextSignals: InFlightSignal[] = [];

    // ── Phase 1: resolve signals that arrived this tick ───────────────────
    for (const signal of board.inFlightSignals) {
      for (const item of board.items) {
        if (!item.def.triggerModes.includes("signal")) continue;

        const hitRel = item.def.cells.find(c =>
          item.anchor.col + c.col === signal.targetCell.col &&
          item.anchor.row + c.row === signal.targetCell.row
        );
        if (!hitRel) continue;

        const outputs = applyBehaviors(item, hitRel, signal);
        events.push({ kind: "itemActivate", tick: state.tick, itemId: item.id, itemName: item.def.name, anchor: item.anchor, outputs });
        for (const o of outputs) {
          nextSignals.push({ targetCell: resolveOutputTarget(item.anchor, o), direction: o.direction, sourceItemId: item.id });
        }
      }
    }

    // ── Phase 2: cooldown-triggered items ─────────────────────────────────
    for (const item of board.items) {
      if (!item.def.triggerModes.includes("cooldown")) continue;
      if (item.nextActivation > state.tick) continue;

      const outputs = item.outputs ?? [];
      events.push({ kind: "itemActivate", tick: state.tick, itemId: item.id, itemName: item.def.name, anchor: item.anchor, outputs });
      item.nextActivation = state.tick + item.def.cooldownTicks;

      for (const o of outputs) {
        nextSignals.push({ targetCell: resolveOutputTarget(item.anchor, o), direction: o.direction, sourceItemId: item.id });
      }
    }

    board.inFlightSignals = nextSignals;
  }

  const done = state.winner !== null || state.tick >= MAX_TICKS;
  return { tick: state.tick, events, done };
}

export function runLoop(
  state: SimState,
  onTick: (result: TickResult) => void,
  onDone?: () => void,
): void {
  const handle = setInterval(() => {
    const result = advanceTick(state);
    onTick(result);
    if (result.done) {
      clearInterval(handle);
      if (onDone) setTimeout(onDone, 3000);
    }
  }, TICK_MS);
}
