import { Component, Inject, OnInit, computed, signal } from '@angular/core';
import { Subject } from 'rxjs';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RecipeService, Recipe } from '../../services/recipe';
import { MealEntry, MealPlanService, AddMealEntryPayload } from '../../services/meal-plan';

export interface DayDetailDialogData {
  planId: string;
  date: string; // YYYY-MM-DD
  entries: MealEntry[];
}

/** Returns the final entries list so the parent can sync its signal. */
export interface DayDetailDialogResult {
  finalEntries: MealEntry[];
}

@Component({
  selector: 'app-day-detail-dialog',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatTabsModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    MatSnackBarModule,
    MatTooltipModule,
  ],
  templateUrl: './day-detail-dialog.html',
  styleUrl: './day-detail-dialog.scss',
})
export class DayDetailDialogComponent implements OnInit {
  searchQuery = signal('');
  customText = signal('');
  loadingRecipes = signal(false);
  savingEntry = signal(false);
  deletingId = signal<string | null>(null);

  entries = signal<MealEntry[]>([]);
  /** Emits the live entries list after every add or remove. */
  readonly entriesChange = new Subject<MealEntry[]>();

  filteredRecipes = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    const all = this.recipeService.getRecipes();
    if (!q) return all.slice(0, 30);
    return all.filter(r =>
      r.name.toLowerCase().includes(q) || r.description.toLowerCase().includes(q)
    ).slice(0, 30);
  });

  constructor(
    private dialogRef: MatDialogRef<DayDetailDialogComponent, DayDetailDialogResult>,
    @Inject(MAT_DIALOG_DATA) public data: DayDetailDialogData,
    private recipeService: RecipeService,
    private mealPlanService: MealPlanService,
    private snackBar: MatSnackBar,
  ) {}

  ngOnInit(): void {
    this.entries.set([...this.data.entries]);
    if (this.recipeService.getRecipes().length === 0) {
      this.loadingRecipes.set(true);
      this.recipeService.refreshRecipes().subscribe({
        next: () => this.loadingRecipes.set(false),
        error: () => this.loadingRecipes.set(false),
      });
    }
  }

  formatDate(dateKey: string): string {
    const [y, m, d] = dateKey.split('-').map(Number);
    return new Date(y, m - 1, d).toLocaleDateString(undefined, {
      weekday: 'long', month: 'long', day: 'numeric',
    });
  }

  addRecipe(recipe: Recipe): void {
    if (this.savingEntry()) return;
    const payload: AddMealEntryPayload = {
      date: this.data.date,
      type: 'recipe',
      recipeId: recipe.id,
      recipeName: recipe.name,
      recipeImage: recipe.images?.[0],
      servings: recipe.servings,
    };
    this.savingEntry.set(true);
    this.mealPlanService.addEntry(this.data.planId, payload).subscribe({
      next: entry => {
        this.entries.update(prev => [...prev, entry]);
        this.entriesChange.next(this.entries());
        this.searchQuery.set('');
        this.savingEntry.set(false);
      },
      error: () => {
        this.savingEntry.set(false);
        this.snackBar.open('Failed to add meal.', 'Dismiss', { duration: 4000 });
      },
    });
  }

  addCustom(): void {
    const text = this.customText().trim();
    if (!text || this.savingEntry()) return;
    const payload: AddMealEntryPayload = {
      date: this.data.date,
      type: 'custom',
      customText: text,
    };
    this.savingEntry.set(true);
    this.mealPlanService.addEntry(this.data.planId, payload).subscribe({
      next: entry => {
        this.entries.update(prev => [...prev, entry]);
        this.entriesChange.next(this.entries());
        this.customText.set('');
        this.savingEntry.set(false);
      },
      error: () => {
        this.savingEntry.set(false);
        this.snackBar.open('Failed to add meal.', 'Dismiss', { duration: 4000 });
      },
    });
  }

  removeEntry(entry: MealEntry): void {
    if (this.deletingId()) return;
    this.deletingId.set(entry.id);
    this.mealPlanService.deleteEntry(this.data.planId, entry.id).subscribe({
      next: () => {
        this.entries.update(prev => prev.filter(e => e.id !== entry.id));
        this.entriesChange.next(this.entries());
        this.deletingId.set(null);
      },
      error: () => {
        this.deletingId.set(null);
        this.snackBar.open('Failed to remove meal.', 'Dismiss', { duration: 4000 });
      },
    });
  }

  close(): void {
    this.dialogRef.close({ finalEntries: this.entries() });
  }
}
