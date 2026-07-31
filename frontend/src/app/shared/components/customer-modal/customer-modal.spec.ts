import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomerModal } from './customer-modal';

describe('CustomerModal', () => {
  let component: CustomerModal;
  let fixture: ComponentFixture<CustomerModal>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomerModal],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomerModal);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
