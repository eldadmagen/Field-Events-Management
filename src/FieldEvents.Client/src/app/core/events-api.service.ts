import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { EventPriority, EventStatus, EventSummary, TechnicianSummary } from '../models/event.model';

@Injectable({ providedIn: 'root' })
export class EventsApiService {
  constructor(private readonly http: HttpClient) {}

  getAll() {
    return this.http.get<EventSummary[]>(`${environment.apiBaseUrl}/api/events`);
  }

  getTechnicians() {
    return this.http.get<TechnicianSummary[]>(`${environment.apiBaseUrl}/api/users/technicians`);
  }

  assign(id: number, technicianId: number) {
    return this.http.post<void>(`${environment.apiBaseUrl}/api/events/${id}/assign`, { technicianId });
  }

  transfer(id: number, technicianId: number) {
    return this.http.post<void>(`${environment.apiBaseUrl}/api/events/${id}/transfer`, { technicianId });
  }

  setPriority(id: number, newPriority: EventPriority) {
    return this.http.post<void>(`${environment.apiBaseUrl}/api/events/${id}/priority`, { newPriority });
  }

  getMine() {
    return this.http.get<EventSummary[]>(`${environment.apiBaseUrl}/api/events/mine`);
  }

  getAvailable() {
    return this.http.get<EventSummary[]>(`${environment.apiBaseUrl}/api/events/available`);
  }

  claim(id: number) {
    return this.http.post<void>(`${environment.apiBaseUrl}/api/events/${id}/claim`, {});
  }

  changeStatus(id: number, newStatus: EventStatus) {
    return this.http.post<void>(`${environment.apiBaseUrl}/api/events/${id}/status`, { newStatus });
  }

  addComment(id: number, text: string) {
    return this.http.post<void>(`${environment.apiBaseUrl}/api/events/${id}/comments`, { text });
  }
}
