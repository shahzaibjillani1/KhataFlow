import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProductModal } from './product-modal';

describe('ProductModal', () => {
  let component: ProductModal;
  let fixture: ComponentFixture<ProductModal>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductModal],
    }).compileComponents();

    fixture = TestBed.createComponent(ProductModal);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
