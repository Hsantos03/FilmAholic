import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface MovieRatingSummaryDTO {
  average: number;
  count: number;
  userScore: number | null;
}

@Injectable({ providedIn: 'root' })
export class MovieRatingService {
  private apiUrl = 'https://localhost:7277/api/movieratings';

  constructor(private http: HttpClient) { }

  getSummary(movieId: number): Observable<MovieRatingSummaryDTO> {
    return this.http.get<MovieRatingSummaryDTO>(`${this.apiUrl}/${movieId}`, {
      withCredentials: true
    });
  }

  setMyRating(movieId: number, score: number): Observable<MovieRatingSummaryDTO> {
    return this.http.put<MovieRatingSummaryDTO>(
      `${this.apiUrl}/${movieId}`,
      { score },
      { withCredentials: true }
    );
  }

  clearMyRating(movieId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${movieId}`, {
      withCredentials: true
    });
  }
}
