import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AdminService, Admin } from '../../services/admin';
import { AuthService } from '../../services/auth';
import { CategoryService, Category } from '../../services/category';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatTooltipModule,
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

  constructor(
    public adminService: AdminService,
    public authService: AuthService,
    public categoryService: CategoryService,
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
