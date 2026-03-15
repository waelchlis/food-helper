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

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private recipeService: RecipeService,
    private shoppingListService: ShoppingListService,
    private snackBar: MatSnackBar,
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        const recipe = this.recipeService.getRecipeById(id);
        this.recipe.set(recipe);
        if (recipe) {
          this.desiredServings.set(recipe.servings);
          this.updateScaledIngredients();
        }
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


