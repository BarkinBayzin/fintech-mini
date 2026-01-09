import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { map } from 'rxjs';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-auth-token-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './auth-token-page.component.html',
  styleUrl: './auth-token-page.component.scss'
})
export class AuthTokenPageComponent {
  private readonly authService = inject(AuthService);

  readonly token$ = this.authService.token$;
  readonly isOps$ = this.authService.isOps$;
  readonly hasToken$ = this.token$.pipe(map((token) => Boolean(token)));

  token = this.authService.getToken() ?? '';

  save(): void {
    this.authService.setToken(this.token);
  }

  clear(): void {
    this.authService.clearToken();
    this.token = '';
  }
}
