import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { provideRouter } from '@angular/router';

import { RecipeListComponent } from './recipe-list';
import { RecipeService } from '../../services/recipe';
import { AuthService } from '../../services/auth';
import { FavoriteService } from '../../services/favorite';

describe('RecipeListComponent', () => {
  let component: RecipeListComponent;
  let fixture: ComponentFixture<RecipeListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RecipeListComponent],
      providers: [
        provideRouter([]),
        {
          provide: RecipeService,
          useValue: {
            getRecipes: () => [],
            refreshRecipes: () => of([]),
            queryRecipes: () => of({ items: [], nextCursor: null }),
            allIngredients: () => [],
            deleteRecipe: () => of(true),
          },
        },
        {
          provide: AuthService,
          useValue: { isAuthenticated: () => false, isAdmin: () => false },
        },
        {
          provide: FavoriteService,
          useValue: { isFavorite: () => false, favoriteIds: () => new Set(), toggle: () => {} },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RecipeListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
