import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { PopularActor } from '../../services/atores.service';

@Component({
  selector: 'app-actors-carousel',
  templateUrl: './actors-carousel.component.html',
  styleUrls: ['./actors-carousel.component.css']
})
export class ActorsCarouselComponent implements OnInit, OnDestroy {
  @Input() actors: PopularActor[] = [];
  @Input() visibleCount: number = 5;

  currentIndex = 0;
  isAutoPlaying = true;
  private intervalId: any;
  private readonly intervalTime = 10000; // 10 seconds

  ngOnInit(): void {
    this.startAutoPlay();
  }

  ngOnDestroy(): void {
    this.stopAutoPlay();
  }

  get visibleActors(): PopularActor[] {
    if (!this.actors || this.actors.length === 0) return [];
    
    const result: PopularActor[] = [];
    for (let i = 0; i < this.visibleCount; i++) {
      const index = (this.currentIndex + i) % this.actors.length;
      result.push(this.actors[index]);
    }
    return result;
  }

  getActorRanking(actor: PopularActor): number {
    // Sort all actors by popularity (descending) and find position
    const sortedActors = [...this.actors].sort((a, b) => b.popularidade - a.popularidade);
    return sortedActors.findIndex(a => a.id === actor.id) + 1;
  }

  get totalSlides(): number {
    if (!this.actors || this.actors.length === 0) return 0;
    return this.actors.length;
  }

  get currentSlide(): number {
    return this.currentIndex + 1;
  }

  next(): void {
    this.stopAutoPlay();
    this.goToNext();
    this.startAutoPlay();
  }

  prev(): void {
    this.stopAutoPlay();
    this.goToPrev();
    this.startAutoPlay();
  }

  goToSlide(slideIndex: number): void {
    this.stopAutoPlay();
    this.currentIndex = slideIndex - 1;
    this.startAutoPlay();
  }

  private goToNext(): void {
    if (!this.actors || this.actors.length === 0) return;
    
    this.currentIndex = (this.currentIndex + 1) % this.actors.length;
  }

  private goToPrev(): void {
    if (!this.actors || this.actors.length === 0) return;
    
    this.currentIndex = this.currentIndex === 0 ? this.actors.length - 1 : this.currentIndex - 1;
  }

  private startAutoPlay(): void {
    if (this.isAutoPlaying && this.actors && this.actors.length > this.visibleCount) {
      this.intervalId = setInterval(() => {
        this.goToNext();
      }, this.intervalTime);
    }
  }

  private stopAutoPlay(): void {
    if (this.intervalId) {
      clearInterval(this.intervalId);
      this.intervalId = null;
    }
  }

  toggleAutoPlay(): void {
    this.isAutoPlaying = !this.isAutoPlaying;
    if (this.isAutoPlaying) {
      this.startAutoPlay();
    } else {
      this.stopAutoPlay();
    }
  }

  fotoAtor(actor: PopularActor): string {
    return actor?.fotoUrl || 'https://via.placeholder.com/300x300?text=Actor';
  }
}
