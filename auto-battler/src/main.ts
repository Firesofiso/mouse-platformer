import { createPlayerBoard, placeItem, snapshotBoard, createSimState } from "./simulation/state.js";
import { runLoop } from "./simulation/tick.js";
import { EventBus } from "./events/bus.js";
import { attachConsoleRenderer, printBoards } from "./renderer/console.js";
import { CALL_TO_ACTION, CUT_PASTE } from "./simulation/items.js";

// Board layout:
//   [   ][   ][   ]
//   [   ][C/P][   ]
//   [CtA][Ct→][C/P]
//
// CTA fires every 2s, output → from (1,2) targets (2,2).
// C/P occupies (1,1) and (2,2). Signal hits (2,2), relays → out of (1,1).
// Expected: [Cut/Paste] output [→] at (1,1)

const playerBoard = createPlayerBoard(3, 3);

placeItem(playerBoard, CALL_TO_ACTION, { col: 0, row: 2 }, [
  { cell: { col: 1, row: 0 }, direction: "right" },
]);

placeItem(playerBoard, CUT_PASTE, { col: 1, row: 1 });

const opponentBoard = createPlayerBoard(3, 3);

function startBattle() {
  const playerSnapshot = snapshotBoard(playerBoard);
  const opponentSnapshot = snapshotBoard(opponentBoard);
  const state = createSimState(playerSnapshot, opponentSnapshot);

  printBoards(playerBoard, playerSnapshot, opponentBoard, opponentSnapshot);
  const bus = new EventBus();
  attachConsoleRenderer(bus);
  runLoop(
    state,
    ({ events }) => { for (const e of events) bus.emit(e); },
    startBattle,
  );
}

startBattle();
