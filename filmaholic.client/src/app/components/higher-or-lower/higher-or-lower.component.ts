import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-higher-or-lower',
  templateUrl: './higher-or-lower.component.html',
  styleUrls: ['./higher-or-lower.component.css']
})
export class HigherOrLowerComponent {
  isPlaying = false;
  showHistory = false;

  constructor(private router: Router) { }

  startGame(): void {
    // Placeholder: switching UI into "playing" mode.
    // Game logic will be implemented next.
    this.isPlaying = true;
    this.showHistory = false;
  }

  openHistory(): void {
    this.showHistory = true;
    this.isPlaying = false;
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}
