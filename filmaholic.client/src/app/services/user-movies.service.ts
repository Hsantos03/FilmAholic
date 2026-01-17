import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})

export class UserMoviesService {

  private apiUrl = 'https://localhost:7277/api/usermovies';

  constructor(private http: HttpClient) { }

  addMovie(filmeId: number, jaViu: boolean): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/add?filmeId=${filmeId}&jaViu=${jaViu}`,
      {},
      { withCredentials: true }
    );
  }

  removeMovie(filmeId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/remove/${filmeId}`, { withCredentials: true });
  }

  getList(jaViu: boolean): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/list/${jaViu}`, { withCredentials: true });
  }

  getTotalHours(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/totalhours`, { withCredentials: true });
  }

  getStats(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/stats`, { withCredentials: true });
  }
}
