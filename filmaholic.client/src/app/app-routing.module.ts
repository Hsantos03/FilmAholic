import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { RegisterComponent } from './components/register/register.component';
import { LoginComponent } from './components/login/login.component';
import { EmailConfirmadoComponent } from './components/email-confirmado/email-confirmado.component';
import { ForgotPasswordComponent } from './components/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './components/reset-password/reset-password.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { ProfileComponent } from './components/profile/profile.component';
import { SelecionarGenerosComponent } from './components/selecionar-generos/selecionar-generos.component';
import { SearchResultsComponent } from './components/search-results/search-results.component';
import { MoviePageComponent } from './components/movie-page/movie-page.component';
import { HigherOrLowerComponent } from './components/higher-or-lower/higher-or-lower.component';
import { CinemaMoviesComponent } from './components/cinema-movies/cinema-movies.component';
import { CinemaMapComponent } from './components/cinema-map/cinema-map.component';
import { ActorDetailComponent } from './components/actor-detail/actor-detail.component';

const routes: Routes = [
  { path: 'register', component: RegisterComponent },
  { path: 'login', component: LoginComponent },
  { path: 'email-confirmado', component: EmailConfirmadoComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent }, 
  { path: 'reset-password', component: ResetPasswordComponent },
  { path: 'selecionar-generos', component: SelecionarGenerosComponent },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'profile', component: ProfileComponent },
  { path: 'search', component: SearchResultsComponent },
  { path: 'movie-detail/:id', component: MoviePageComponent }, // new route
  { path: 'actor/:id', component: ActorDetailComponent }, // actor details page
  { path: 'higher-or-lower', component: HigherOrLowerComponent }, // game page
  { path: 'cinema-movies', component: CinemaMoviesComponent }, // cinema movies page
  { path: 'cinemas-proximos', component: CinemaMapComponent }, // mapa de cinemas próximos (FR40)
  // Alias para compatibilidade com links antigos
  { path: 'mapa-cinemas', redirectTo: 'cinemas-proximos', pathMatch: 'full' },
  { path: '', redirectTo: '/register', pathMatch: 'full' } // Rota inicial
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
