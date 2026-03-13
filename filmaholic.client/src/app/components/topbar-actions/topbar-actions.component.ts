import { Component, OnInit, OnDestroy, ViewChild, ElementRef, HostListener } from '@angular/core';
import { Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { FilmesService, Filme } from '../../services/filmes.service';

@Component({
  selector: 'app-topbar-actions',
  templateUrl: './topbar-actions.component.html',
  styleUrls: ['./topbar-actions.component.css']
})
export class TopbarActionsComponent implements OnInit, OnDestroy {
  @ViewChild('notificationsContainer', { static: false }) notificationsContainerRef?: ElementRef<HTMLElement>;

  isNotificationsOpen = false;
  movies: Filme[] = [];
  private isLoadingUpcomingDetails = false;
  private dateOnlyMs = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();

  constructor(
    private filmesService: FilmesService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.filmesService.getAll().subscribe({
      next: (list) => { this.movies = list || []; },
      error: () => { this.movies = []; }
    });
  }

  ngOnDestroy(): void {}

  @HostListener('document:click', ['$event'])
  onDocumentClick(e: MouseEvent): void {
    const el = this.notificationsContainerRef?.nativeElement;
    if (el && !el.contains(e.target as Node)) this.isNotificationsOpen = false;
  }

  toggleNotifications(e: MouseEvent): void {
    e.stopPropagation();
    this.isNotificationsOpen = !this.isNotificationsOpen;
    if (this.isNotificationsOpen) this.loadUpcomingDetails();
  }

  get upcomingMovies(): Filme[] {
    const today = new Date();
    const todayMs = this.dateOnlyMs(today);
    const currentYear = today.getFullYear();
    const upcoming = (this.movies || []).filter(m => {
      if (m.releaseDate) {
        const parsed = new Date(m.releaseDate);
        if (isNaN(parsed.getTime())) return false;
        return this.dateOnlyMs(parsed) > todayMs;
      }
      if (m.ano != null) {
        const anoNum = Number(m.ano);
        if (!isNaN(anoNum)) return anoNum > currentYear;
      }
      return false;
    });
    upcoming.sort((a, b) => {
      const dateOf = (m: Filme) => {
        if (m.releaseDate) {
          const d = new Date(m.releaseDate);
          if (!isNaN(d.getTime())) return this.dateOnlyMs(d);
        }
        if (m.ano != null) {
          const anoNum = Number(m.ano);
          if (!isNaN(anoNum)) return new Date(anoNum, 0, 1).getTime();
        }
        return Number.MAX_SAFE_INTEGER;
      };
      return dateOf(a) - dateOf(b);
    });
    return upcoming.slice(0, 5);
  }

  private loadUpcomingDetails(): void {
    if (this.isLoadingUpcomingDetails) return;
    const upcoming = this.upcomingMovies;
    const missing = upcoming.filter(m => !m.releaseDate && m.tmdbId);
    if (!missing.length) return;
    this.isLoadingUpcomingDetails = true;
    const requests = missing.map(m => {
      const idNum = Number(m.tmdbId);
      if (!idNum || isNaN(idNum)) return of(null);
      return this.filmesService.getMovieFromTmdb(idNum).pipe(catchError(() => of(null)));
    });
    forkJoin(requests).subscribe({
      next: (results: (Filme | null)[]) => {
        results.forEach((res, idx) => {
          if (!res || !missing[idx]) return;
          const anyRes = res as any;
          let remoteDate: string | undefined = anyRes.releaseDate ?? anyRes.release_date ?? anyRes.ReleaseDate;
          if (!remoteDate && anyRes.release_dates && Array.isArray(anyRes.release_dates)) {
            try {
              for (const rdGroup of anyRes.release_dates) {
                if (rdGroup?.release_dates?.length) {
                  const found = rdGroup.release_dates.find((x: any) => x.iso_3166_1 === 'PT' || x.iso_3166_1 === 'US') ?? rdGroup.release_dates[0];
                  remoteDate = found?.release_date ?? found?.date;
                  if (remoteDate) break;
                }
              }
            } catch {}
          }
          if (remoteDate) {
            const parsed = new Date(remoteDate);
            if (!isNaN(parsed.getTime())) {
              const local = this.movies.find(x => x.id === missing[idx].id);
              if (local) local.releaseDate = parsed.toISOString();
            }
          } else {
            const anyResYear = anyRes.ano ?? anyRes.year ?? anyRes.Ano;
            if (anyResYear != null) {
              const local = this.movies.find(x => x.id === missing[idx].id);
              if (local && !local.ano) {
                const y = Number(anyResYear);
                if (!isNaN(y)) local.ano = y;
              }
            }
          }
        });
      },
      complete: () => { this.isLoadingUpcomingDetails = false; },
      error: () => { this.isLoadingUpcomingDetails = false; }
    });
  }

  releaseLabel(f: Filme): string {
    if (!f) return '';
    if (f.releaseDate) {
      const d = new Date(f.releaseDate);
      if (!isNaN(d.getTime())) return d.toLocaleDateString('pt-PT', { day: '2-digit', month: 'long', year: 'numeric' });
    }
    if (f.ano != null) return `${f.ano} (TBA)`;
    return 'TBA';
  }

  posterOf(f: Filme): string {
    return f?.posterUrl || 'https://via.placeholder.com/300x450?text=Poster';
  }

  openNotificationMovie(m: Filme): void {
    this.isNotificationsOpen = false;
    if (m?.id) this.router.navigate(['/movie-detail', m.id]);
  }
}
