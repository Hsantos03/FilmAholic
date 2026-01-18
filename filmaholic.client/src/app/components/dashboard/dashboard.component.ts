import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  userName: string = '';
  isDesafiosOpen: boolean = false;

  ngOnInit(): void {
    // Obter o nome do utilizador do localStorage
    this.userName = localStorage.getItem('user_nome') || 'Utilizador';
  }

  public openDesafios(): void {
    this.isDesafiosOpen = true;
  }

  public closeDesafios(): void {
    this.isDesafiosOpen = false;
  }
}
