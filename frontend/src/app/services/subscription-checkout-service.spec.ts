import { TestBed } from '@angular/core/testing';

import { SubscriptionCheckoutService } from './subscription-checkout-service';

describe('SubscriptionCheckoutService', () => {
  let service: SubscriptionCheckoutService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SubscriptionCheckoutService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
