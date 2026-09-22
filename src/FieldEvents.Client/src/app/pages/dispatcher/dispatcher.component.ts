import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { SignalRService } from '../../core/signalr.service';
import { EventsApiService } from '../../core/events-api.service';
import { EventPriority, EventStatus, EventStatusHistoryEntry, EventSummary, TechnicianSummary } from '../../models/event.model';

/** Statuses a dispatcher can directly close an event into, per current state (see EventStateMachine.cs). */
const DISPATCHER_CLOSE_STATUSES: Record<EventStatus, EventStatus[]> = {
  [EventStatus.New]: [EventStatus.Cancelled],
  [EventStatus.Assigned]: [EventStatus.Cancelled],
  [EventStatus.InProgress]: [EventStatus.Completed, EventStatus.Cancelled],
  [EventStatus.Completed]: [],
  [EventStatus.Cancelled]: []
};

const ACTIVE_STATUSES = new Set([EventStatus.New, EventStatus.Assigned, EventStatus.InProgress]);

interface TechnicianRow extends TechnicianSummary {
  activeEvents: EventSummary[];
  activeEventTitles: string;
}

@Component({
  selector: 'app-dispatcher',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './dispatcher.component.html',
  styleUrl: './dispatcher.component.css'
})
export class DispatcherComponent implements OnInit, OnDestroy {
  readonly EventStatus = EventStatus;
  readonly EventPriority = EventPriority;
  readonly priorities = [EventPriority.Low, EventPriority.Normal, EventPriority.High, EventPriority.Critical];

  readonly technicians = signal<TechnicianSummary[]>([]);
  private readonly technicianPicks = signal<Record<number, number | null>>({});
  readonly openHistoryEventId = signal<number | null>(null);
  readonly historyEntries = signal<EventStatusHistoryEntry[]>([]);

  readonly technicianRows = computed<TechnicianRow[]>(() => {
    const presence = this.signalr.technicianPresence();
    const events = this.signalr.events();
    return this.technicians().map((t) => {
      const activeEvents = events.filter((e) => e.assignedTechnicianId === t.id && ACTIVE_STATUSES.has(e.status));
      return {
        ...t,
        isOnline: presence[t.id] ?? t.isOnline,
        activeEvents,
        activeEventTitles: activeEvents.map((e) => e.title).join(', ')
      };
    });
  });

  constructor(
    readonly auth: AuthService,
    readonly signalr: SignalRService,
    private readonly api: EventsApiService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.api.getAll().subscribe((events) => this.signalr.seedEvents(events));
    this.api.getTechnicians().subscribe((technicians) => {
      this.technicians.set(technicians);
      this.signalr.seedTechnicianPresence(
        Object.fromEntries(technicians.map((t) => [t.id, t.isOnline]))
      );
    });
    this.signalr.connect();
  }

  ngOnDestroy(): void {
    this.signalr.disconnect();
  }

  closeStatuses(status: EventStatus): EventStatus[] {
    return DISPATCHER_CLOSE_STATUSES[status] ?? [];
  }

  technicianName(id?: number): string {
    if (id == null) return '-';
    return this.technicians().find((t) => t.id === id)?.userName ?? `#${id}`;
  }

  pickedTechnician(eventId: number): number | null {
    return this.technicianPicks()[eventId] ?? null;
  }

  pickTechnician(eventId: number, technicianId: string): void {
    this.technicianPicks.update((picks) => ({ ...picks, [eventId]: technicianId ? Number(technicianId) : null }));
  }

  assign(eventId: number): void {
    const technicianId = this.pickedTechnician(eventId);
    if (technicianId == null) return;
    this.api.assign(eventId, technicianId).subscribe();
  }

  transfer(eventId: number): void {
    const technicianId = this.pickedTechnician(eventId);
    if (technicianId == null) return;
    this.api.transfer(eventId, technicianId).subscribe();
  }

  setPriority(eventId: number, priority: string): void {
    this.api.setPriority(eventId, Number(priority) as EventPriority).subscribe();
  }

  changeStatus(eventId: number, newStatus: EventStatus): void {
    this.api.changeStatus(eventId, newStatus).subscribe();
  }

  toggleHistory(eventId: number): void {
    if (this.openHistoryEventId() === eventId) {
      this.openHistoryEventId.set(null);
      return;
    }
    this.api.getHistory(eventId).subscribe((entries) => {
      this.historyEntries.set(entries);
      this.openHistoryEventId.set(eventId);
    });
  }

  changedByName(userId?: number): string {
    if (userId == null) return 'System';
    return this.technicians().find((t) => t.id === userId)?.userName ?? `User #${userId}`;
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
