import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-auth-token-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './auth-token-page.component.html',
  styleUrl: './auth-token-page.component.scss'
})
export class AuthTokenPageComponent {
  token = localStorage.getItem('access_token') ?? '';

  get hasToken(): boolean {
    return Boolean(localStorage.getItem('access_token'));
  }

  save(): void {
    if (this.token.trim().length === 0) {
      this.clear();
      return;
    }

    localStorage.setItem('access_token', this.token.trim());
  }

  clear(): void {
    localStorage.removeItem('access_token');
    this.token = '';
  }
}
