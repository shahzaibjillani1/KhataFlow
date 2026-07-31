import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Businesses } from './businesses';

describe('Businesses', () => {
  let component: Businesses;
  let fixture: ComponentFixture<Businesses>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Businesses],
    }).compileComponents();

    fixture = TestBed.createComponent(Businesses);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
