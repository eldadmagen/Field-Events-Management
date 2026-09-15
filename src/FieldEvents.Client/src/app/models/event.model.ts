export enum EventStatus {
  New = 0,
  Assigned = 1,
  InProgress = 2,
  Completed = 3,
  Cancelled = 4
}

export enum EventPriority {
  Low = 0,
  Normal = 1,
  High = 2,
  Critical = 3
}

export interface EventSummary {
  id: number;
  title: string;
  description: string;
  location?: string;
  source: string;
  priority: EventPriority;
  status: EventStatus;
  assignedTechnicianId?: number;
  createdAtUtc: string;
}
