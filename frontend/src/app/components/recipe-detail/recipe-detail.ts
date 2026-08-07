import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Recipe, RecipeService, Ingredient } from '../../services/recipe';
import { ShoppingListService } from '../../services/shopping-list';
import { AuthService } from '../../services/auth';
import { FavoriteService } from '../../services/favorite';
import { take } from 'rxjs';

@Component({
  selector: 'app-recipe-detail',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatDividerModule,
    MatSnackBarModule,
  ],
  templateUrl: './recipe-detail.html',
  styleUrl: './recipe-detail.scss',
})
export class RecipeDetailComponent implements OnInit {
  recipe = signal<Recipe | undefined>(undefined);
  desiredServings = signal<number>(4);
  scaledIngredients = signal<Ingredient[]>([]);
  activeImageIndex = signal<number>(0);
  similarRecipes = signal<Recipe[]>([]);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private recipeService: RecipeService,
    private shoppingListService: ShoppingListService,
    private snackBar: MatSnackBar,
    public authService: AuthService,
    public favoriteService: FavoriteService,
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.activeImageIndex.set(0);
        this.recipeService.loadRecipeById(id).subscribe(recipe => {
          this.recipe.set(recipe);
          if (recipe) {
            this.desiredServings.set(recipe.servings);
            this.updateScaledIngredients();
          }
        });
        this.recipeService.getSimilar(id).subscribe(recipes => this.similarRecipes.set(recipes));
      }
    });
  }

  updateScaledIngredients(): void {
    const recipe = this.recipe();
    if (recipe) {
      const scaled = this.recipeService.scaleIngredients(recipe, this.desiredServings());
      this.scaledIngredients.set(scaled);
    }
  }

  onServingsChange(): void {
    this.updateScaledIngredients();
  }

  decreaseServings(): void {
    this.desiredServings.set(Math.max(1, this.desiredServings() - 1));
    this.updateScaledIngredients();
  }

  increaseServings(): void {
    this.desiredServings.set(this.desiredServings() + 1);
    this.updateScaledIngredients();
  }

  resetServings(): void {
    const recipe = this.recipe();
    if (recipe) {
      this.desiredServings.set(recipe.servings);
      this.updateScaledIngredients();
    }
  }

  prevImage(): void {
    const images = this.recipe()?.images ?? [];
    if (images.length === 0) return;
    this.activeImageIndex.set((this.activeImageIndex() - 1 + images.length) % images.length);
  }

  nextImage(): void {
    const images = this.recipe()?.images ?? [];
    if (images.length === 0) return;
    this.activeImageIndex.set((this.activeImageIndex() + 1) % images.length);
  }

  toggleFavorite(): void {
    const recipe = this.recipe();
    if (recipe) {
      this.favoriteService.toggle(recipe);
    }
  }

  async shareRecipe(): Promise<void> {
    try {
      await navigator.clipboard.writeText(window.location.href);
      this.snackBar.open('Link copied to clipboard.', undefined, { duration: 2500, panelClass: 'snack-success' });
    } catch {
      this.snackBar.open('Could not copy link.', 'Dismiss', { duration: 4000 });
    }
  }

  addToShoppingList(): void {
    const ingredients = this.scaledIngredients();
    if (ingredients.length > 0) {
      this.shoppingListService.addIngredientsFromRecipe(ingredients);
      const ref = this.snackBar.open(
        `${ingredients.length} ingredient${ingredients.length === 1 ? '' : 's'} added to shopping list`,
        'View',
        { duration: 3000, panelClass: 'snack-success' }
      );
      ref.onAction()
      .pipe(take(1))
      .subscribe(() => this.router.navigate(['/shopping-list']));
    }
  }
}
