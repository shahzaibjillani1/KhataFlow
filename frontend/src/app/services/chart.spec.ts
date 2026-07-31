import { TestBed } from '@angular/core/testing';

import { Chart } from './chart';

describe('Chart', () => {
  let service: Chart;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Chart);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
