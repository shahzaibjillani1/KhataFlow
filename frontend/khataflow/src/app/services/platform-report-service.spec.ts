import { TestBed } from '@angular/core/testing';

import { PlatformReportService } from './platform-report-service';

describe('PlatformReportService', () => {
  let service: PlatformReportService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(PlatformReportService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
