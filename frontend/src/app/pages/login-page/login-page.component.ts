import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.scss'
})
export class LoginPageComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  errorMessage = '';
  isSubmitting = false;

  readonly form = this.formBuilder.group({
    username: ['', [Validators.required]],
    password: ['', [Validators.required]]
  });

  submit(): void {
    this.errorMessage = '';
    if (this.form.invalid || this.isSubmitting) {
      this.form.markAllAsTouched();
      return;
    }

    const username = this.form.value.username?.trim() ?? '';
    const password = this.form.value.password ?? '';
    if (!username || !password) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.authService.login(username, password).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.router.navigateByUrl('/payments');
      },
      error: (err) => {
        this.isSubmitting = false;
        const apiError = err?.error;
        this.errorMessage =
          apiError?.error_description ??
          apiError?.error ??
          'Login failed. Check credentials.';
      }
    });
  }
}
