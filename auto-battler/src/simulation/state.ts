import type { ItemDef } from "./items.js";
import type { Cell, Direction } from "./types.js";
import { DIR_OFFSET } from "./types.js";

export type { Cell, Direction };

// ─── In-Flight Signal ─────────────────────────────────────────────────────────
// A signal traveling through the board. Lives for one tick, resolved the next.

export interface InFlightSignal {
  targetCell: Cell;
  direction: Direction;
  sourceItemId: string;
  payload?: unknown;
}

// ─── Item Output ──────────────────────────────────────────────────────────────

export interface ItemOutput {
  cell: Cell;      // relative to item anchor
  direction: Direction;
}

/** Absolute board cell that an output signal lands on. */
export function resolveOutputTarget(anchor: Cell, output: ItemOutput): Cell {
  return {
    col: anchor.col + output.cell.col + DIR_OFFSET[output.direction].col,
    row: anchor.row + output.cell.row + DIR_OFFSET[output.direction].row,
  };
}

// ─── Player Board ─────────────────────────────────────────────────────────────

export interface PlayerItem {
  id: string;
  def: ItemDef;
  anchor: Cell;
  outputs?: ItemOutput[]; // only needed for non-relay items; set at placement time
}

export interface PlayerBoard {
  cols: number;
  rows: number;
  items: PlayerItem[];
}

// ─── Battle Board ─────────────────────────────────────────────────────────────

export interface BattleItem {
  id: string;
  def: ItemDef;
  anchor: Cell;
  outputs?: ItemOutput[];
  nextActivation: number; // tick on which this item next fires (cooldown items only)
}

export interface BattleBoard {
  cols: number;
  rows: number;
  items: BattleItem[];
  inFlightSignals: InFlightSignal[];
}

/** Snapshot a PlayerBoard into a BattleBoard, resetting all runtime state. */
export function snapshotBoard(player: PlayerBoard): BattleBoard {
  return {
    cols: player.cols,
    rows: player.rows,
    items: player.items.map(item => ({
      id: item.id,
      def: item.def,
      anchor: { ...item.anchor },
      outputs: item.outputs?.map(o => ({ cell: { ...o.cell }, direction: o.direction })),
      nextActivation: 0,
    })),
    inFlightSignals: [],
  };
}

// ─── Sim State ────────────────────────────────────────────────────────────────

export interface SimState {
  tick: number;
  winner: string | null;
  playerBoard: BattleBoard;
  opponentBoard: BattleBoard;
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

let _nextId = 0;

export function placeItem(board: PlayerBoard, def: ItemDef, anchor: Cell, outputs?: ItemOutput[]): PlayerItem {
  const item: PlayerItem = { id: `${def.id}-${_nextId++}`, def, anchor, outputs };
  board.items.push(item);
  return item;
}

export function createPlayerBoard(cols: number, rows: number): PlayerBoard {
  return { cols, rows, items: [] };
}

export function createSimState(playerBoard: BattleBoard, opponentBoard: BattleBoard): SimState {
  return { tick: 0, winner: null, playerBoard, opponentBoard };
}
