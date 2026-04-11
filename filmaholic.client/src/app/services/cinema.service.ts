import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

/// <summary>
/// Interface que representa um filme em exibição nos cinemas, contendo informações como ID, título, poster, cinema, horários, gênero, duração, classificação, idioma, sala e link para mais detalhes.
/// </summary>
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


/// <summary>
/// Interface que representa um local de cinema, contendo informações como ID, nome, morada, latitude, longitude, distância em km (opcional) e website (opcional).
/// </summary>
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

  /// <summary>
  /// Serviço para operações relacionadas com cinemas, incluindo obtenção de cinemas próximos e filmes em exibição, bem como pesquisa de filmes por título.
  /// </summary>
export class CinemaService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private readonly cinemaApi = this.apiBase ? `${this.apiBase}/api/cinema` : '/api/cinema';


  /// <summary>
  /// Construtor do serviço de cinema, injetando o HttpClient para comunicação com a API.
  /// </summary>
  constructor(private http: HttpClient) {}


  /// <summary>
  /// Obtém a lista de cinemas próximos, retornando um array de objetos CinemaVenue.
  /// </summary>
  getNearbyCinemas(): Observable<CinemaVenue[]> {
    return this.http.get<CinemaVenue[]>(`${this.cinemaApi}/proximos`).pipe(
      catchError(() => of([]))
    );
  }


  /// <summary>
  /// Obtém a lista de filmes em exibição nos cinemas, retornando um array de objetos CinemaMovie.
  /// </summary>
  getCinemaMovies(): Observable<CinemaMovie[]> {
    // Agora usa o caminho relativo. O Angular sabe que deve chamar a API no mesmo domínio onde o site está hospedado
    return this.http.get<CinemaMovie[]>(`${this.cinemaApi}/em-cartaz`).pipe(
      catchError(() => of(this.getMockCinemaMovies()))
    );
  }

  /// <summary>
  /// Fornece uma lista de filmes em exibição nos cinemas como dados mock, caso a chamada à API falhe.
  /// Esta função retorna um array de objetos CinemaMovie com informações detalhadas sobre cada filme.
  /// </summary>
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

  /// <summary>
  /// Pesquisa um filme por título, retornando o ID do filme se encontrado ou null caso contrário.
  /// </summary>
  searchMovieByTitle(titulo: string): Observable<number | null> {
    // NUNCA uses localhost:5185 aqui, senão o site no Azure vai falhar sempre
    return this.http.get<any>(`${this.cinemaApi}/search-tmdb?titulo=${encodeURIComponent(titulo)}`).pipe(
      map(res => res.id ?? null),
      catchError(() => of(null))
    );
  }
}
