import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  userName: string = '';

  ngOnInit() {
    // Obter o nome do utilizador do localStorage
    this.userName = localStorage.getItem('user_nome') || 'Utilizador';
  }
}
