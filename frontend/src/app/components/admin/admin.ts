import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AdminService, Admin } from '../../services/admin';
import { AuthService } from '../../services/auth';
import { CategoryService, Category } from '../../services/category';
import { RecipeService, RecipeImportResult } from '../../services/recipe';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatTooltipModule,
    MatSnackBarModule,
  ],
  templateUrl: './admin.html',
  styleUrl: './admin.scss',
})
export class AdminComponent implements OnInit {
  newEmail = '';
  error = signal('');

  // Category management
  newCategoryName = '';
  categoryError = signal('');
  editingCategoryId = signal<string | null>(null);
  editingCategoryName = signal('');

  // Import / export
  importing = signal(false);
  importResults = signal<RecipeImportResult[] | null>(null);

  constructor(
    public adminService: AdminService,
    public authService: AuthService,
    public categoryService: CategoryService,
    private recipeService: RecipeService,
    private snackBar: MatSnackBar,
  ) {}

  ngOnInit(): void {
    this.adminService.loadAdmins().subscribe();
    this.categoryService.loadAll();
  }

  addAdmin(): void {
    const email = this.newEmail.trim();
    if (!email) return;

    this.error.set('');
    this.adminService.addAdmin(email).subscribe({
      next: () => {
        this.newEmail = '';
      },
      error: () => {
        this.error.set('Failed to add admin. Please check the email and try again.');
      },
    });
  }

  removeAdmin(admin: Admin): void {
    this.error.set('');
    this.adminService.removeAdmin(admin.id).subscribe({
      next: (removed) => {
        if (!removed) {
          this.error.set('Failed to remove admin.');
        }
      },
      error: () => {
        this.error.set('Failed to remove admin.');
      },
    });
  }

  isSelf(admin: Admin): boolean {
    const email = this.authService.userEmail();
    return !!email && admin.email.toLowerCase() === email.toLowerCase();
  }

  addCategory(): void {
    const name = this.newCategoryName.trim();
    if (!name) return;

    this.categoryError.set('');
    this.categoryService.add(name).subscribe({
      next: () => {
        this.newCategoryName = '';
      },
      error: () => {
        this.categoryError.set('Failed to add category. It may already exist.');
      },
    });
  }

  startEditCategory(category: Category): void {
    this.editingCategoryId.set(category.id);
    this.editingCategoryName.set(category.name);
  }

  cancelEditCategory(): void {
    this.editingCategoryId.set(null);
    this.editingCategoryName.set('');
  }

  saveEditCategory(id: string): void {
    const name = this.editingCategoryName().trim();
    if (!name) return;

    this.categoryError.set('');
    this.categoryService.rename(id, name).subscribe({
      next: () => {
        this.editingCategoryId.set(null);
        this.editingCategoryName.set('');
      },
      error: () => {
        this.categoryError.set('Failed to rename category. The name may already be in use.');
      },
    });
  }

  exportRecipes(): void {
    this.recipeService.exportRecipes().subscribe({
      next: recipes => {
        const blob = new Blob([JSON.stringify(recipes, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `recipes-export-${new Date().toISOString().slice(0, 10)}.json`;
        link.click();
        URL.revokeObjectURL(url);
      },
      error: () => this.snackBar.open('Failed to export recipes.', 'Dismiss', { duration: 4000 }),
    });
  }

  async onImportFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.importResults.set(null);
    this.importing.set(true);

    try {
      const text = await file.text();
      const items = JSON.parse(text);
      if (!Array.isArray(items)) {
        throw new Error('Expected a JSON array of recipes.');
      }

      this.recipeService.importRecipes(items).subscribe({
        next: results => {
          this.importResults.set(results);
          this.importing.set(false);
          const successCount = results.filter(r => r.success).length;
          this.snackBar.open(`Imported ${successCount} of ${results.length} recipes.`, undefined, { duration: 4000 });
        },
        error: () => {
          this.importing.set(false);
          this.snackBar.open('Import failed.', 'Dismiss', { duration: 4000 });
        },
      });
    } catch {
      this.importing.set(false);
      this.snackBar.open('Selected file is not valid JSON.', 'Dismiss', { duration: 4000 });
    } finally {
      input.value = '';
    }
  }

  removeCategory(id: string): void {
    this.categoryError.set('');
    this.categoryService.delete(id).subscribe({
      next: (removed) => {
        if (!removed) {
          this.categoryError.set('Failed to remove category.');
        }
      },
      error: () => {
        this.categoryError.set('Cannot remove category — it may still be used by one or more recipes.');
      },
    });
  }
}
