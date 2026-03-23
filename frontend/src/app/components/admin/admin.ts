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

  constructor(
    public adminService: AdminService,
    public authService: AuthService,
  ) {}

  ngOnInit(): void {
    this.adminService.loadAdmins().subscribe();
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
    const subject = this.authService.getUserSubject();
    return admin.isLinked && !!subject;
  }
}
