import { TestBed } from '@angular/core/testing';

import { CustomerLedgerViewService } from './customer-ledger-view-service';

describe('CustomerLedgerViewService', () => {
  let service: CustomerLedgerViewService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CustomerLedgerViewService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
