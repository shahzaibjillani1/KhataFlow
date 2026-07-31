import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PublicLedgerView } from './public-ledger-view';

describe('PublicLedgerView', () => {
  let component: PublicLedgerView;
  let fixture: ComponentFixture<PublicLedgerView>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PublicLedgerView],
    }).compileComponents();

    fixture = TestBed.createComponent(PublicLedgerView);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
