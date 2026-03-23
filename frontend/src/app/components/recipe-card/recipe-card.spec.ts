import { ComponentFixture, TestBed } from '@angular/core/testing';

import { provideRouter } from '@angular/router';
import { RecipeCardComponent } from './recipe-card';

describe('RecipeCardComponent', () => {
  let component: RecipeCardComponent;
  let fixture: ComponentFixture<RecipeCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RecipeCardComponent],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(RecipeCardComponent);
    component = fixture.componentInstance;
    component.recipe = {
      id: '1',
      name: 'Recipe',
      description: 'Desc',
      servings: 2,
      prepTime: 10,
      cookTime: 10,
      ingredients: [],
      instructions: [],
      createdAt: new Date(),
      updatedAt: new Date(),
    };
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
