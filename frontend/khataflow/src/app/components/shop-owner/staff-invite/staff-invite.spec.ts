import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StaffInvite } from './staff-invite';

describe('StaffInvite', () => {
  let component: StaffInvite;
  let fixture: ComponentFixture<StaffInvite>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StaffInvite],
    }).compileComponents();

    fixture = TestBed.createComponent(StaffInvite);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
