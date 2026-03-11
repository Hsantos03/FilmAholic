import { Component, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CinemaService, CinemaVenue } from '../../services/cinema.service';
import * as L from 'leaflet';

// Fix Leaflet default icon 404 in Angular (marker icons must be served from assets)
const iconUrl = 'leaflet/marker-icon.png';
const iconRetinaUrl = 'leaflet/marker-icon-2x.png';
const shadowUrl = 'leaflet/marker-shadow.png';
const iconDefault = L.icon({
  iconUrl,
  iconRetinaUrl,
  shadowUrl,
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  shadowSize: [41, 41]
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
  userPosition: { lat: number; lng: number } | null = null;
  loading = true;
  geoError: string | null = null;
  cinemasError = false;
  private geoDone = false;
  private cinemasDone = false;
  private geoErrorTimeoutId: ReturnType<typeof setTimeout> | null = null;

  private readonly PORTUGAL_CENTER = { lat: 38.7223, lng: -9.1393 };
  private readonly DEFAULT_ZOOM = 10;
  /** Atrasar a mensagem de erro para não mostrar se a localização chegar logo a seguir (ex.: 2 s). */
  private readonly GEO_ERROR_DELAY_MS = 3500;

  constructor(private cinemaService: CinemaService) {}

  ngOnInit(): void {
    this.loadCinemas();
    this.requestGeolocation();
  }

  ngAfterViewInit(): void {}

  ngOnDestroy(): void {
    if (this.geoErrorTimeoutId != null) clearTimeout(this.geoErrorTimeoutId);
    if (this.map) {
      this.map.remove();
      this.map = null;
    }
  }

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
        this.userPosition = {
          lat: position.coords.latitude,
          lng: position.coords.longitude
        };
        this.calculateDistances();
        this.geoDone = true;
        this.tryFinishAndInitMap();
        // Se o mapa já foi inicializado (ex.: após timeout), adicionar marcador da posição agora
        if (this.map) this.addUserMarkerAndFit();
      },
      (err) => {
        let message: string;
        switch (err.code) {
          case err.PERMISSION_DENIED:
            message = 'Permissão de localização negada. Ative a localização para ver a distância aos cinemas.';
            break;
          case err.POSITION_UNAVAILABLE:
            message = 'Localização indisponível.';
            break;
          case err.TIMEOUT:
            message = 'Tempo esgotado ao obter a localização. Tente atualizar a página após ativar a localização.';
            break;
          default:
            message = 'Não foi possível obter a sua localização.';
        }
        this.geoDone = true;
        this.tryFinishAndInitMap();
        // Só mostrar o erro após um pequeno atraso; se entretanto chegar sucesso, não mostramos
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
        this.userPosition!.lat,
        this.userPosition!.lng,
        c.latitude,
        c.longitude
      );
    });
    this.cinemas.sort((a, b) => (a.distanceKm ?? 999) - (b.distanceKm ?? 999));
  }

  private haversineKm(lat1: number, lon1: number, lat2: number, lon2: number): number {
    const R = 6371;
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;
    const a =
      Math.sin(dLat / 2) * Math.sin(dLat / 2) +
      Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
      Math.sin(dLon / 2) * Math.sin(dLon / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return Math.round(R * c * 10) / 10;
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
      L.marker([c.latitude, c.longitude])
        .addTo(this.map)
        .bindPopup(popup);
    });
  }

  /** Chamado quando a geolocalização tem sucesso depois do mapa já estar inicializado (ex.: utilizador autorizou com atraso). */
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
}
