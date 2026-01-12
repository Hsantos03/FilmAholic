import { Component, Input, OnInit } from '@angular/core';
import { UserMoviesService } from 'src/app/services/user-movies.service';

@Component({
  selector: 'app-movie-detail',
  templateUrl: './movie-detail.component.html'
})
export class MovieDetailComponent implements OnInit {
  @Input() filmeId!: number;
  totalHours: number = 0;

  constructor(private userMoviesService: UserMoviesService) { }

  ngOnInit() {
    this.loadTotalHours();
  }

  loadTotalHours() {
    this.userMoviesService.getTotalHours().subscribe({
      next: (hours) => this.totalHours = hours,
      error: (err) => console.error(err)
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
