import { TestBed } from '@angular/core/testing';

import { InvoiceSettings } from './invoice-settings';

describe('InvoiceSettings', () => {
  let service: InvoiceSettings;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(InvoiceSettings);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
