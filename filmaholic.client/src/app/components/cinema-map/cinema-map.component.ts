import { Component, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CinemaService, CinemaVenue } from '../../services/cinema.service';
import * as L from 'leaflet';

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

  constructor(
    private cinemaService: CinemaService,
    private http: HttpClient
  ) { }

  ngOnInit(): void {
    this.loadCinemas();
    this.requestGeolocation();
    this.loadFavoritos();
  }

  ngAfterViewInit(): void { }

  ngOnDestroy(): void {
    if (this.geoErrorTimeoutId != null) clearTimeout(this.geoErrorTimeoutId);
    if (this.map) { this.map.remove(); this.map = null; }
  }


  // ─── FAVORITOS ─────

  private loadFavoritos(): void {
    this.http.get<string[]>(`${this.API}/cinemas-favoritos`, { withCredentials: true })
      .subscribe({
        next: (ids) => { this.favoritosIds = new Set(ids); },
        error: () => { }
      });
  }

  isFavorito(cinema: CinemaVenue): boolean {
    return this.favoritosIds.has(this.cinemaId(cinema));
  }

  toggleFavorito(cinema: CinemaVenue): void {
    const id = this.cinemaId(cinema);
    if (this.togglingId === id) return;
    this.togglingId = id;

    this.http.post<{ cinemaId: string; isFavorito: boolean }>(
      `${this.API}/favoritos/toggle`,
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
  cinemaId(c: CinemaVenue): string {
    return `${c.nome}|${c.latitude}|${c.longitude}`;
  }


  // ─── GEO + CINEMAS ────

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

  private tryFinishAndInitMap(): void {
    if (!this.geoDone || !this.cinemasDone) return;
    this.loading = false;
    setTimeout(() => this.initMap(), 150);
  }

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

  private initMap(): void {
    if (!this.mapContainer?.nativeElement || this.map) return;

    const center = this.userPosition ?? this.PORTUGAL_CENTER;
    const zoom = this.userPosition ? 12 : this.DEFAULT_ZOOM;

    this.map = L.map(this.mapContainer.nativeElement).setView([center.lat, center.lng], zoom);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(this.map);

    if (this.userPosition) {
      L.marker([this.userPosition.lat, this.userPosition.lng])
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

  private addUserMarkerAndFit(): void {
    if (!this.map || !this.userPosition) return;
    L.marker([this.userPosition.lat, this.userPosition.lng])
      .addTo(this.map)
      .bindPopup('<strong>A sua localização</strong>')
      .openPopup();
    this.map.setView([this.userPosition.lat, this.userPosition.lng], 12);
  }

  private escapeHtml(text: string): string {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }

  get sortedCinemas(): CinemaVenue[] {
    return [...this.cinemas].sort((a, b) => (a.distanceKm ?? 999) - (b.distanceKm ?? 999));
  }

  onCardClick(cinema: CinemaVenue): void {
    this.selectedCinemaId = this.cinemaId(cinema);
    this.flyToMarker(cinema);
  }

  private scrollToCardById(id: string): void {
    const el = document.querySelector(`.cinema-card[data-id="${id}"]`) as HTMLElement | null;
    if (el) {
      el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }
}
