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
import { MatSelectModule } from '@angular/material/select';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Recipe, RecipeService, Ingredient, RecipeRevision } from '../../services/recipe';
import { AuthService } from '../../services/auth';
import { IngredientWordService } from '../../services/ingredient-word';
import { CategoryService } from '../../services/category';
import { ConfirmDialogService } from '../../shared/confirm-dialog';

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
    MatSelectModule,
    MatButtonToggleModule,
    MatSnackBarModule,
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
    tips: [],
    images: [],
  });
  selectedImageFiles = signal<File[]>([]);
  newImagePreviews = signal<string[]>([]);
  dietType = '';
  ingredientFilters = signal<Record<number, string>>({});
  saving = signal(false);

  history = signal<RecipeRevision[]>([]);
  showHistory = signal(false);

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
    protected categoryService: CategoryService,
    private confirmDialog: ConfirmDialogService,
    private snackBar: MatSnackBar,
  ) {}

  ngOnInit(): void {
    this.ingredientWordService.loadAll();
    this.categoryService.loadAll();

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
            this.recipe.set({ ...existingRecipe, images: [...(existingRecipe.images || [])] });
            this.dietType = existingRecipe.dietType || '';
          }
        });
      } else {
        // Initialize empty ingredients and instructions arrays
        this.recipe.set({
          ...this.recipe(),
          ingredients: [this.createEmptyIngredient()],
          instructions: [''],
          tips: [],
          images: [],
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

  addTip(): void {
    const current = this.recipe();
    this.recipe.set({
      ...current,
      tips: [...(current.tips || []), ''],
    });
  }

  removeTip(index: number): void {
    const current = this.recipe();
    const tips = current.tips || [];
    this.recipe.set({
      ...current,
      tips: tips.filter((_, i) => i !== index),
    });
  }

  updateTip(index: number, value: string): void {
    const current = this.recipe();
    const tips = [...(current.tips || [])];
    tips[index] = value;
    this.recipe.set({
      ...current,
      tips,
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

  // ── Image gallery management ──────────────────────────────────────────

  onImageFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files || []);
    if (files.length === 0) return;

    const invalid = files.find(f => !f.type.startsWith('image/'));
    if (invalid) {
      this.snackBar.open('Please select only image files.', 'Dismiss', { duration: 4000 });
      return;
    }
    const tooLarge = files.find(f => f.size > 5 * 1024 * 1024);
    if (tooLarge) {
      this.snackBar.open('Each image must be smaller than 5 MB.', 'Dismiss', { duration: 4000 });
      return;
    }

    this.selectedImageFiles.set([...this.selectedImageFiles(), ...files]);
    for (const file of files) {
      const reader = new FileReader();
      reader.onload = () => this.newImagePreviews.set([...this.newImagePreviews(), reader.result as string]);
      reader.readAsDataURL(file);
    }
    input.value = '';
  }

  removeQueuedImage(index: number): void {
    this.selectedImageFiles.set(this.selectedImageFiles().filter((_, i) => i !== index));
    this.newImagePreviews.set(this.newImagePreviews().filter((_, i) => i !== index));
  }

  async removeExistingImage(url: string): Promise<void> {
    const recipeId = this.recipe().id;
    if (!recipeId) return;

    const confirmed = await this.confirmDialog.confirm({
      title: 'Remove image',
      message: 'Remove this image from the recipe?',
      confirmLabel: 'Remove',
      danger: true,
    });
    if (!confirmed) return;

    this.recipeService.removeImage(recipeId, url).subscribe({
      next: updated => this.recipe.set({ ...this.recipe(), images: updated.images }),
      error: () => this.snackBar.open('Failed to remove image.', 'Dismiss', { duration: 4000 }),
    });
  }

  moveExistingImage(index: number, direction: -1 | 1): void {
    const recipeId = this.recipe().id;
    const images = [...(this.recipe().images || [])];
    const targetIndex = index + direction;
    if (!recipeId || targetIndex < 0 || targetIndex >= images.length) return;

    [images[index], images[targetIndex]] = [images[targetIndex], images[index]];
    this.recipeService.reorderImages(recipeId, images).subscribe({
      next: updated => this.recipe.set({ ...this.recipe(), images: updated.images }),
      error: () => this.snackBar.open('Failed to reorder images.', 'Dismiss', { duration: 4000 }),
    });
  }

  private uploadQueuedImages(recipeId: string): Promise<void> {
    const files = this.selectedImageFiles();
    if (files.length === 0) return Promise.resolve();

    return new Promise((resolve) => {
      let remaining = files.length;
      files.forEach(file => {
        this.recipeService.uploadImage(recipeId, file).subscribe({
          next: () => {
            remaining--;
            if (remaining === 0) resolve();
          },
          error: () => {
            remaining--;
            if (remaining === 0) resolve();
          },
        });
      });
    });
  }

  // ── History (admin audit trail) ─────────────────────────────────────────

  toggleHistory(): void {
    const recipeId = this.recipe().id;
    if (!recipeId) return;

    this.showHistory.set(!this.showHistory());
    if (this.showHistory() && this.history().length === 0) {
      this.recipeService.getHistory(recipeId).subscribe(revisions => this.history.set(revisions));
    }
  }

  // ── Save / cancel ────────────────────────────────────────────────────

  async saveRecipe(): Promise<void> {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/recipes']);
      return;
    }

    const recipe = this.recipe();

    if (!recipe.name?.trim()) {
      this.snackBar.open('Recipe name is required.', 'Dismiss', { duration: 4000 });
      return;
    }

    const validIngredients = recipe.ingredients?.filter(ing => ing.name.trim());
    if (!validIngredients || validIngredients.length === 0) {
      this.snackBar.open('At least one ingredient is required.', 'Dismiss', { duration: 4000 });
      return;
    }

    const validInstructions = recipe.instructions?.filter(inst => inst.trim());
    if (!validInstructions || validInstructions.length === 0) {
      this.snackBar.open('At least one instruction is required.', 'Dismiss', { duration: 4000 });
      return;
    }

    const recipeData = {
      ...recipe,
      ingredients: validIngredients,
      instructions: validInstructions,
      tips: (recipe.tips || []).filter(t => t.trim()),
      images: recipe.images || [],
      dietType: (this.dietType || undefined) as 'vegan' | 'vegetarian' | undefined,
    } as Omit<Recipe, 'id' | 'createdAt' | 'updatedAt'>;

    this.saving.set(true);

    if (this.isEditMode() && recipe.id) {
      this.recipeService.updateRecipe(recipe.id, { ...recipeData }).subscribe(async updatedRecipe => {
        if (updatedRecipe) {
          await this.uploadQueuedImages(updatedRecipe.id);
          this.saving.set(false);
          this.router.navigate(['/recipe', updatedRecipe.id]);
        } else {
          this.saving.set(false);
        }
      });
    } else {
      this.recipeService.createRecipe(recipeData).subscribe(async newRecipe => {
        await this.uploadQueuedImages(newRecipe.id);
        this.saving.set(false);
        this.router.navigate(['/recipe', newRecipe.id]);
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
