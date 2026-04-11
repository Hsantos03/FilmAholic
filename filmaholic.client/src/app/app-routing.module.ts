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
import { LeaderboardComponent } from './components/leaderboard/leaderboard.component';
import { ComunidadesComponent } from './components/comunidades/comunidades.component';
import { ComunidadeDetalheComponent } from './components/comunidade-detalhe/comunidade-detalhe.component';
import { NotificacoesConfigComponent } from './components/notificacoes-config/notificacoes-config.component';
import { AdminPanelComponent } from './components/admin-panel/admin-panel.component';
import { HomePageComponent } from './components/homepage/homepage.component';
import { AdminGuard } from './guards/admin.guard';

/// <summary>
/// Configura as rotas da aplicação, associando caminhos a componentes específicos e aplicando guardas de autenticação quando necessário.
/// </summary>
const routes: Routes = [
  { path: 'register', component: RegisterComponent },
  { path: 'login', component: LoginComponent },
  { path: 'email-confirmado', component: EmailConfirmadoComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent }, 
  { path: 'reset-password', component: ResetPasswordComponent },
  { path: 'selecionar-generos', component: SelecionarGenerosComponent },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'profile/:userId', component: ProfileComponent },
  { path: 'profile', component: ProfileComponent },
  { path: 'search', component: SearchResultsComponent },
  { path: 'movie-detail/:id', component: MoviePageComponent },
  { path: 'actor/:id', component: ActorDetailComponent },
  { path: 'higher-or-lower', component: HigherOrLowerComponent },
  { path: 'cinema-movies', component: CinemaMoviesComponent },
  { path: 'cinemas-proximos', component: CinemaMapComponent },
  { path: 'leaderboard', component: LeaderboardComponent },
  { path: 'comunidades', component: ComunidadesComponent },
  { path: 'comunidades/:id', component: ComunidadeDetalheComponent },
  { path: 'definicoes-notificacoes', component: NotificacoesConfigComponent },
  { path: 'admin', component: AdminPanelComponent, canActivate: [AdminGuard] },
  { path: 'mapa-cinemas', redirectTo: 'cinemas-proximos', pathMatch: 'full' },
  { path: 'home', component: HomePageComponent },
  { path: '', redirectTo: '/home', pathMatch: 'full' }
];

/// <summary>
/// Configura o módulo de roteamento da aplicação, importando as rotas definidas e exportando o RouterModule para uso em outros módulos.
/// </summary>
@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
