import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDividerModule } from '@angular/material/divider';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { Recipe, RecipeService, Ingredient } from '../../services/recipe';
import { AuthService } from '../../services/auth';
import { IngredientWordService } from '../../services/ingredient-word';

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
    MatAutocompleteModule,
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
  selectedImageFile: File | null = null;
  imagePreview = signal<string | null>(null);
  ingredientFilters = signal<Record<number, string>>({});

  filteredIngredientWords = computed(() => {
    const words = this.ingredientWordService.allWords();
    const filters = this.ingredientFilters();
    const result: Record<number, string[]> = {};
    const indices = Object.keys(filters);
    for (const idx of indices) {
      const filter = (filters[+idx] || '').toLowerCase();
      result[+idx] = filter
        ? words.map(w => w.name).filter(n => n.toLowerCase().includes(filter))
        : words.map(w => w.name);
    }
    return result;
  });

  usedIngredientNames = computed(() => {
    const names = new Set<string>();
    this.recipeService.getRecipes().forEach(r => {
      r.ingredients.forEach(i => names.add(i.name.toLowerCase()));
    });
    return names;
  });

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private recipeService: RecipeService,
    protected authService: AuthService,
    protected ingredientWordService: IngredientWordService,
  ) {}

  ngOnInit(): void {
    this.ingredientWordService.loadAll();

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

    if (field === 'name') {
      this.ingredientFilters.set({ ...this.ingredientFilters(), [index]: value as string });
    }
  }

  onIngredientInputFocus(index: number): void {
    const current = this.recipe().ingredients?.[index]?.name || '';
    this.ingredientFilters.set({ ...this.ingredientFilters(), [index]: current });
  }

  onIngredientNameInput(index: number, value: string): void {
    this.ingredientFilters.set({ ...this.ingredientFilters(), [index]: value });
  }

  onIngredientNameEnter(index: number): void {
    const name = this.recipe().ingredients?.[index]?.name?.trim();
    if (!name) return;
    const exists = this.ingredientWordService.allWords().some(
      w => w.name.toLowerCase() === name.toLowerCase()
    );
    if (!exists) {
      this.ingredientWordService.add(name);
    }
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

  onImageFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      alert('Please select an image file');
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      alert('Image must be smaller than 5 MB');
      return;
    }

    this.selectedImageFile = file;
    const reader = new FileReader();
    reader.onload = () => this.imagePreview.set(reader.result as string);
    reader.readAsDataURL(file);
  }

  removeSelectedImage(): void {
    this.selectedImageFile = null;
    this.imagePreview.set(null);
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
          if (this.selectedImageFile) {
            this.recipeService.uploadImage(updatedRecipe.id, this.selectedImageFile).subscribe({
              next: () => this.router.navigate(['/recipe', updatedRecipe.id]),
              error: () => this.router.navigate(['/recipe', updatedRecipe.id]),
            });
          } else {
            this.router.navigate(['/recipe', updatedRecipe.id]);
          }
        }
      });
    } else {
      this.recipeService.createRecipe(recipeData).subscribe(newRecipe => {
        if (this.selectedImageFile) {
          this.recipeService.uploadImage(newRecipe.id, this.selectedImageFile).subscribe({
            next: () => this.router.navigate(['/recipe', newRecipe.id]),
            error: () => this.router.navigate(['/recipe', newRecipe.id]),
          });
        } else {
          this.router.navigate(['/recipe', newRecipe.id]);
        }
      });
    }
  }

  deleteIngredientWordByName(name: string, event: MouseEvent): void {
    event.stopPropagation();
    const word = this.ingredientWordService.allWords().find(
      w => w.name.toLowerCase() === name.toLowerCase()
    );
    if (word) {
      this.ingredientWordService.delete(word.id);
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

