import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDividerModule } from '@angular/material/divider';
import { Recipe, RecipeService } from '../../services/recipe';
import { RecipeCardComponent } from '../recipe-card/recipe-card';

@Component({
  selector: 'app-recipe-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatToolbarModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDividerModule,
    RecipeCardComponent,
  ],
  templateUrl: './recipe-list.html',
  styleUrl: './recipe-list.scss',
})
export class RecipeListComponent implements OnInit {
  searchQuery = signal<string>('');
  selectedIngredient = signal<string>('');
  
  allIngredients: string[] = [];

  filteredRecipes = computed(() => {
    const query = this.searchQuery();
    const ingredient = this.selectedIngredient();
    const recipes = this.recipeService.getRecipes();

    if (query) {
      return this.recipeService.searchRecipes(query);
    } else if (ingredient) {
      return this.recipeService.filterByIngredient(ingredient);
    }
    return recipes;
  });

  constructor(protected recipeService: RecipeService) {}

  ngOnInit(): void {
    this.allIngredients = this.recipeService.allIngredients();
  }

  onSearchChange(): void {
    this.selectedIngredient.set('');
  }

  onIngredientChange(): void {
    this.searchQuery.set('');
  }

  clearFilters(): void {
    this.searchQuery.set('');
    this.selectedIngredient.set('');
  }

  deleteRecipe(id: string): void {
    if (confirm('Are you sure you want to delete this recipe?')) {
      this.recipeService.deleteRecipe(id);
    }
  }
}

