import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';


/// <summary>
/// Serviço para operações relacionadas com atores, incluindo obtenção de atores populares, pesquisa de atores, detalhes de atores e filmes associados.
/// </summary>
export interface PopularActor {
  id: number;
  nome: string;
  fotoUrl: string;
  popularidade: number;
}


/// <summary>
/// Resultado da pesquisa de atores, contendo informações básicas como ID, nome e URL da foto.
/// </summary>
export interface ActorSearchResult {
  id: number;
  nome: string;
  fotoUrl: string;
}


/// <summary>
/// Representa um filme associado a um ator, incluindo informações como ID, título, URL do poster, personagem interpretada e data de lançamento.
/// </summary>
export interface ActorMovie {
  id: number;
  titulo: string;
  posterUrl: string | null;
  personagem?: string | null;
  dataLancamento?: string | null;
}


/// <summary>
/// Detalhes completos de um ator, incluindo informações como biografia, data de nascimento, local de nascimento, departamento e data de falecimento (se aplicável).
/// </summary>
export interface ActorDetails {
  id: number;
  nome: string;
  fotoUrl: string | null;
  biografia?: string | null;
  dataNascimento?: string | null;
  localNascimento?: string | null;
  departamento?: string | null;
  dataFalecimento?: string | null;
}


/// <summary>
/// Serviço para operações relacionadas com atores, incluindo obtenção de atores populares, pesquisa de atores, detalhes de atores e filmes associados.
/// </summary>
@Injectable({ providedIn: 'root' })
export class AtoresService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/atores` : '/api/atores';


  /// <summary>
  /// Serviço para operações relacionadas com atores, incluindo obtenção de atores populares, pesquisa de atores, detalhes de atores e filmes associados.
  /// </summary>
  constructor(private http: HttpClient) { }


  /// <summary>
  /// Obtém uma lista de atores populares, com suporte para paginação.
  /// </summary>
  getPopular(page: number = 1, count: number = 10): Observable<PopularActor[]> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('count', count.toString());

    return this.http.get<PopularActor[]>(`${this.apiUrl}/popular`, { params });
  }

  /// <summary>
  /// Pesquisa atores com base em uma consulta fornecida.
  /// </summary>
  searchActors(query: string): Observable<ActorSearchResult[]> {
    const params = new HttpParams().set('query', query.trim());
    return this.http.get<ActorSearchResult[]>(`${this.apiUrl}/search`, { params });
  }

  /// <summary>
  /// Obtém uma lista de filmes associados a um ator específico.
  /// </summary>
  getMoviesByActor(personId: number): Observable<ActorMovie[]> {
    return this.http.get<ActorMovie[]>(`${this.apiUrl}/${personId}/movies`);
  }

  /// <summary>
  /// Obtém os detalhes completos de um ator específico.
  /// </summary>
  getActorDetails(personId: number): Observable<ActorDetails> {
    return this.http.get<ActorDetails>(`${this.apiUrl}/${personId}`);
  }
}

