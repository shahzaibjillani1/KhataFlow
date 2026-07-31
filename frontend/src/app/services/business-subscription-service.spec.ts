import { TestBed } from '@angular/core/testing';

import { BusinessSubscriptionService } from './business-subscription-service';

describe('BusinessSubscriptionService', () => {
  let service: BusinessSubscriptionService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BusinessSubscriptionService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
