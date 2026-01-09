import { Routes } from '@angular/router';
import { AuthTokenPageComponent } from './pages/auth-token-page/auth-token-page.component';
import { HomePageComponent } from './pages/home-page/home-page.component';
import { PaymentsPageComponent } from './pages/payments-page/payments-page.component';
import { LoginPageComponent } from './pages/login-page/login-page.component';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', component: HomePageComponent },
  { path: 'login', component: LoginPageComponent },
  { path: 'payments', component: PaymentsPageComponent, canActivate: [authGuard] },
  { path: 'auth', component: AuthTokenPageComponent },
  { path: '**', redirectTo: '' }
];
