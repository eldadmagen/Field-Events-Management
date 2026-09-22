import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { SignalRService } from '../../core/signalr.service';
import { EventsApiService } from '../../core/events-api.service';
import { EventPriority, EventStatus, EventStatusHistoryEntry, EventSummary } from '../../models/event.model';

/** Statuses a technician is allowed to move an assigned event to, per state. */
const TECHNICIAN_NEXT_STATUSES: Record<EventStatus, EventStatus[]> = {
  [EventStatus.New]: [],
  [EventStatus.Assigned]: [EventStatus.InProgress],
  [EventStatus.InProgress]: [EventStatus.Completed],
  [EventStatus.Completed]: [],
  [EventStatus.Cancelled]: []
};

@Component({
  selector: 'app-technician',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './technician.component.html',
  styleUrl: './technician.component.css'
})
export class TechnicianComponent implements OnInit, OnDestroy {
  readonly EventStatus = EventStatus;
  readonly EventPriority = EventPriority;
  readonly available = signal<EventSummary[]>([]);
  private readonly commentDrafts = signal<Record<number, string>>({});
  readonly openHistoryEventId = signal<number | null>(null);
  readonly historyEntries = signal<EventStatusHistoryEntry[]>([]);

  constructor(
    readonly auth: AuthService,
    readonly signalr: SignalRService,
    private readonly api: EventsApiService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.refreshMine();
    this.refreshAvailable();
    this.signalr.connect();
  }

  ngOnDestroy(): void {
    this.signalr.disconnect();
  }

  nextStatuses(status: EventStatus): EventStatus[] {
    return TECHNICIAN_NEXT_STATUSES[status] ?? [];
  }

  claim(id: number): void {
    this.api.claim(id).subscribe(() => {
      this.refreshMine();
      this.refreshAvailable();
    });
  }

  changeStatus(id: number, newStatus: EventStatus): void {
    this.api.changeStatus(id, newStatus).subscribe(() => this.refreshMine());
  }

  commentDraft(id: number): string {
    return this.commentDrafts()[id] ?? '';
  }

  updateCommentDraft(id: number, text: string): void {
    this.commentDrafts.update((drafts) => ({ ...drafts, [id]: text }));
  }

  sendComment(id: number): void {
    const text = this.commentDraft(id).trim();
    if (!text) return;

    this.api.addComment(id, text).subscribe(() => this.updateCommentDraft(id, ''));
  }

  toggleHistory(id: number): void {
    if (this.openHistoryEventId() === id) {
      this.openHistoryEventId.set(null);
      return;
    }
    this.api.getHistory(id).subscribe((entries) => {
      this.historyEntries.set(entries);
      this.openHistoryEventId.set(id);
    });
  }

  changedByName(userId?: number): string {
    return userId == null ? 'System' : `User #${userId}`;
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }

  private refreshMine(): void {
    this.api.getMine().subscribe((events) => this.signalr.seedEvents(events));
  }

  private refreshAvailable(): void {
    this.api.getAvailable().subscribe((events) => this.available.set(events));
  }
}
