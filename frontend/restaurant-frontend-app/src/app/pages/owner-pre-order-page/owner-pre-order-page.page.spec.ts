import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OwnerPreOrderPagePage } from './owner-pre-order-page.page';

describe('OwnerPreOrderPagePage', () => {
  let component: OwnerPreOrderPagePage;
  let fixture: ComponentFixture<OwnerPreOrderPagePage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OwnerPreOrderPagePage],
    }).compileComponents();

    fixture = TestBed.createComponent(OwnerPreOrderPagePage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
