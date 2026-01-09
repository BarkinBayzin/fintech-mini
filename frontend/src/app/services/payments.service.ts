import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CreatePaymentIntentDto {
  merchantId: string;
  amount: number;
  currency: string;
  customerId: string;
  reference?: string | null;
}

export interface PaymentIntent {
  id: string;
  merchantId: string;
  amount: number;
  currency: string;
  customerId: string;
  reference?: string | null;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentsService {
  private lastIdempotencyKey: string | null = null;

  constructor(private readonly http: HttpClient) {}

  createIntent(dto: CreatePaymentIntentDto): Observable<PaymentIntent> {
    const idempotencyKey = this.getOrCreateIdempotencyKey();
    const headers = new HttpHeaders({
      'Idempotency-Key': idempotencyKey,
      'X-Correlation-Id': crypto.randomUUID()
    });

    return this.http.post<PaymentIntent>('/api/payments/intents', dto, { headers });
  }

  captureIntent(id: string): Observable<PaymentIntent> {
    const headers = new HttpHeaders({
      'X-Correlation-Id': crypto.randomUUID()
    });

    return this.http.post<PaymentIntent>(`/api/payments/intents/${id}/capture`, {}, { headers });
  }

  resetIdempotencyKey(): void {
    this.lastIdempotencyKey = null;
  }

  private getOrCreateIdempotencyKey(): string {
    if (!this.lastIdempotencyKey) {
      this.lastIdempotencyKey = crypto.randomUUID();
    }

    return this.lastIdempotencyKey;
  }
}
