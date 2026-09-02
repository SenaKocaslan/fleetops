// Backend'deki TaskSummary kaydinin istemci karsiligi.
export interface TaskSummary {
  id: string;
  status: string;
  materialCode: string;
  quantity: number;
  priority: number;
  createdAtUtc: string;
  assignedAgvId: string | null;
}

export interface CreateTaskRequest {
  fromLocationId: string;
  toLocationId: string;
  materialCode: string;
  quantity: number;
  priority: number;
}
