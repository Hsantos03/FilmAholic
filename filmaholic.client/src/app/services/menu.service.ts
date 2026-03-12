import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class MenuService {

  private isOpen = new BehaviorSubject<boolean>(false);

  isOpen$ = this.isOpen.asObservable();

  toggle(): void {
    this.isOpen.next(!this.isOpen.value);
  }

  close(): void {
    this.isOpen.next(false);
  }

  open(): void {
    this.isOpen.next(true);
  }

}
