import { TestBed } from '@angular/core/testing';

import { SubscriptionPlanService } from './subscription-plan-service';

describe('SubscriptionPlanService', () => {
  let service: SubscriptionPlanService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SubscriptionPlanService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
