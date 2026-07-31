import { TestBed } from '@angular/core/testing';

import { NotificationHub } from './notification-hub';

describe('NotificationHub', () => {
  let service: NotificationHub;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(NotificationHub);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
