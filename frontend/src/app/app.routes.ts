import { Routes } from '@angular/router';
import { AuthTokenPageComponent } from './pages/auth-token-page/auth-token-page.component';
import { HomePageComponent } from './pages/home-page/home-page.component';
import { PaymentsPageComponent } from './pages/payments-page/payments-page.component';

export const routes: Routes = [
  { path: '', component: HomePageComponent },
  { path: 'payments', component: PaymentsPageComponent },
  { path: 'auth', component: AuthTokenPageComponent },
  { path: '**', redirectTo: '' }
];
