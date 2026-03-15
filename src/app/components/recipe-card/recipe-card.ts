import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { Recipe } from '../../services/recipe';

@Component({
  selector: 'app-recipe-card',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
  ],
  templateUrl: './recipe-card.html',
  styleUrl: './recipe-card.scss',
})
export class RecipeCardComponent {
  @Input() recipe!: Recipe;
  @Output() delete = new EventEmitter<string>();

  constructor(private router: Router) {}

  viewRecipe(): void {
    this.router.navigate(['/recipe', this.recipe.id]);
  }

  onDelete(): void {
    this.delete.emit(this.recipe.id);
  }
}

