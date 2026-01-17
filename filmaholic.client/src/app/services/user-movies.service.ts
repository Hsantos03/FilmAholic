@Injectable({ providedIn: 'root' })
export class UserMoviesService {
  private api = 'https://localhost:5001/api/usermovies';

  constructor(private http: HttpClient) { }

  addMovie(filmeId: number, jaViu: boolean) {
    return this.http.post(`${this.api}/add?filmeId=${filmeId}&jaViu=${jaViu}`, {});
  }

  removeMovie(filmeId: number) {
    return this.http.delete(`${this.api}/remove/${filmeId}`);
  }

  getTotalHours() {
    return this.http.get<number>('https://localhost:5001/api/usermovies/totalhours');
  }

}
