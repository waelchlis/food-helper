import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';

export interface EditMealPlanDialogData {
  mode: 'create' | 'edit';
  name?: string;
  description?: string;
}

export interface EditMealPlanDialogResult {
  name: string;
  description: string;
}

@Component({
  selector: 'app-edit-meal-plan-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
  ],
  templateUrl: './edit-meal-plan-dialog.html',
  styleUrl: './edit-meal-plan-dialog.scss',
})
export class EditMealPlanDialogComponent {
  name: string;
  description: string;

  get isEdit(): boolean {
    return this.data.mode === 'edit';
  }

  constructor(
    private dialogRef: MatDialogRef<EditMealPlanDialogComponent, EditMealPlanDialogResult>,
    @Inject(MAT_DIALOG_DATA) public data: EditMealPlanDialogData,
  ) {
    this.name = data.name ?? '';
    this.description = data.description ?? '';
  }

  submit(): void {
    if (!this.name.trim()) return;
    this.dialogRef.close({ name: this.name.trim(), description: this.description.trim() });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
