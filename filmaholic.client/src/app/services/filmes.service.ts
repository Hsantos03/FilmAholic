import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Filme {
  id: number;
  titulo: string;
  duracao: number;
  genero: string;
  posterUrl: string;
}

@Injectable({ providedIn: 'root' })
export class FilmesService {
  private apiUrl = 'https://localhost:7277/api/filmes';

  constructor(private http: HttpClient) { }

  getAll(): Observable<Filme[]> {
    return this.http.get<Filme[]>(this.apiUrl);
  }
}
