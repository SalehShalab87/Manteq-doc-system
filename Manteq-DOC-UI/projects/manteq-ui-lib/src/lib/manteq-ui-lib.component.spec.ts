import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ManteqUiLibComponent } from './manteq-ui-lib.component';

describe('ManteqUiLibComponent', () => {
  let component: ManteqUiLibComponent;
  let fixture: ComponentFixture<ManteqUiLibComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ManteqUiLibComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ManteqUiLibComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
