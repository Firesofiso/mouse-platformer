import type { EventBus } from "../events/bus.js";
import type { ItemActivateEvent } from "../events/types.js";
import type { Direction, ItemOutput, PlayerBoard, BattleBoard } from "../simulation/state.js";

const DIR_ARROW: Record<Direction, string> = {
  "up":         "↑",
  "down":       "↓",
  "left":       "←",
  "right":      "→",
  "up-left":    "↖",
  "up-right":   "↗",
  "down-left":  "↙",
  "down-right": "↘",
};

function simTime(tick: number): string {
  const sec = (tick - 1) * 0.0625;
  return `${sec < 10 ? " " : ""}+${sec.toFixed(2)}s`;
}

type GridItem = {
  def: { name: string; cells: { col: number; row: number }[] };
  anchor: { col: number; row: number };
  outputs?: ItemOutput[];
};

function renderGrid(cols: number, rows: number, items: GridItem[]): string[] {
  const grid: string[][] = Array.from({ length: rows }, () => Array(cols).fill("   "));

  for (const item of items) {
    const outputMap = new Map<string, string>();
    for (const o of item.outputs ?? []) {
      outputMap.set(`${o.cell.col},${o.cell.row}`, DIR_ARROW[o.direction]);
    }

    const abbr2 = item.def.name.split(" ").map((w: string) => w[0]).join("").slice(0, 2);
    const abbr3 = item.def.name.split(" ").map((w: string) => w[0]).join("").slice(0, 3).padEnd(3);

    for (const relCell of item.def.cells) {
      const row = grid[item.anchor.row + relCell.row];
      if (!row) continue;
      const col = item.anchor.col + relCell.col;
      if (col >= cols) continue;
      const arrow = outputMap.get(`${relCell.col},${relCell.row}`);
      row[col] = arrow ? `${abbr2}${arrow}` : abbr3;
    }
  }

  return grid.map(row => row.map(cell => `[${cell}]`).join(""));
}

export function printBoards(
  player: PlayerBoard,
  playerSnap: BattleBoard,
  opponent: PlayerBoard,
  opponentSnap: BattleBoard,
): void {
  const pw = renderGrid(player.cols, player.rows, player.items);
  const ow = renderGrid(opponent.cols, opponent.rows, opponent.items);

  console.log(`  ${"Player".padEnd(player.cols * 5)}    ${"Opponent".padEnd(opponent.cols * 5)}`);
  const rows = Math.max(pw.length, ow.length);
  for (let r = 0; r < rows; r++) {
    console.log(`  ${pw[r] ?? ""}    ${ow[r] ?? ""}`);
  }
  console.log(`  (${playerSnap.items.length} items)           (${opponentSnap.items.length} items)\n`);
}

export function attachConsoleRenderer(bus: EventBus): void {
  bus.on("itemActivate", (e: ItemActivateEvent) => {
    const outputs = e.outputs && e.outputs.length > 0
      ? ` output [${e.outputs.map(o => DIR_ARROW[o.direction]).join(" ")}]`
      : "";
    console.log(`${simTime(e.tick)} [${e.itemName}]${outputs} at (${e.anchor.col},${e.anchor.row})`);
  });
}
