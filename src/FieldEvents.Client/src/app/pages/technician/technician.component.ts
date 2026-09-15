import { Component, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { EventsApiService } from '../../core/events-api.service';
import { EventPriority, EventStatus, EventSummary } from '../../models/event.model';

/**
 * Skeleton page: shows events currently assigned to this technician via a plain REST fetch.
 * Live push-on-assignment (SignalR when online / Web Push stub when offline) is implemented
 * server-side in INotificationService.NotifyTechnicianAsync - wiring this page to listen on
 * ClientsHub too is straightforward (same pattern as DispatcherComponent) but is not part of
 * the required E2E flow, so it is left as a follow-up rather than done here.
 */
@Component({
  selector: 'app-technician',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './technician.component.html',
  styleUrl: './technician.component.css'
})
export class TechnicianComponent implements OnInit {
  readonly EventStatus = EventStatus;
  readonly EventPriority = EventPriority;
  readonly events = signal<EventSummary[]>([]);

  constructor(
    readonly auth: AuthService,
    private readonly api: EventsApiService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.api.getMine().subscribe((events) => this.events.set(events));
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
