import { Component, Inject, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RecipeService, Recipe } from '../../services/recipe';
import { AddMealEntryPayload } from '../../services/meal-plan';

export interface AddMealDialogData {
  planId: string;
  date: string; // YYYY-MM-DD
}

@Component({
  selector: 'app-add-meal-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatTabsModule,
    MatCardModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './add-meal-dialog.html',
  styleUrl: './add-meal-dialog.scss',
})
export class AddMealDialogComponent implements OnInit {
  searchQuery = signal('');
  customText = signal('');
  loadingRecipes = signal(false);
  selectedRecipe = signal<Recipe | null>(null);
  servings = signal(1);

  filteredRecipes = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    const all = this.recipeService.getRecipes();
    if (!q) return all.slice(0, 30);
    return all.filter(r =>
      r.name.toLowerCase().includes(q) || r.description.toLowerCase().includes(q)
    ).slice(0, 30);
  });

  constructor(
    private dialogRef: MatDialogRef<AddMealDialogComponent, AddMealEntryPayload>,
    @Inject(MAT_DIALOG_DATA) public data: AddMealDialogData,
    private recipeService: RecipeService,
  ) {}

  ngOnInit(): void {
    if (this.recipeService.getRecipes().length === 0) {
      this.loadingRecipes.set(true);
      this.recipeService.refreshRecipes().subscribe({
        next: () => this.loadingRecipes.set(false),
        error: () => this.loadingRecipes.set(false),
      });
    }
  }

  pickRecipe(recipe: Recipe): void {
    this.selectedRecipe.set(recipe);
    this.servings.set(recipe.servings || 1);
  }

  backToSearch(): void {
    this.selectedRecipe.set(null);
  }

  confirmRecipe(): void {
    const recipe = this.selectedRecipe();
    if (!recipe) return;

    this.dialogRef.close({
      date: this.data.date,
      type: 'recipe',
      recipeId: recipe.id,
      recipeName: recipe.name,
      recipeImage: recipe.images?.[0],
      servings: this.servings(),
    });
  }

  submitCustom(): void {
    const text = this.customText().trim();
    if (!text) return;
    this.dialogRef.close({
      date: this.data.date,
      type: 'custom',
      customText: text,
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }

  formatDate(dateStr: string): string {
    const [year, month, day] = dateStr.split('-').map(Number);
    return new Date(year, month - 1, day).toLocaleDateString(undefined, {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }
}
