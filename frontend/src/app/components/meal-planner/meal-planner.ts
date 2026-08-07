import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MealPlan, MealPlanService } from '../../services/meal-plan';
import { EditMealPlanDialogComponent, EditMealPlanDialogData, EditMealPlanDialogResult } from '../edit-meal-plan-dialog/edit-meal-plan-dialog';
import { ConfirmDialogService } from '../../shared/confirm-dialog';

@Component({
  selector: 'app-meal-planner',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTooltipModule,
  ],
  templateUrl: './meal-planner.html',
  styleUrl: './meal-planner.scss',
})
export class MealPlannerComponent implements OnInit {
  private readonly mealPlanService = inject(MealPlanService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly confirmDialog = inject(ConfirmDialogService);

  readonly mealPlans = this.mealPlanService.allMealPlans;
  loading = signal(true);
  deletingId = signal<string | null>(null);

  ngOnInit(): void {
    this.mealPlanService.loadMealPlans().subscribe({
      next: () => this.loading.set(false),
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load meal plans.', 'Dismiss', { duration: 4000 });
      },
    });
  }

  openCreateDialog(): void {
    const ref = this.dialog.open<EditMealPlanDialogComponent, EditMealPlanDialogData, EditMealPlanDialogResult>(
      EditMealPlanDialogComponent,
      {
        width: '480px',
        maxWidth: '95vw',
        data: { mode: 'create' },
      }
    );

    ref.afterClosed().subscribe(result => {
      if (!result) return;
      this.mealPlanService.createMealPlan(result.name, result.description).subscribe({
        next: () => this.snackBar.open('Meal plan created!', undefined, { duration: 2500, panelClass: 'snack-success' }),
        error: () => this.snackBar.open('Failed to create meal plan.', 'Dismiss', { duration: 4000 }),
      });
    });
  }

  async deletePlan(plan: MealPlan): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Delete meal plan',
      message: `Delete "${plan.name}"? This cannot be undone.`,
      confirmLabel: 'Delete',
      danger: true,
    });
    if (!confirmed) return;

    this.deletingId.set(plan.id);
    this.mealPlanService.deleteMealPlan(plan.id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.snackBar.open('Meal plan deleted.', undefined, { duration: 2500 });
      },
      error: () => {
        this.deletingId.set(null);
        this.snackBar.open('Failed to delete meal plan.', 'Dismiss', { duration: 4000 });
      },
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
  }
}
