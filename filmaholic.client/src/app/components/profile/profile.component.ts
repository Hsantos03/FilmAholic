import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { UserMoviesService } from '../../services/user-movies.service';
import { Filme, FilmesService } from '../../services/filmes.service';
import { FavoritesService, FavoritosDTO } from '../../services/favorites.service';

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

  // ✅ FR06 - Favorites
  favoritosFilmes: number[] = [];
  favoritosAtores: string[] = [];
  novoAtor = '';
  showOnlyFavorites = false;
  isSavingFavorites = false;

  // Modal / edit state
  isEditing = false;
  editUserName = '';
  editBio = '';

  // Delete account state
  isDeleting = false;
  deleteInput = '';
  deleteRequiredText = 'delete';
  isDeletingSaving = false;

  // List view modal state
  showListModal = false;
  currentListType: 'watchLater' | 'watched' | null = null;

  // movies saving state
  isSavingMovie = false;

  // -- new: watch later & watched filter state
  watchLaterFilter: 'all' | 'newest' | 'oldest' | '7days' | '30days' = 'all';
  watchedFilter: 'all' | 'newest' | 'oldest' | '7days' | '30days' = 'all';

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private userMoviesService: UserMoviesService,
    private filmesService: FilmesService,
    private favoritesService: FavoritesService
  ) { }

  ngOnInit(): void {
    const userId = localStorage.getItem('user_id');

    this.loadCatalogo();
    this.refreshAllListsAndStats();
    this.loadFavorites();

    if (!userId) {
      console.warn('No user_id in localStorage â€” using fallback values.');
      return;
    }

    this.http
      .get<any>(`${this.apiBase}/${encodeURIComponent(userId)}`, { withCredentials: true })
      .subscribe({
        next: (res) => {
          // Backend returns fields like: id, userName, nome, sobrenome, email, dataCriacao
          // Use userName if available and not empty, otherwise use nome + sobrenome, fallback to nome
          if (res?.userName && res.userName.trim() && res.userName !== res?.email) {
            this.userName = res.userName;
          } else if (res?.nome) {
            // Use nome + sobrenome if available, otherwise just nome
            this.userName = res.sobrenome ? `${res.nome} ${res.sobrenome}` : res.nome;
          } else {
            // Keep current userName as fallback
            this.userName = this.userName || 'User';
          }
          this.bio = res?.bio ?? this.bio;

          if (res?.dataCriacao) {
            this.joined = new Date(res.dataCriacao).toLocaleString();
          }
        },
        error: (err) => console.warn('Failed to load profile from API; keeping local values.', err)
      });
  }

  // -----------------------
  // LOADERS (catalog/lists)
  // -----------------------
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

  // -----------------------
  // FR05 - Watch later / Watched
  // -----------------------
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
    this.userMoviesService.removeMovie(userMovie.filmeId).subscribe({
      next: () => this.refreshAllListsAndStats(),
      error: (err) => console.warn('removeMovie failed', err),
      complete: () => (this.isSavingMovie = false)
    });
  }

  private addMovieToList(filmeId: number, jaViu: boolean): void {
    // Optimistic UI update: add a temporary entry to appropriate list
    const filmFromSeed = this.catalogo.find(f => f.id === filmeId);

    if (filmFromSeed) {
      if (!jaViu) {
        // Watch later optimistic add
        const already = this.watchLater?.some(x => x?.filme?.titulo === filmFromSeed.titulo || x?.filme?.Titulo === filmFromSeed.titulo);
        if (!already) {
          const tempEntry = {
            filme: filmFromSeed,
            filmeId: -1,
            JaViu: false,
            Data: new Date().toISOString()
          };
          this.watchLater = [tempEntry, ...this.watchLater];
        }
      } else {
        // Watched optimistic add
        const already = this.watched?.some(x => x?.filme?.titulo === filmFromSeed.titulo || x?.filme?.Titulo === filmFromSeed.titulo);
        if (!already) {
          const tempEntry = {
            filme: filmFromSeed,
            filmeId: -1,
            JaViu: true,
            Data: new Date().toISOString()
          };
          this.watched = [tempEntry, ...this.watched];
        }
      }
    }

    this.isSavingMovie = true;
    this.userMoviesService.addMovie(filmeId, jaViu).subscribe({
      next: () => this.refreshAllListsAndStats(),
      error: (err) => {
        console.warn('addMovie failed', err);
        // rollback optimistic add if it exists
        if (filmFromSeed) {
          if (!jaViu) {
            this.watchLater = this.watchLater.filter(
              x => !(x?.filme?.titulo === filmFromSeed.titulo || x?.filme?.Titulo === filmFromSeed.titulo) || x?.filmeId > 0
            );
          } else {
            this.watched = this.watched.filter(
              x => !(x?.filme?.titulo === filmFromSeed.titulo || x?.filme?.Titulo === filmFromSeed.titulo) || x?.filmeId > 0
            );
          }
        }
      },
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
    
    return this.watched?.some(x => x?.filme?.titulo === filme.titulo || 
                                   x?.filme?.Titulo === filme.titulo);
  }

  // -----------------------
  // ✅ FR06 - Favorites (Top 10)
  // -----------------------
  loadFavorites(): void {
    this.favoritesService.getFavorites().subscribe({
      next: (fav: FavoritosDTO) => {
        this.favoritosFilmes = (fav?.filmes || []).slice(0, 10);
        this.favoritosAtores = (fav?.atores || []).slice(0, 10);
      },
      error: () => {
        this.favoritosFilmes = [];
        this.favoritosAtores = [];
      }
    });
  }

  isFilmeFavorito(filmeId: number): boolean {
    return this.favoritosFilmes.includes(filmeId);
  }

  toggleFavoriteFilme(filmeId: number): void {
    const idx = this.favoritosFilmes.indexOf(filmeId);

    if (idx >= 0) {
      this.favoritosFilmes.splice(idx, 1);
    } else {
      if (this.favoritosFilmes.length >= 10) return;
      this.favoritosFilmes.push(filmeId);
    }

    this.saveFavorites();
  }

  addAtorFavorito(): void {
    const nome = (this.novoAtor || '').trim();
    if (!nome) return;
    if (this.favoritosAtores.includes(nome)) {
      this.novoAtor = '';
      return;
    }
    if (this.favoritosAtores.length >= 10) return;

    this.favoritosAtores.push(nome);
    this.novoAtor = '';
    this.saveFavorites();
  }

  removeAtorFavorito(nome: string): void {
    this.favoritosAtores = this.favoritosAtores.filter(a => a !== nome);
    this.saveFavorites();
  }

  private saveFavorites(): void {
    this.isSavingFavorites = true;

    const dto: FavoritosDTO = {
      filmes: this.favoritosFilmes.slice(0, 10),
      atores: this.favoritosAtores.slice(0, 10)
    };

    this.favoritesService.saveFavorites(dto).subscribe({
      next: () => { },
      error: (err) => console.warn('saveFavorites failed', err),
      complete: () => (this.isSavingFavorites = false)
    });
  }

  // ✅ sub-task: filtro (catálogo só favoritos)
  get catalogoFiltrado(): Filme[] {
    if (!this.showOnlyFavorites) return this.catalogo;
    return (this.catalogo || []).filter(f => this.favoritosFilmes.includes(f.id));
  }

  // para renderizar posters do Top10 de forma bonita
  get favoritosFilmesDetalhes(): Filme[] {
    return this.favoritosFilmes
      .map(id => this.catalogo.find(f => f.id === id))
      .filter((x): x is Filme => !!x);
  }

  // -----------------------
  // Manage account modal
  // -----------------------
  openEdit(): void {
    this.editUserName = this.userName;
    this.editBio = this.bio;
    this.isEditing = true;
  }

  closeEdit(): void {
    this.isEditing = false;
  }

  saveChanges(): void {
    this.userName = this.editUserName;
    this.bio = this.editBio;
    this.isEditing = false;

    const userId = localStorage.getItem('user_id');
    if (!userId) return;

    this.http
      .put<any>(
        `${this.apiBase}/${encodeURIComponent(userId)}`,
        { userName: this.userName, bio: this.bio },
        { withCredentials: true }
      )
      .subscribe({
        next: () => { },
        error: (err) => console.warn('Failed to persist profile changes to API.', err)
      });
  }

  // -----------------------
  // Delete account modal
  // -----------------------
  openDeleteConfirm(): void {
    this.deleteInput = '';
    this.isDeleting = true;
  }

  closeDeleteConfirm(): void {
    this.isDeleting = false;
  }

  confirmDelete(): void {
    if ((this.deleteInput || '').trim().toLowerCase() !== this.deleteRequiredText) return;

    const userId = localStorage.getItem('user_id');
    if (!userId) return;

    this.isDeletingSaving = true;

    this.http.delete<any>(`${this.apiBase}/${encodeURIComponent(userId)}`, { withCredentials: true })
      .subscribe({
        next: () => this.authService.logout(),
        error: (err) => console.warn('Failed to delete account via API.', err),
        complete: () => {
          this.isDeletingSaving = false;
          this.isDeleting = false;
        }
      });
  }

  openListModal(type: 'watchLater' | 'watched'): void {
    this.currentListType = type;
    this.showListModal = true;
    // reset filter when opening list
    if (type === 'watchLater') this.watchLaterFilter = 'all';
    if (type === 'watched') this.watchedFilter = 'all';
  }

  closeListModal(): void {
    this.showListModal = false;
    this.currentListType = null;
  }

  // new: filtered list that applies filters for watch-later and watched
  get filteredCurrentList(): any[] {
    const list = this.currentList || [];

    // normalize date value getter
    const parseDate = (m: any): Date | null => {
      const v = m?.data ?? m?.Data;
      if (!v) return null;
      const d = new Date(v);
      return isNaN(d.getTime()) ? null : d;
    };

    const applyFilter = (filter: string) => {
      switch (filter) {
        case 'newest':
          return [...list].sort((a, b) => {
            const da = parseDate(a)?.getTime() ?? 0;
            const db = parseDate(b)?.getTime() ?? 0;
            return db - da;
          });
        case 'oldest':
          return [...list].sort((a, b) => {
            const da = parseDate(a)?.getTime() ?? 0;
            const db = parseDate(b)?.getTime() ?? 0;
            return da - db;
          });
        case '7days': {
          const cutoff = Date.now() - 7 * 24 * 60 * 60 * 1000;
          return list.filter(m => (parseDate(m)?.getTime() ?? 0) >= cutoff)
                     .sort((a, b) => (parseDate(b)?.getTime() ?? 0) - (parseDate(a)?.getTime() ?? 0));
        }
        case '30days': {
          const cutoff = Date.now() - 30 * 24 * 60 * 60 * 1000;
          return list.filter(m => (parseDate(m)?.getTime() ?? 0) >= cutoff)
                     .sort((a, b) => (parseDate(b)?.getTime() ?? 0) - (parseDate(a)?.getTime() ?? 0));
        }
        case 'all':
        default:
          return [...list].sort((a, b) => (parseDate(b)?.getTime() ?? 0) - (parseDate(a)?.getTime() ?? 0));
      }
    };

    if (this.currentListType === 'watchLater') {
      return applyFilter(this.watchLaterFilter);
    } else if (this.currentListType === 'watched') {
      return applyFilter(this.watchedFilter);
    }

    return list;
  }

  get currentList(): any[] {
    if (this.currentListType === 'watchLater') {
      return this.watchLater;
    } else if (this.currentListType === 'watched') {
      return this.watched;
    }
    return [];
  }

  get currentListTitle(): string {
    if (this.currentListType === 'watchLater') {
      return 'Watch Later';
    } else if (this.currentListType === 'watched') {
      return 'Watched';
    }
    return '';
  }

  logout(): void {
    this.authService.logout();
  }
}
