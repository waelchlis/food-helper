import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDividerModule } from '@angular/material/divider';
import { Recipe, RecipeService, Ingredient } from '../../services/recipe';

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
    private recipeService: RecipeService
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
}


