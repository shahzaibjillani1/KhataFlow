import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShopOwnerDashboard } from './shop-owner-dashboard';

describe('ShopOwnerDashboard', () => {
  let component: ShopOwnerDashboard;
  let fixture: ComponentFixture<ShopOwnerDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShopOwnerDashboard],
    }).compileComponents();

    fixture = TestBed.createComponent(ShopOwnerDashboard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
