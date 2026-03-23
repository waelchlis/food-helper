import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { RecipeFormComponent } from './recipe-form';
import { RecipeService } from '../../services/recipe';
import { AuthService } from '../../services/auth';

describe('RecipeFormComponent', () => {
  let component: RecipeFormComponent;
  let fixture: ComponentFixture<RecipeFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RecipeFormComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { paramMap: of(convertToParamMap({ id: 'new' })) },
        },
        {
          provide: RecipeService,
          useValue: {
            loadRecipeById: () => of(undefined),
            createRecipe: () => of({ id: '1' }),
            updateRecipe: () => of(undefined),
          },
        },
        {
          provide: AuthService,
          useValue: { isAuthenticated: () => true },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RecipeFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
