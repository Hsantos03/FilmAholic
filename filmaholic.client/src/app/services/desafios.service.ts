import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class DesafiosService {
  private apiUrl = 'https://localhost:7277/api/desafios';

  constructor(private http: HttpClient) { }

  getAll(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
  }

  // Returns desafios including the current user's progress (requires auth cookie)
  getWithUserProgress(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/user`, { withCredentials: true });
  }
}
