import { Component, Input, OnInit } from '@angular/core';
import { UserMoviesService } from '../../services/user-movies.service';
import { FilmesService, RatingsDto } from '../../services/filmes.service';

@Component({
  selector: 'app-movie-detail',
  templateUrl: './movie-detail.component.html',
  styleUrls: ['./movie-detail.component.css']
})
export class MovieDetailComponent implements OnInit {
  @Input() filmeId!: number;
  totalHours: number = 0;

  ratings: RatingsDto | null = null;
  isLoadingRatings = false;

  constructor(
    private userMoviesService: UserMoviesService,
    private filmesService: FilmesService
  ) { }

  ngOnInit() {
    this.loadTotalHours();
    this.loadRatings();
  }

  loadTotalHours() {
    this.userMoviesService.getTotalHours().subscribe({
      next: (hours: number) => this.totalHours = hours,
      error: (err: any) => console.error(err)
    });
  }

  loadRatings() {
    if (!this.filmeId) return;

    this.isLoadingRatings = true;

    this.filmesService.getRatings(this.filmeId).subscribe({
      next: (res: RatingsDto) => (this.ratings = res ?? null),
      error: (err: any) => {
        console.warn('Failed to load ratings', err);
        this.ratings = null;
      },
      complete: () => (this.isLoadingRatings = false)
    });
  }

  addQueroVer() {
    this.userMoviesService.addMovie(this.filmeId, false).subscribe({
      next: () => this.loadTotalHours(),
      error: (err: any) => console.warn('addMovie failed', err)
    });
  }

  addJaVi() {
    this.userMoviesService.addMovie(this.filmeId, true).subscribe({
      next: () => this.loadTotalHours(),
      error: (err: any) => console.warn('addMovie failed', err)
    });
  }

  remove() {
    this.userMoviesService.removeMovie(this.filmeId).subscribe({
      next: () => this.loadTotalHours(),
      error: (err: any) => console.warn('removeMovie failed', err)
    });
  }
}
