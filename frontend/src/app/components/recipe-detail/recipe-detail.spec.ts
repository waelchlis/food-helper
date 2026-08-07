import { ComponentFixture, TestBed } from '@angular/core/testing';
import { convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';

import { RecipeDetailComponent } from './recipe-detail';
import { RecipeService } from '../../services/recipe';
import { ShoppingListService } from '../../services/shopping-list';
import { AuthService } from '../../services/auth';
import { FavoriteService } from '../../services/favorite';

describe('RecipeDetailComponent', () => {
  let component: RecipeDetailComponent;
  let fixture: ComponentFixture<RecipeDetailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RecipeDetailComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { paramMap: of(convertToParamMap({ id: '1' })) },
        },
        {
          provide: RecipeService,
          useValue: {
            loadRecipeById: () => of(undefined),
            scaleIngredients: () => [],
            getSimilar: () => of([]),
          },
        },
        {
          provide: ShoppingListService,
          useValue: { addIngredientsFromRecipe: () => {} },
        },
        {
          provide: MatSnackBar,
          useValue: { open: () => ({ onAction: () => of() }) },
        },
        {
          provide: AuthService,
          useValue: { isAuthenticated: () => false },
        },
        {
          provide: FavoriteService,
          useValue: { isFavorite: () => false, toggle: () => {} },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RecipeDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
