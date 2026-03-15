import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RecipeForm } from './recipe-form';

describe('RecipeForm', () => {
  let component: RecipeForm;
  let fixture: ComponentFixture<RecipeForm>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RecipeForm],
    }).compileComponents();

    fixture = TestBed.createComponent(RecipeForm);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
