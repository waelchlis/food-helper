import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SamplePage } from './sample-page';

describe('SamplePage', () => {
  let component: SamplePage;
  let fixture: ComponentFixture<SamplePage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SamplePage],
    }).compileComponents();

    fixture = TestBed.createComponent(SamplePage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
