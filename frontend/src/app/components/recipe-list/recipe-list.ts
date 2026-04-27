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
import { CategoryService } from '../../services/category';

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
  selectedCategory = signal<string>('');
  maxTotalTime = signal<number>(0);

  readonly timeOptions = [
    { label: '15 minutes', value: 15 },
    { label: '30 minutes', value: 30 },
    { label: '45 minutes', value: 45 },
    { label: '60 minutes', value: 60 },
    { label: '90 minutes', value: 90 },
  ];

  allIngredients = computed(() => this.recipeService.allIngredients());
  allCategories = computed(() => this.categoryService.allCategories());

  filteredRecipes = computed(() => {
    const query = this.searchQuery();
    const ingredient = this.selectedIngredient();
    const categoryId = this.selectedCategory();
    const maxTime = this.maxTotalTime();
    const recipes = this.recipeService.getRecipes();

    let results: typeof recipes;
    if (query) {
      results = this.recipeService.searchRecipes(query);
    } else if (ingredient) {
      results = this.recipeService.filterByIngredient(ingredient);
    } else if (categoryId) {
      results = this.recipeService.filterByCategory(categoryId);
    } else {
      results = recipes;
    }

    if (maxTime > 0) {
      results = results.filter(r => r.prepTime + r.cookTime <= maxTime);
    }

    return results;
  });

  constructor(
    protected recipeService: RecipeService,
    public authService: AuthService,
    protected categoryService: CategoryService,
  ) {}

  ngOnInit(): void {
    this.recipeService.refreshRecipes().subscribe();
    this.categoryService.loadAll();
  }

  onSearchChange(): void {
    this.selectedIngredient.set('');
    this.selectedCategory.set('');
  }

  onIngredientChange(): void {
    this.searchQuery.set('');
    this.selectedCategory.set('');
  }

  onCategoryChange(): void {
    this.searchQuery.set('');
    this.selectedIngredient.set('');
  }

  clearFilters(): void {
    this.searchQuery.set('');
    this.selectedIngredient.set('');
    this.selectedCategory.set('');
    this.maxTotalTime.set(0);
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

