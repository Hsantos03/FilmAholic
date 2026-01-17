import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { UserMoviesService } from '../../services/user-movies.service';
import { Filme, FilmesService } from '../../services/filmes.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {

  userName = localStorage.getItem('userName') || 'RandomUser';  
  joined = '14 hours ago';
  bio = 'Lorem ipsum dolor sit amet consectetur adipisicing elit. Quisque faucibus ex sapien vitae pellentesque sem placerat.';

  private apiBase = 'https://localhost:7277/api/Profile';

  catalogo: Filme[] = [];

  watchLater: any[] = [];
  watched: any[] = [];

  totalHours = 0;
  stats: any;

  favoriteMovieIds: number[] = [];

  // Modal / edit state
  isEditing = false;
  editUserName = '';
  editBio = '';

  // Delete account state
  isDeleting = false;
  deleteInput = '';
  deleteRequiredText = 'delete';
  isDeletingSaving = false;

  isSavingMovie = false;

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private userMoviesService: UserMoviesService,
    private filmesService: FilmesService
  ) {}

  ngOnInit(): void {
    const userId = localStorage.getItem('user_id');

    this.loadCatalogo();
    this.refreshAllListsAndStats();

    if (!userId) {
      console.warn('No user_id in localStorage — using fallback values.');
      return;
    }

    // Call backend GET api/Profile/{id} to fetch user data
    this.http
      .get<any>(`${this.apiBase}/${encodeURIComponent(userId)}`, { withCredentials: true })
      .subscribe({
        next: (res) => {
          // Backend returns fields like: id, userName, nome, sobrenome, email, dataCriacao
          this.userName = res?.userName ?? this.userName;
          this.bio = res?.bio ?? this.bio;

          if (res?.dataCriacao) {
            // Normalize server date to readable string
            this.joined = new Date(res.dataCriacao).toLocaleString();
          }
        },
        error: (err) => {
          console.warn('Failed to load profile from API; keeping local values.', err);
        }
      });
  }

  refreshAllListsAndStats(): void {
    this.loadLists();
    this.loadStats();
    this.loadTotalHours();
  }

  loadCatalogo(): void {
    this.filmesService.getAll().subscribe({
      next: (res) => (this.catalogo = res || []),
      error: () => (this.catalogo = [])
    });
  }

  loadLists(): void {
    this.userMoviesService.getList(false).subscribe({
      next: (res) => (this.watchLater = res || []),
      error: () => (this.watchLater = [])
    });

    this.userMoviesService.getList(true).subscribe({
      next: (res) => (this.watched = res || []),
      error: () => (this.watched = [])
    });
  }

  loadTotalHours(): void {
    this.userMoviesService.getTotalHours().subscribe({
      next: (h) => (this.totalHours = h ?? 0),
      error: () => (this.totalHours = 0)
    });
  }

  loadStats(): void {
    this.userMoviesService.getStats().subscribe({
      next: (res) => (this.stats = res),
      error: () => (this.stats = null)
    });
  }

  addToWatchLater(filmeId: number): void {
    this.addMovieToList(filmeId, false);
  }

  addToWatched(filmeId: number): void {
    this.addMovieToList(filmeId, true);
  }

  removeFromLists(filmeId: number): void {
    // Buscar o filme no catálogo pelo ID do seed
    const filme = this.catalogo.find(f => f.id === filmeId);
    if (!filme) return;
    
    // Encontrar o UserMovie correspondente pelo título
    const userMovie = [...this.watchLater, ...this.watched].find(
      x => x?.filme?.titulo === filme.titulo || x?.filme?.Titulo === filme.titulo
    );
    
    if (!userMovie || !userMovie.filmeId) {
      console.warn('Movie not found in user lists');
      return;
    }
    
    this.isSavingMovie = true;
    // Usar o ID real do filme na BD
    this.userMoviesService.removeMovie(userMovie.filmeId).subscribe({
      next: () => this.refreshAllListsAndStats(),
      error: (err) => console.warn('removeMovie failed', err),
      complete: () => (this.isSavingMovie = false)
    });
  }

  private addMovieToList(filmeId: number, jaViu: boolean): void {
    this.isSavingMovie = true;
    this.userMoviesService.addMovie(filmeId, jaViu).subscribe({
      next: () => this.refreshAllListsAndStats(),
      error: (err) => console.warn('addMovie failed', err),
      complete: () => (this.isSavingMovie = false)
    });
  }

  inWatchLater(filmeId: number): boolean {
    // Buscar o filme no catálogo pelo ID do seed
    const filme = this.catalogo.find(f => f.id === filmeId);
    if (!filme) return false;
    
    // Comparar por título (mais confiável que ID)
    return this.watchLater?.some(x => x?.filme?.titulo === filme.titulo || 
                                      x?.filme?.Titulo === filme.titulo);
  }

  inWatched(filmeId: number): boolean {
    // Buscar o filme no catálogo pelo ID do seed
    const filme = this.catalogo.find(f => f.id === filmeId);
    if (!filme) return false;
    
    // Comparar por título (mais confiável que ID)
    return this.watched?.some(x => x?.filme?.titulo === filme.titulo || 
                                   x?.filme?.Titulo === filme.titulo);
  }

  toggleFavorite(movieId: number): void {
    if (this.favoriteMovieIds.includes(movieId)) {
      this.favoriteMovieIds = this.favoriteMovieIds.filter(id => id !== movieId);
    } else if (this.favoriteMovieIds.length < 10) {
      this.favoriteMovieIds.push(movieId);
    }
  }

  get favoriteMovies(): any[] {
    return this.watched.filter(m =>
      this.favoriteMovieIds.includes(m.FilmeId)
    );
  }

  openEdit(): void {
    // populate the edit fields with current values
    this.editUserName = this.userName;
    this.editBio = this.bio;
    this.isEditing = true;
  }

  closeEdit(): void {
    this.isEditing = false;
  }

  saveChanges(): void {
    // Update UI immediately
    this.userName = this.editUserName;
    this.bio = this.editBio;
    this.isEditing = false;

    // Optionally persist to server if an update endpoint exists.
    const userId = localStorage.getItem('user_id');
    if (!userId) return;

    this.http
      .put<any>(
        `${this.apiBase}/${encodeURIComponent(userId)}`,
        { userName: this.userName, bio: this.bio },
        { withCredentials: true }
      )
      .subscribe({
        next: () => {
          // success - nothing else required for now
        },
        error: (err) => {
          console.warn('Failed to persist profile changes to API.', err);
        }
      });
  }

  // Delete account related methods
  openDeleteConfirm(): void {
    this.deleteInput = '';
    this.isDeleting = true;
  }

  closeDeleteConfirm(): void {
    this.isDeleting = false;
  }

  confirmDelete(): void {
    // accept trimmed, case-insensitive "delete" to reduce UX friction
    if ((this.deleteInput || '').trim().toLowerCase() !== this.deleteRequiredText) {
      return;
    }

    const userId = localStorage.getItem('user_id');
    if (!userId) {
      console.warn('No user_id in localStorage; cannot delete account.');
      return;
    }

    this.isDeletingSaving = true;
    this.http.delete<any>(`${this.apiBase}/${encodeURIComponent(userId)}`, { withCredentials: true })
      .subscribe({
        next: () => {
          // on successful deletion, sign out and let AuthService handle navigation/cleanup
          this.authService.logout();
        },
        error: (err) => {
          console.warn('Failed to delete account via API.', err);
        },
        complete: () => {
          this.isDeletingSaving = false;
          this.isDeleting = false;
        }
      });
  }

  logout(): void {
    this.authService.logout();
  }
}
