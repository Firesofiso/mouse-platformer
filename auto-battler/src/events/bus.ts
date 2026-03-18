import type { BattleEvent } from "./types.js";

type Handler<T extends BattleEvent> = (event: T) => void;
type AnyHandler = Handler<BattleEvent>;

export class EventBus {
  private handlers = new Map<string, AnyHandler[]>();

  on<T extends BattleEvent>(kind: T["kind"], handler: Handler<T>): void {
    const list = this.handlers.get(kind) ?? [];
    list.push(handler as AnyHandler);
    this.handlers.set(kind, list);
  }

  emit(event: BattleEvent): void {
    const list = this.handlers.get(event.kind);
    if (list) {
      for (const h of list) h(event);
    }
  }
}
