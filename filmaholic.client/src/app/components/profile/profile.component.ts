import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { UserMoviesService, StatsComparison } from '../../services/user-movies.service';
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
  bio = '';
  fotoPerfilUrl: string | null = null;
  capaUrl: string | null = null;

  // XP / Level
  xp = 0;
  level = 0;

  private apiBase = 'https://localhost:7277/api/Profile';

  catalogo: Filme[] = [];
  watchLater: any[] = [];
  watched: any[] = [];

  totalHours = 0;
  stats: any;

  // FR38 - Stats comparison
  statsComparison: StatsComparison | null = null;
  isLoadingComparison = false;

  // FR06 - Favorites
  favoritosFilmes: number[] = [];
  favoritosAtores: string[] = [];
  novoAtor = '';
  showOnlyFavorites = false;
  isSavingFavorites = false;

  // Drag and drop state (filmes)
  draggedIndex: number | null = null;
  dragOverIndex: number | null = null;

  // Drag and drop state (atores)
  draggedActorIndex: number | null = null;
  dragOverActorIndex: number | null = null;

  // Modal / edit state
  isEditing = false;
  editUserName = '';
  editBio = '';
  editFotoPerfilUrl: string | null = null;
  editCapaUrl: string | null = null;
  
  // Separate modals for images
  isEditingCapa = false;
  isEditingAvatar = false;

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

  watchLaterFilter: 'all' | 'newest' | 'oldest' | '7days' | '30days' = 'all';
  watchedFilter: 'all' | 'newest' | 'oldest' | '7days' | '30days' = 'all';

  constructor(
    private http: HttpClient,
    private router: Router,
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

    this.favoritesService.favoritesChanged$
      .subscribe(() => {
        this.loadFavorites();
      });

    if (!userId) {
      console.warn('No user_id in localStorage Ã¢â‚¬â€ using fallback values.');
      return;
    }

    this.http
      .get<any>(`${this.apiBase}/${encodeURIComponent(userId)}`, { withCredentials: true })
      .subscribe({
        next: (res) => {
          if (res?.userName && res.userName.trim() && res.userName !== res?.email) {
            this.userName = res.userName;
          } else if (res?.nome) {
            this.userName = res.sobrenome ? `${res.nome} ${res.sobrenome}` : res.nome;
          } else {
            this.userName = this.userName || 'User';
          }
          this.bio = res?.bio ?? '';
          this.fotoPerfilUrl = res?.fotoPerfilUrl ?? null;
          this.capaUrl = res?.capaUrl ?? null;

          if (res?.dataCriacao) {
            this.joined = new Date(res.dataCriacao).toLocaleString();
          }

          // XP / Level
          this.xp = res?.xp ?? 0;
          this.level = Math.floor(this.xp / 10);
        },
        error: (err) => console.warn('Failed to load profile from API; keeping local values.', err)
      });
  }

  refreshAllListsAndStats(): void {
    this.loadLists();
    this.loadStats();
    this.loadTotalHours();
    this.loadStatsComparison();
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

  // FR38 - Load stats comparison
  loadStatsComparison(): void {
    this.isLoadingComparison = true;
    this.userMoviesService.getStatsComparison().subscribe({
      next: (res) => {
        this.statsComparison = res;
        this.isLoadingComparison = false;
      },
      error: () => {
        this.statsComparison = null;
        this.isLoadingComparison = false;
      }
    });
  }

  getComparisonBarWidth(userValue: number, globalValue: number): number {
    const max = Math.max(userValue, globalValue, 1);
    return (userValue / max) * 100;
  }

  getGlobalBarWidth(userValue: number, globalValue: number): number {
    const max = Math.max(userValue, globalValue, 1);
    return (globalValue / max) * 100;
  }

  addToWatchLater(filmeId: number): void {
    this.addMovieToList(filmeId, false);
  }

  addToWatched(filmeId: number): void {
    this.addMovieToList(filmeId, true);
  }

  removeFromLists(filmeId: number): void {
    const filme = this.catalogo.find(f => f.id === filmeId);
    if (!filme) return;
    
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
    const filmFromSeed = this.catalogo.find(f => f.id === filmeId);

    if (filmFromSeed) {
      if (!jaViu) {
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
    const filme = this.catalogo.find(f => f.id === filmeId);
    if (!filme) return false;
    
    return this.watchLater?.some(x => x?.filme?.titulo === filme.titulo || 
                                      x?.filme?.Titulo === filme.titulo);
  }

  inWatched(filmeId: number): boolean {
    const filme = this.catalogo.find(f => f.id === filmeId);
    if (!filme) return false;
    
    return this.watched?.some(x => x?.filme?.titulo === filme.titulo || 
                                   x?.filme?.Titulo === filme.titulo);
  }

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

  removeFromFavorites(filmeId: number): void {
    const idx = this.favoritosFilmes.indexOf(filmeId);
    if (idx >= 0) {
      this.favoritosFilmes.splice(idx, 1);
      this.saveFavorites();
    }
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

  get catalogoFiltrado(): Filme[] {
    if (!this.showOnlyFavorites) return this.catalogo;
    return (this.catalogo || []).filter(f => this.favoritosFilmes.includes(f.id));
  }

  get favoritosFilmesDetalhes(): Filme[] {
    return this.favoritosFilmes
      .map(id => this.catalogo.find(f => f.id === id))
      .filter((x): x is Filme => !!x);
  }

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
        { 
          userName: this.userName, 
          bio: this.bio
        },
        { withCredentials: true }
      )
      .subscribe({
        next: () => { },
        error: (err) => console.warn('Failed to persist profile changes to API.', err)
      });
  }

  // Edit Cover Photo
  openEditCapa(): void {
    this.editCapaUrl = this.capaUrl;
    this.isEditingCapa = true;
  }

  closeEditCapa(): void {
    this.isEditingCapa = false;
  }

  saveCapa(): void {
    this.capaUrl = this.editCapaUrl;
    this.isEditingCapa = false;

    const userId = localStorage.getItem('user_id');
    if (!userId) return;

    this.http
      .put<any>(
        `${this.apiBase}/${encodeURIComponent(userId)}`,
        { capaUrl: this.capaUrl },
        { withCredentials: true }
      )
      .subscribe({
        next: () => { },
        error: (err) => console.warn('Failed to save cover photo.', err)
      });
  }

  // Edit Avatar
  openEditAvatar(): void {
    this.editFotoPerfilUrl = this.fotoPerfilUrl;
    this.isEditingAvatar = true;
  }

  closeEditAvatar(): void {
    this.isEditingAvatar = false;
  }

  saveAvatar(): void {
    this.fotoPerfilUrl = this.editFotoPerfilUrl;
    this.isEditingAvatar = false;

    const userId = localStorage.getItem('user_id');
    if (!userId) return;

    this.http
      .put<any>(
        `${this.apiBase}/${encodeURIComponent(userId)}`,
        { fotoPerfilUrl: this.fotoPerfilUrl },
        { withCredentials: true }
      )
      .subscribe({
        next: () => { },
        error: (err) => console.warn('Failed to save avatar.', err)
      });
  }

  onAvatarFileSelected(event: any): void {
    const file = event.target.files[0];
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
      alert('A imagem e muito grande. Por favor, escolha uma imagem menor que 5MB.');
      return;
    }

    const reader = new FileReader();
    reader.onload = (e: any) => {
      this.editFotoPerfilUrl = e.target.result;
    };
    reader.readAsDataURL(file);
  }

  onCapaFileSelected(event: any): void {
    const file = event.target.files[0];
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
      alert('A imagem e muito grande. Por favor, escolha uma imagem menor que 5MB.');
      return;
    }

    const reader = new FileReader();
    reader.onload = (e: any) => {
      this.editCapaUrl = e.target.result;
    };
    reader.readAsDataURL(file);
  }

  goToHome(): void {
    this.router.navigate(['/dashboard']);
  }

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
    if (type === 'watchLater') this.watchLaterFilter = 'all';
    if (type === 'watched') this.watchedFilter = 'all';
  }

  closeListModal(): void {
    this.showListModal = false;
    this.currentListType = null;
  }

  get filteredCurrentList(): any[] {
    const list = this.currentList || [];

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

  onDragStart(event: DragEvent, index: number): void {
    this.draggedIndex = index;
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/html', index.toString());
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move';
    }
    
    const target = event.currentTarget as HTMLElement;
    const index = parseInt(target.getAttribute('data-index') || '0', 10);
    this.dragOverIndex = index;
  }

  onDrop(event: DragEvent, dropIndex: number): void {
    event.preventDefault();
    event.stopPropagation();

    if (this.draggedIndex === null || this.draggedIndex === dropIndex) {
      this.dragOverIndex = null;
      return;
    }

    // Reordenar o array de favoritos
    const newOrder = [...this.favoritosFilmes];
    const [removed] = newOrder.splice(this.draggedIndex, 1);
    newOrder.splice(dropIndex, 0, removed);

    this.favoritosFilmes = newOrder;
    this.saveFavorites();

    // Limpar estados
    this.draggedIndex = null;
    this.dragOverIndex = null;
  }

  onDragEnd(): void {
    document.querySelectorAll('.fav-poster').forEach(el => {
      const htmlEl = el as HTMLElement;
      htmlEl.style.opacity = '';
      htmlEl.style.transform = '';
    });
    this.draggedIndex = null;
    this.dragOverIndex = null;
  }

  onActorDragStart(event: DragEvent, index: number): void {
    this.draggedActorIndex = index;
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/html', index.toString());
    }
  }

  onActorDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move';
    }
    
    const target = event.currentTarget as HTMLElement;
    const index = parseInt(target.getAttribute('data-index') || '0', 10);
    this.dragOverActorIndex = index;
  }

  onActorDrop(event: DragEvent, dropIndex: number): void {
    event.preventDefault();
    event.stopPropagation();

    if (this.draggedActorIndex === null || this.draggedActorIndex === dropIndex) {
      this.dragOverActorIndex = null;
      return;
    }

    // Reordenar o array de atores
    const newOrder = [...this.favoritosAtores];
    const [removed] = newOrder.splice(this.draggedActorIndex, 1);
    newOrder.splice(dropIndex, 0, removed);

    this.favoritosAtores = newOrder;
    this.saveFavorites();

    // Limpar estados
    this.draggedActorIndex = null;
    this.dragOverActorIndex = null;
  }

  onActorDragEnd(): void {
    // Limpar feedback visual e remover estilos inline
    document.querySelectorAll('.fav-actor-card').forEach(el => {
      const htmlEl = el as HTMLElement;
      htmlEl.style.opacity = '';
      htmlEl.style.transform = '';
    });
    this.draggedActorIndex = null;
    this.dragOverActorIndex = null;
  }
}
