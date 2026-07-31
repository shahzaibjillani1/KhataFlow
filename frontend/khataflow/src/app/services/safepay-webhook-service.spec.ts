import { TestBed } from '@angular/core/testing';

import { SafepayWebhookService } from './safepay-webhook-service';

describe('SafepayWebhookService', () => {
  let service: SafepayWebhookService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SafepayWebhookService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
