import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { PaymentsService, PaymentIntent } from '../../services/payments.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-payments-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './payments-page.component.html',
  styleUrl: './payments-page.component.scss'
})
export class PaymentsPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly paymentsService = inject(PaymentsService);
  private readonly authService = inject(AuthService);

  readonly intents: PaymentIntent[] = [];
  errorMessage = '';
  isSubmitting = false;

  readonly form = this.formBuilder.group({
    merchantId: ['', [Validators.required]],
    customerId: ['', [Validators.required]],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
    currency: ['USD', [Validators.required, Validators.minLength(3), Validators.maxLength(3)]],
    reference: ['']
  });

  get isOps(): boolean {
    return this.authService.isInRole('ops');
  }

  get isLoggedIn(): boolean {
    return Boolean(this.authService.getToken());
  }

  submit(): void {
    this.errorMessage = '';
    if (this.form.invalid || this.isSubmitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const payload = {
      merchantId: this.form.value.merchantId!.trim(),
      customerId: this.form.value.customerId!.trim(),
      amount: Number(this.form.value.amount),
      currency: this.form.value.currency!.trim().toUpperCase(),
      reference: this.form.value.reference?.trim() || null
    };

    this.paymentsService.createIntent(payload).subscribe({
      next: (intent) => {
        this.intents.unshift(intent);
        this.form.reset({ currency: payload.currency });
        this.paymentsService.resetIdempotencyKey();
        this.isSubmitting = false;
      },
      error: () => {
        this.errorMessage = 'Failed to create intent. Please retry.';
        this.isSubmitting = false;
      }
    });
  }

  capture(intent: PaymentIntent): void {
    this.errorMessage = '';
    this.paymentsService.captureIntent(intent.id).subscribe({
      next: (updated) => {
        const index = this.intents.findIndex((item) => item.id === updated.id);
        if (index >= 0) {
          this.intents[index] = updated;
        }
      },
      error: () => {
        this.errorMessage = 'Capture failed. Please retry.';
      }
    });
  }

  canCapture(intent: PaymentIntent): boolean {
    return intent.status?.toLowerCase() !== 'captured';
  }
}
