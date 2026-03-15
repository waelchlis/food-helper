import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef } from '@angular/material/dialog';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ShoppingListService, ShoppingListItem } from '../../services/shopping-list';

@Component({
  selector: 'app-shopping-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatDialogModule,
  ],
  templateUrl: './shopping-list.html',
  styleUrl: './shopping-list.scss',
})
export class ShoppingListComponent {
  newName = '';
  newAmount = 1;
  newUnit = '';
  newNotes = '';

  constructor(
    public shoppingListService: ShoppingListService,
    private dialog: MatDialog,
  ) {}

  addItem(): void {
    if (!this.newName.trim() || !this.newUnit.trim()) return;
    this.shoppingListService.addItem(this.newName.trim(), this.newAmount, this.newUnit.trim(), this.newNotes.trim());
    this.newName = '';
    this.newAmount = 1;
    this.newUnit = '';
    this.newNotes = '';
  }

  removeItem(id: string): void {
    this.shoppingListService.removeItem(id);
  }

  updateAmount(item: ShoppingListItem, value: string): void {
    const amount = parseFloat(value);
    if (!isNaN(amount) && amount > 0) {
      this.shoppingListService.updateItem(item.id, { amount });
    }
  }

  updateNotes(item: ShoppingListItem, notes: string): void {
    this.shoppingListService.updateItem(item.id, { notes });
  }

  updateQuantity(item: ShoppingListItem, value: string): void {
    const amount = parseFloat(value);
    if (!isNaN(amount) && amount > 0) {
      this.shoppingListService.updateItem(item.id, { amount });
    }
  }

  clearAll(): void {
    this.dialog.open(ConfirmClearDialogComponent).afterClosed().subscribe(confirmed => {
      if (confirmed) this.shoppingListService.clearAll();
    });
  }
}

@Component({
  selector: 'app-confirm-clear-dialog',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatDialogModule],
  template: `
    <h2 mat-dialog-title>Clear Shopping List</h2>
    <mat-dialog-content>Are you sure you want to remove all items?</mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close(false)">Cancel</button>
      <button mat-raised-button color="warn" (click)="dialogRef.close(true)">Clear All</button>
    </mat-dialog-actions>
  `,
})
export class ConfirmClearDialogComponent {
  constructor(public dialogRef: MatDialogRef<ConfirmClearDialogComponent>) {}
}
