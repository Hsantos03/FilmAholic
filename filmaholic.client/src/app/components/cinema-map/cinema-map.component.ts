import { Component, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { CinemaService, CinemaVenue } from '../../services/cinema.service';
import { MenuService } from '../../services/menu.service';
import { AuthService } from '../../services/auth.service';
import * as L from 'leaflet';
import { OnboardingStep } from '../../services/onboarding.service';

// Fix Leaflet default icon 404 in Angular
const iconUrl = 'leaflet/marker-icon.png';
const iconRetinaUrl = 'leaflet/marker-icon-2x.png';
const shadowUrl = 'leaflet/marker-shadow.png';
const iconDefault = L.icon({
  iconUrl, iconRetinaUrl, shadowUrl,
  iconSize: [25, 41], iconAnchor: [12, 41],
  popupAnchor: [1, -34], shadowSize: [41, 41]
});
L.Marker.prototype.options.icon = iconDefault;

/// <summary>
/// Representa a localização do utilizador no mapa.
/// </summary>
const userLocationIcon = L.icon({
  iconUrl, iconRetinaUrl, shadowUrl,
  iconSize: [25, 41], iconAnchor: [12, 41],
  popupAnchor: [1, -34], shadowSize: [41, 41],
  className: 'user-location-marker-filter'
});

/// <summary>
/// Componente responsável por exibir um mapa interativo com os cinemas próximos, permitindo ao utilizador explorar os cinemas, marcar favoritos e ver a distância a cada cinema a partir da sua localização atual.
/// </summary>
@Component({
  selector: 'app-cinema-map',
  templateUrl: './cinema-map.component.html',
  styleUrls: ['./cinema-map.component.css']
})
export class CinemaMapComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('mapContainer', { static: false }) mapContainer!: ElementRef<HTMLDivElement>;

  map: any = null;
  cinemas: CinemaVenue[] = [];
  selectedCinemaId: string | null = null;
  userPosition: { lat: number; lng: number } | null = null;
  loading = true;
  geoError: string | null = null;
  cinemasError = false;

  /// <summary>
  /// Representa a configuração do tour de onboarding para o mapa de cinemas.
  /// </summary>
  readonly cinemaMapOnboardingSteps: OnboardingStep[] = [
    {
      selector: '[data-tour="cinema-map-menu"]',
      title: 'Menu',
      body: 'Acede às outras secções da app sem saires do mapa.'
    },
    {
      selector: '[data-tour="cinema-map-hero"]',
      title: 'Cinemas próximos',
      body: 'Esta página junta mapa e lista ordenada pela distância à tua localização.'
    },
    {
      selector: '[data-tour="cinema-map-map"]',
      title: 'Mapa',
      body: 'Explora marcadores, clica nos cinemas e usa o mapa para te orientares.'
    },
    {
      selector: '[data-tour="cinema-map-list"]',
      title: 'Lista e favoritos',
      body: 'Marca favoritos, vê distâncias e abre o site do cinema quando disponível.'
    }
  ];

  // Favoritos
  favoritosIds: Set<string> = new Set();
  togglingId: string | null = null;

  private markers: Map<string, any> = new Map();

  private geoDone = false;
  private cinemasDone = false;
  private geoErrorTimeoutId: ReturnType<typeof setTimeout> | null = null;

  private readonly PORTUGAL_CENTER = { lat: 38.7223, lng: -9.1393 };
  private readonly DEFAULT_ZOOM = 10;
  private readonly GEO_ERROR_DELAY_MS = 3500;
  private readonly API = '/api/cinema';

  /// <summary>
  /// Representa a configuração do tour de onboarding para o mapa de cinemas.
  /// </summary>
  constructor(
    private cinemaService: CinemaService,
    private http: HttpClient,
    private router: Router,
    public menuService: MenuService,
    private authService: AuthService
  ) { }

  /// <summary>
  /// Verifica se o utilizador tem perfil de administrador para mostrar opções avançadas no menu.
  /// </summary>
  get isAdmin(): boolean {
    return this.authService.isAdministrador();
  }

  /// <summary>
  /// Alterna a visibilidade do menu lateral.
  /// </summary>
  toggleMenu(): void {
    this.menuService.toggle();
  }

  /// <summary>
  /// Navega para o dashboard de desafios.
  /// </summary>
  goToDashboardDesafios(): void {
    this.router.navigate(['/dashboard'], { queryParams: { openDesafios: '1' } });
  }

  /// <summary>
  /// Método de ciclo de vida do Angular que é chamado quando o componente é inicializado.
  ///Inicia o carregamento dos cinemas, solicita a geolocalização do utilizador e carrega os favoritos.
  /// </summary>
  ngOnInit(): void {
    this.loadCinemas();
    this.requestGeolocation();
    this.loadFavoritos();
  }

  /// <summary>
  /// Método de ciclo de vida do Angular que é chamado após a inicialização da view do componente.
  /// </summary>
  ngAfterViewInit(): void { }

  /// <summary>
  /// Método de ciclo de vida do Angular que é chamado quando o componente é destruído.
  /// </summary>
  ngOnDestroy(): void {
    if (this.geoErrorTimeoutId != null) clearTimeout(this.geoErrorTimeoutId);
    if (this.map) { this.map.remove(); this.map = null; }
  }


  // ─── FAVORITOS ─────
  /// <summary>
  /// Carrega os cinemas favoritos do utilizador a partir do backend e armazena os IDs em um Set para fácil verificação.
  /// </summary>
  private loadFavoritos(): void {
    this.http.get<string[]>(`${this.API}/cinemas-favoritos`, { withCredentials: true })
      .subscribe({
        next: (ids) => { this.favoritosIds = new Set(ids); },
        error: () => { }
      });
  }

  /// <summary>
  /// Verifica se um cinema específico está marcado como favorito pelo utilizador, consultando o Set de IDs de favoritos.
  /// </summary>
  isFavorito(cinema: CinemaVenue): boolean {
    return this.favoritosIds.has(this.cinemaId(cinema));
  }

  /// <summary>
  /// Alterna o estado de favorito de um cinema específico para o utilizador.
  /// </summary>
  toggleFavorito(cinema: CinemaVenue): void {
    const id = this.cinemaId(cinema);
    if (this.togglingId === id) return;
    this.togglingId = id;

    this.http.post<{ cinemaId: string; isFavorito: boolean }>(
      `${this.API}/cinemas-favoritos/toggle`,
      { cinemaId: id },
      { withCredentials: true }
    ).subscribe({
      next: (res) => {
        if (res.isFavorito) {
          this.favoritosIds.add(id);
        } else {
          this.favoritosIds.delete(id);
        }
        this.togglingId = null;
      },
      error: () => { this.togglingId = null; }
    });
  }

  // ID único para cada cinema baseado no nome + coordenadas
  /// <summary>
  /// Gera um ID único para um cinema com base no seu nome e coordenadas geográficas (latitude e longitude).
  /// </summary>
  cinemaId(c: CinemaVenue): string {
    return `${c.nome}|${c.latitude}|${c.longitude}`;
  }


  // ─── GEO + CINEMAS ────
  /// <summary>
  /// Solicita a geolocalização do utilizador usando a API de Geolocalização do navegador. Se a geolocalização for obtida com sucesso,
  /// armazena a posição do utilizador, calcula as distâncias para os cinemas e inicializa o mapa.Se ocorrer um erro ou se o navegador não suportar geolocalização, exibe uma mensagem de erro apropriada.
  /// </summary>
  private requestGeolocation(): void {
    if (!navigator.geolocation) {
      this.geoError = 'O seu browser não suporta geolocalização.';
      this.geoDone = true;
      this.tryFinishAndInitMap();
      return;
    }
    navigator.geolocation.getCurrentPosition(
      (position) => {
        if (this.geoErrorTimeoutId != null) {
          clearTimeout(this.geoErrorTimeoutId);
          this.geoErrorTimeoutId = null;
        }
        this.geoError = null;
        this.userPosition = { lat: position.coords.latitude, lng: position.coords.longitude };
        this.calculateDistances();
        this.geoDone = true;
        this.tryFinishAndInitMap();
        if (this.map) this.addUserMarkerAndFit();
      },
      (err) => {
        let message: string;
        switch (err.code) {
          case err.PERMISSION_DENIED:
            message = 'Permissão de localização negada. Ative a localização para ver a distância aos cinemas.'; break;
          case err.POSITION_UNAVAILABLE:
            message = 'Localização indisponível.'; break;
          case err.TIMEOUT:
            message = 'Tempo esgotado ao obter a localização.'; break;
          default:
            message = 'Não foi possível obter a sua localização.';
        }
        this.geoDone = true;
        this.tryFinishAndInitMap();
        this.geoErrorTimeoutId = setTimeout(() => {
          this.geoErrorTimeoutId = null;
          if (!this.userPosition) this.geoError = message;
        }, this.GEO_ERROR_DELAY_MS);
      },
      { enableHighAccuracy: true, timeout: 15000, maximumAge: 60000 }
    );
  }

  /// <summary>
  /// Carrega a lista de cinemas próximos do utilizador.
  /// </summary>
  private loadCinemas(): void {
    this.cinemaService.getNearbyCinemas().subscribe({
      next: (list) => {
        this.cinemas = list || [];
        this.cinemasError = this.cinemas.length === 0;
        if (this.userPosition) this.calculateDistances();
        this.cinemasDone = true;
        this.tryFinishAndInitMap();
      },
      error: () => {
        this.cinemas = [];
        this.cinemasError = true;
        this.cinemasDone = true;
        this.tryFinishAndInitMap();
      }
    });
  }

  /// <summary>
  /// Tenta finalizar o carregamento dos dados e inicializar o mapa.
  /// </summary>
  private tryFinishAndInitMap(): void {
    if (!this.geoDone || !this.cinemasDone) return;
    this.loading = false;
    setTimeout(() => this.initMap(), 150);
  }

  /// <summary>
  /// Calcula as distâncias entre o utilizador e os cinemas.
  /// </summary>
  private calculateDistances(): void {
    if (!this.userPosition) return;
    this.cinemas.forEach(c => {
      c.distanceKm = this.haversineKm(
        this.userPosition!.lat, this.userPosition!.lng,
        c.latitude, c.longitude
      );
    });
    this.cinemas.sort((a, b) => (a.distanceKm ?? 999) - (b.distanceKm ?? 999));
  }

  /// <summary>
  /// Calcula a distância entre dois pontos geográficos usando a fórmula de Haversine.
  /// </summary>
  private haversineKm(lat1: number, lon1: number, lat2: number, lon2: number): number {
    const R = 6371;
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;
    const a =
      Math.sin(dLat / 2) ** 2 +
      Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
      Math.sin(dLon / 2) ** 2;
    return Math.round(6371 * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a)) * 10) / 10;
  }

  /// <summary>
  /// Inicializa o mapa Leaflet, centralizando-o na posição do utilizador (ou no centro de Portugal se a posição não estiver disponível) e adicionando marcadores para cada cinema.
  /// </summary>
  private initMap(): void {
    if (!this.mapContainer?.nativeElement || this.map) return;

    const center = this.userPosition ?? this.PORTUGAL_CENTER;
    const zoom = this.userPosition ? 12 : this.DEFAULT_ZOOM;

    this.map = L.map(this.mapContainer.nativeElement).setView([center.lat, center.lng], zoom);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(this.map);

    if (this.userPosition) {
      L.marker([this.userPosition.lat, this.userPosition.lng], { icon: userLocationIcon })
        .addTo(this.map)
        .bindPopup('<strong>A sua localização</strong>')
        .openPopup();
    }

    this.cinemas.forEach(c => {
      const distText = c.distanceKm != null ? `<br><strong>Distância:</strong> ${c.distanceKm} km` : '';
      const popup = `<strong>${this.escapeHtml(c.nome)}</strong><br>${this.escapeHtml(c.morada)}${distText}`;
      const marker = L.marker([c.latitude, c.longitude]).addTo(this.map).bindPopup(popup);
      const id = this.cinemaId(c);
      this.markers.set(id, marker);
      marker.on('click', () => {
        this.selectedCinemaId = id;
        this.scrollToCardById(id);
      });
    });
  }

  /// <summary>
  /// Centraliza o mapa no marcador do cinema selecionado e abre o popup correspondente.
  /// Também atualiza o estado do cinema selecionado para destacar a card correspondente na lista e rola a lista para garantir que a card esteja visível.
  /// </summary>
  flyToMarker(cinema: CinemaVenue): void {
    if (!this.map) return;
    this.map.flyTo([cinema.latitude, cinema.longitude], 15, { duration: 1.2 });
    const id = this.cinemaId(cinema);
    const marker = this.markers.get(id);
    if (marker) marker.openPopup();
    this.selectedCinemaId = id;
    // Scroll para o mapa
    document.querySelector('.map-wrap')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }
  
  /// <summary>
  /// Adiciona um marcador para a posição do utilizador no mapa e ajusta a vista para centralizar nesse marcador.
  /// </summary>
  private addUserMarkerAndFit(): void {
    if (!this.map || !this.userPosition) return;
    L.marker([this.userPosition.lat, this.userPosition.lng], { icon: userLocationIcon })
      .addTo(this.map)
      .bindPopup('<strong>A sua localização</strong>')
      .openPopup();
    this.map.setView([this.userPosition.lat, this.userPosition.lng], 12);
  }
  
  /// <summary>
  /// Escapa caracteres HTML em uma string para evitar injeção de código.
  /// </summary>
  private escapeHtml(text: string): string {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }

  /// <summary>
  /// Retorna a lista de cinemas ordenada, colocando os favoritos primeiro e depois ordenando por distância.
  /// </summary>
  get sortedCinemas(): CinemaVenue[] {
    return [...this.cinemas].sort((a, b) => {
      const aFav = this.isFavorito(a) ? 0 : 1;
      const bFav = this.isFavorito(b) ? 0 : 1;
      if (aFav !== bFav) return aFav - bFav;
      return (a.distanceKm ?? 999) - (b.distanceKm ?? 999);
    });
  }
  
  /// <summary>
  /// Manipula o clique em uma card de cinema, centralizando o mapa no marcador correspondente.
  /// </summary>
  onCardClick(cinema: CinemaVenue): void {
    this.selectedCinemaId = this.cinemaId(cinema);
    this.flyToMarker(cinema);
  }
  
  /// <summary>
  /// Rola a lista de cinemas para garantir que a card correspondente ao ID fornecido esteja visível.
  /// </summary>
  private scrollToCardById(id: string): void {
    const el = document.querySelector(`.cinema-card[data-id="${id}"]`) as HTMLElement | null;
    if (el) {
      el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }
}
