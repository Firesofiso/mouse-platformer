export interface Cell {
  col: number;
  row: number;
}

export type Direction =
  | "up" | "down" | "left" | "right"
  | "up-left" | "up-right" | "down-left" | "down-right";

// Clockwise order for rotation
const DIR_CW: Direction[] = ["up", "up-right", "right", "down-right", "down", "down-left", "left", "up-left"];

export function rotateDirection(dir: Direction, degrees: 45 | 90 | 135 | 180 | 225 | 270 | 315): Direction {
  const steps = degrees / 45;
  const idx = DIR_CW.indexOf(dir);
  return DIR_CW[(idx + steps) % 8]!;
}

export const DIR_OFFSET: Record<Direction, Cell> = {
  "up":         { col:  0, row: -1 },
  "down":       { col:  0, row: +1 },
  "left":       { col: -1, row:  0 },
  "right":      { col: +1, row:  0 },
  "up-left":    { col: -1, row: -1 },
  "up-right":   { col: +1, row: -1 },
  "down-left":  { col: -1, row: +1 },
  "down-right": { col: +1, row: +1 },
};
