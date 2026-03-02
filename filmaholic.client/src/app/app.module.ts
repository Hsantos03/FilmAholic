import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { LOCALE_ID, NgModule } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
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
import { FormatDurationPipe } from './pipes/format-duration.pipe';


registerLocaleData(localePt);

@NgModule({
  declarations: [
    AppComponent,
    FormatDurationPipe,
    RegisterComponent,
    LoginComponent,
    EmailConfirmadoComponent,
    ForgotPasswordComponent,
    ResetPasswordComponent,
    DashboardComponent,
    ProfileComponent,
    SelecionarGenerosComponent,
    SearchResultsComponent,
    MoviePageComponent
  ],
  imports: [
    BrowserModule,
    CommonModule,
    HttpClientModule,
    FormsModule,
    AppRoutingModule
  ],
  providers: [{provide: LOCALE_ID, useValue: 'pt-PT'}],
  bootstrap: [AppComponent]
})
export class AppModule { }
