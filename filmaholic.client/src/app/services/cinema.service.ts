import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface CinemaMovie {
  id: string;
  titulo: string;
  poster: string;
  cinema: string;
  horarios: string[];
  genero: string;
  duracao: string;
  classificacao: string;
  idioma: string;
  sala: string;
  link: string;
}

export interface CinemaVenue {
  id: string;
  nome: string;
  morada: string;
  latitude: number;
  longitude: number;
  distanceKm?: number;
  website?: string;
}

@Injectable({
  providedIn: 'root'
})
export class CinemaService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private readonly cinemaApi = this.apiBase ? `${this.apiBase}/api/cinema` : '/api/cinema';

  constructor(private http: HttpClient) {}

  /** Lista de cinemas com coordenadas para o mapa (nome, morada, lat, lng). */
  getNearbyCinemas(): Observable<CinemaVenue[]> {
    return this.http.get<CinemaVenue[]>(`${this.cinemaApi}/proximos`).pipe(
      catchError(() => of([]))
    );
  }

  getCinemaMovies(): Observable<CinemaMovie[]> {
    // Agora usa o caminho relativo. O Angular sabe que deve chamar a API no mesmo domínio onde o site está hospedado
    return this.http.get<CinemaMovie[]>(`${this.cinemaApi}/em-cartaz`).pipe(
      catchError(() => of(this.getMockCinemaMovies()))
    );
  }

  private getMockCinemaMovies(): CinemaMovie[] {
    return [
      {
        id: 'cinema-nos-1',
        titulo: 'Duna: Parte Dois',
        poster: 'https://image.tmdb.org/t/p/w500/d5NXSklWG3bVgiQ9dYBHWd2Kvbe.jpg',
        cinema: 'Cinema NOS',
        horarios: ['14:30', '17:45', '21:00', '23:30'],
        genero: 'Ficção Científica',
        duracao: '2h 46min',
        classificacao: 'M/12',
        idioma: 'Legendado',
        sala: 'Sala 1 - 3D',
        link: 'https://www.cinemas.nos.pt/filmes/duna-parte-dois'
      },
      {
        id: 'cinema-nos-2',
        titulo: 'Oppenheimer',
        poster: 'https://image.tmdb.org/t/p/w500/8Gxv8gSFCU0XGDykEGv7zR1sZ2T.jpg',
        cinema: 'Cinema NOS',
        horarios: ['15:00', '18:30', '22:00'],
        genero: 'Drama/História',
        duracao: '3h 0min',
        classificacao: 'M/16',
        idioma: 'Legendado',
        sala: 'Sala IMAX',
        link: 'https://www.cinemas.nos.pt/filmes/oppenheimer'
      },
      {
        id: 'cinema-nos-3',
        titulo: 'Guardiões da Galáxia Vol. 3',
        poster: 'https://image.tmdb.org/t/p/w500/r2J02Z2OpNTctfOSN1Ydgii51I3.jpg',
        cinema: 'Cinema NOS',
        horarios: ['13:15', '16:20', '19:30', '22:40'],
        genero: 'Aventura/Comédia',
        duracao: '2h 30min',
        classificacao: 'M/12',
        idioma: 'Dublado',
        sala: 'Sala 4',
        link: 'https://www.cinemas.nos.pt/filmes/guardioes-da-galaxia-vol-3'
      },
      {
        id: 'cinema-nos-4',
        titulo: 'A Pequena Sereia',
        poster: 'https://image.tmdb.org/t/p/w500/ym1dxyOkBwevReh05hY7J3ZI9GR.jpg',
        cinema: 'Cinema NOS',
        horarios: ['14:00', '17:00', '20:00'],
        genero: 'Fantasia/Musical',
        duracao: '2h 15min',
        classificacao: 'M/6',
        idioma: 'Dublado',
        sala: 'Sala 2',
        link: 'https://www.cinemas.nos.pt/filmes/a-pequena-sereia'
      },
      {
        id: 'cinema-nos-5',
        titulo: 'Velozes e Furiosos 10',
        poster: 'https://image.tmdb.org/t/p/w500/1E5baAaEse26fej7wuH3HJ7MWTu.jpg',
        cinema: 'Cinema NOS',
        horarios: ['15:30', '18:45', '22:15'],
        genero: 'Ação/Aventura',
        duracao: '2h 21min',
        classificacao: 'M/12',
        idioma: 'Legendado',
        sala: 'Sala 5 - 4DX',
        link: 'https://www.cinemas.nos.pt/filmes/velozes-e-furiosos-10'
      }
    ];
  }

  searchMovieByTitle(titulo: string): Observable<number | null> {
    // NUNCA uses localhost:5185 aqui, senão o site no Azure vai falhar sempre
    return this.http.get<any>(`${this.cinemaApi}/search-tmdb?titulo=${encodeURIComponent(titulo)}`).pipe(
      map(res => res.id ?? null),
      catchError(() => of(null))
    );
  }
}
