import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // Substitui pela porta que aparece no teu Swagger
  private apiUrl = 'https://localhost:7277/api/autenticacao';

  constructor(private http: HttpClient) { }

  registar(dados: any): Observable<any> {
    // Envia o Nome, Sobrenome, DataNascimento, Email e Password
    return this.http.post(`${this.apiUrl}/registar`, dados);
  }

  login(dados: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/login`, dados);
  }
}
