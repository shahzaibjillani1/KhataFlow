import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InvoiceSettings } from './invoice-settings';

describe('InvoiceSettings', () => {
  let component: InvoiceSettings;
  let fixture: ComponentFixture<InvoiceSettings>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InvoiceSettings],
    }).compileComponents();

    fixture = TestBed.createComponent(InvoiceSettings);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
