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
import { Recipe, RecipeService, Ingredient } from '../../services/recipe';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-recipe-form',
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
  templateUrl: './recipe-form.html',
  styleUrl: './recipe-form.scss',
})
export class RecipeFormComponent implements OnInit {
  isEditMode = signal<boolean>(false);
  recipe = signal<Partial<Recipe>>({
    name: '',
    description: '',
    servings: 4,
    prepTime: 15,
    cookTime: 30,
    ingredients: [],
    instructions: [],
  });

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private recipeService: RecipeService,
    private authService: AuthService,
  ) {}

  ngOnInit(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/recipes']);
      return;
    }

    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id && id !== 'new') {
        this.isEditMode.set(true);
        this.recipeService.loadRecipeById(id).subscribe(existingRecipe => {
          if (existingRecipe) {
            this.recipe.set({ ...existingRecipe });
          }
        });
      } else {
        // Initialize empty ingredients and instructions arrays
        this.recipe.set({
          ...this.recipe(),
          ingredients: [this.createEmptyIngredient()],
          instructions: [''],
        });
      }
    });

    // Ensure we have at least one ingredient and instruction
    if (!this.recipe().ingredients?.length) {
      this.recipe.set({
        ...this.recipe(),
        ingredients: [this.createEmptyIngredient()],
      });
    }
    if (!this.recipe().instructions?.length) {
      this.recipe.set({
        ...this.recipe(),
        instructions: [''],
      });
    }
  }

  createEmptyIngredient(): Ingredient {
    return {
      id: Math.random().toString(36),
      name: '',
      amount: 0,
      unit: '',
    };
  }

  addIngredient(): void {
    const current = this.recipe();
    this.recipe.set({
      ...current,
      ingredients: [...(current.ingredients || []), this.createEmptyIngredient()],
    });
  }

  removeIngredient(index: number): void {
    const current = this.recipe();
    const ingredients = current.ingredients || [];
    this.recipe.set({
      ...current,
      ingredients: ingredients.filter((_, i) => i !== index),
    });
  }

  addInstruction(): void {
    const current = this.recipe();
    this.recipe.set({
      ...current,
      instructions: [...(current.instructions || []), ''],
    });
  }

  removeInstruction(index: number): void {
    const current = this.recipe();
    const instructions = current.instructions || [];
    this.recipe.set({
      ...current,
      instructions: instructions.filter((_, i) => i !== index),
    });
  }

  moveInstructionUp(index: number): void {
    if (index > 0) {
      const current = this.recipe();
      const instructions = [...(current.instructions || [])];
      [instructions[index - 1], instructions[index]] = [instructions[index], instructions[index - 1]];
      this.recipe.set({
        ...current,
        instructions,
      });
    }
  }

  moveInstructionDown(index: number): void {
    const current = this.recipe();
    const instructions = current.instructions || [];
    if (index < instructions.length - 1) {
      const updated = [...instructions];
      [updated[index], updated[index + 1]] = [updated[index + 1], updated[index]];
      this.recipe.set({
        ...current,
        instructions: updated,
      });
    }
  }

  updateIngredient(index: number, field: keyof Ingredient, value: any): void {
    const current = this.recipe();
    const ingredients = current.ingredients || [];
    const updated = [...ingredients];
    if (updated[index]) {
      updated[index] = {
        ...updated[index],
        [field]: value,
      };
    }
    this.recipe.set({
      ...current,
      ingredients: updated,
    });
  }

  updateInstruction(index: number, value: string): void {
    const current = this.recipe();
    const instructions = current.instructions || [];
    const updated = [...instructions];
    updated[index] = value;
    this.recipe.set({
      ...current,
      instructions: updated,
    });
  }

  saveRecipe(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/recipes']);
      return;
    }

    const recipe = this.recipe();
    
    // Validation
    if (!recipe.name?.trim()) {
      alert('Recipe name is required');
      return;
    }

    const validIngredients = recipe.ingredients?.filter(ing => ing.name.trim());
    if (!validIngredients || validIngredients.length === 0) {
      alert('At least one ingredient is required');
      return;
    }

    const validInstructions = recipe.instructions?.filter(inst => inst.trim());
    if (!validInstructions || validInstructions.length === 0) {
      alert('At least one instruction is required');
      return;
    }

    const recipeData = {
      ...recipe,
      ingredients: validIngredients,
      instructions: validInstructions,
    } as Omit<Recipe, 'id' | 'createdAt' | 'updatedAt'>;

    if (this.isEditMode() && recipe.id) {
      this.recipeService.updateRecipe(recipe.id, { ...recipeData }).subscribe(updatedRecipe => {
        if (updatedRecipe) {
          this.router.navigate(['/recipe', updatedRecipe.id]);
        }
      });
    } else {
      this.recipeService.createRecipe(recipeData).subscribe(newRecipe => {
        this.router.navigate(['/recipe', newRecipe.id]);
      });
    }
  }

  cancel(): void {
    if (this.isEditMode() && this.recipe().id) {
      this.router.navigate(['/recipe', this.recipe().id]);
    } else {
      this.router.navigate(['/recipes']);
    }
  }
}

