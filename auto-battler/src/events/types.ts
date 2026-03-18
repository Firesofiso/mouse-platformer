export interface TickEvent {
  kind: "tick";
  tick: number;
}

export type Direction = "up" | "down" | "left" | "right" | "up-left" | "up-right" | "down-left" | "down-right";

export interface ItemOutput {
  cell: { col: number; row: number }; // relative to item anchor
  direction: Direction;
}

export interface ItemActivateEvent {
  kind: "itemActivate";
  tick: number;
  itemId: string;
  itemName: string;
  anchor: { col: number; row: number };
  outputs?: ItemOutput[];
}

export type BattleEvent = TickEvent | ItemActivateEvent;
