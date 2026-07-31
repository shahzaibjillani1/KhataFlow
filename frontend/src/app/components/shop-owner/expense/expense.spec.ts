import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Expense } from './expense';

describe('Expense', () => {
  let component: Expense;
  let fixture: ComponentFixture<Expense>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Expense],
    }).compileComponents();

    fixture = TestBed.createComponent(Expense);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
