export interface LocationSummary {
  id: string;
  code: string;
  zone: string;
}

export interface StockMovementSummary {
  id: string;
  materialCode: string;
  quantity: number;
  fromLocationCode: string;
  toLocationCode: string;
  sourceTaskId: string;
  movedAtUtc: string;
}
