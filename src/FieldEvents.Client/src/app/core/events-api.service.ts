import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { EventSummary } from '../models/event.model';

@Injectable({ providedIn: 'root' })
export class EventsApiService {
  constructor(private readonly http: HttpClient) {}

  getAll() {
    return this.http.get<EventSummary[]>(`${environment.apiBaseUrl}/api/events`);
  }

  getMine() {
    return this.http.get<EventSummary[]>(`${environment.apiBaseUrl}/api/events/mine`);
  }
}
