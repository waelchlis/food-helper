import { Component, inject, computed, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RecipeService } from '../../services/recipe';
import { CategoryService } from '../../services/category';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterModule, MatButtonModule, MatIconModule],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class HomeComponent implements OnInit {
  private readonly recipeService = inject(RecipeService);
  private readonly categoryService = inject(CategoryService);
  readonly authService = inject(AuthService);

  readonly recipeCount = computed(() => this.recipeService.getRecipes().length);
  readonly categoryCount = computed(() => this.categoryService.allCategories().length);

  readonly features = [
    {
      icon: 'restaurant',
      title: 'Recipe Collection',
      description: 'Browse, search and filter your entire recipe library by ingredient, category or cooking time.',
      link: '/recipes',
      linkLabel: 'Browse Recipes',
    },
    {
      icon: 'shopping_cart',
      title: 'Shopping List',
      description: 'Build your grocery list on the fly — add ingredients from any recipe with a single tap.',
      link: '/shopping-list',
      linkLabel: 'Open List',
    },
    {
      icon: 'donut_large',
      title: 'Wheel of Fortune',
      description: "Can't decide what to cook? Spin the wheel and let fate choose tonight's dinner.",
      link: '/wheel',
      linkLabel: 'Spin the Wheel',
    },
  ];

  ngOnInit(): void {
    this.recipeService.refreshRecipes().subscribe();
    this.categoryService.loadAll();
  }
}
