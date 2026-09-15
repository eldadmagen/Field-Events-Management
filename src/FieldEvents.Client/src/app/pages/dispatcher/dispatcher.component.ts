import { Component, OnDestroy, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { SignalRService } from '../../core/signalr.service';
import { EventsApiService } from '../../core/events-api.service';
import { EventPriority, EventStatus } from '../../models/event.model';

@Component({
  selector: 'app-dispatcher',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './dispatcher.component.html',
  styleUrl: './dispatcher.component.css'
})
export class DispatcherComponent implements OnInit, OnDestroy {
  readonly EventStatus = EventStatus;
  readonly EventPriority = EventPriority;

  constructor(
    readonly auth: AuthService,
    readonly signalr: SignalRService,
    private readonly api: EventsApiService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.api.getAll().subscribe((events) => this.signalr.seedEvents(events));
    this.signalr.connect();
  }

  ngOnDestroy(): void {
    this.signalr.disconnect();
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
