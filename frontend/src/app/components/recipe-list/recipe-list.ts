import { Component, OnInit, computed, signal } from '@angular/core';
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
import { RecipeService } from '../../services/recipe';
import { RecipeCardComponent } from '../recipe-card/recipe-card';
import { AuthService } from '../../services/auth';

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
  
  allIngredients = computed(() => this.recipeService.allIngredients());

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

  constructor(
    protected recipeService: RecipeService,
    public authService: AuthService,
  ) {}

  ngOnInit(): void {
    this.recipeService.refreshRecipes().subscribe();
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
    if (!this.authService.isAuthenticated()) {
      return;
    }

    if (confirm('Are you sure you want to delete this recipe?')) {
      this.recipeService.deleteRecipe(id).subscribe();
    }
  }
}

