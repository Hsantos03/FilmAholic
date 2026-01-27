import { Component, Input, OnInit } from '@angular/core';
import { UserMoviesService } from 'src/app/services/user-movies.service';
import { FilmesService, RatingsDto } from 'src/app/services/filmes.service';

@Component({
  selector: 'app-movie-detail',
  templateUrl: './movie-detail.component.html'
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
      next: (hours) => this.totalHours = hours,
      error: (err) => console.error(err)
    });
  }

  loadRatings() {
    if (!this.filmeId) return;

    this.isLoadingRatings = true;

    this.filmesService.getRatings(this.filmeId).subscribe({
      next: (res) => (this.ratings = res ?? null),
      error: (err) => {
        console.warn('Failed to load ratings', err);
        this.ratings = null;
      },
      complete: () => (this.isLoadingRatings = false)
    });
  }

  addQueroVer() {
    this.userMoviesService.addMovie(this.filmeId, false).subscribe(() => this.loadTotalHours());
  }

  addJaVi() {
    this.userMoviesService.addMovie(this.filmeId, true).subscribe(() => this.loadTotalHours());
  }

  remove() {
    this.userMoviesService.removeMovie(this.filmeId).subscribe(() => this.loadTotalHours());
  }
}
