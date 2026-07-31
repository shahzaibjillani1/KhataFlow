import { Injectable, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

export type SafepayReturnStatus = 'success' | 'failed' | 'cancelled';

export interface SafepayReturnResult {
  status: SafepayReturnStatus;
  message: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class SafepayWebhookService {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  
  consumeReturnParams(): SafepayReturnResult | null {
    const params = this.route.snapshot.queryParamMap;
    const status = params.get('subscriptionStatus') as SafepayReturnStatus | null;

    if (!status) {
      return null;
    }

    const message = params.get('message');

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {},
      replaceUrl: true,
    });

    return { status, message };
  }
}