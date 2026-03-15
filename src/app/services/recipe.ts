import { Injectable, signal, computed } from '@angular/core';

export interface Ingredient {
  id: string;
  name: string;
  amount: number;
  unit: string;
}

export interface Recipe {
  id: string;
  name: string;
  description: string;
  servings: number;
  prepTime: number;
  cookTime: number;
  ingredients: Ingredient[];
  instructions: string[];
  image?: string;
  createdAt: Date;
  updatedAt: Date;
}

@Injectable({
  providedIn: 'root',
})
export class RecipeService {
  private recipes = signal<Recipe[]>([
    {
      id: '1',
      name: 'Classic Spaghetti Carbonara',
      description: 'Traditional Italian pasta with eggs, cheese, and pancetta',
      servings: 4,
      prepTime: 10,
      cookTime: 20,
      ingredients: [
        { id: '1', name: 'Spaghetti', amount: 400, unit: 'grams' },
        { id: '2', name: 'Eggs', amount: 4, unit: 'whole' },
        { id: '3', name: 'Parmesan Cheese', amount: 200, unit: 'grams' },
        { id: '4', name: 'Pancetta', amount: 200, unit: 'grams' },
        { id: '5', name: 'Black Pepper', amount: 1, unit: 'teaspoon' },
        { id: '6', name: 'Salt', amount: 1, unit: 'teaspoon' },
      ],
      instructions: [
        'Bring a large pot of salted water to boil and cook spaghetti until al dente',
        'While pasta cooks, fry diced pancetta until crispy',
        'In a bowl, whisk together eggs, grated Parmesan, and black pepper',
        'Drain pasta and toss with pancetta and its fat',
        'Remove from heat and quickly stir in egg mixture until creamy',
        'Serve immediately with extra Parmesan',
      ],
      createdAt: new Date(),
      updatedAt: new Date(),
    },
    {
      id: '2',
      name: 'Chocolate Chip Cookies',
      description: 'Delicious homemade cookies with chocolate chips',
      servings: 24,
      prepTime: 15,
      cookTime: 12,
      ingredients: [
        { id: '1', name: 'All-Purpose Flour', amount: 2.25, unit: 'cups' },
        { id: '2', name: 'Butter', amount: 1, unit: 'cup' },
        { id: '3', name: 'Brown Sugar', amount: 0.75, unit: 'cup' },
        { id: '4', name: 'White Sugar', amount: 0.75, unit: 'cup' },
        { id: '5', name: 'Eggs', amount: 2, unit: 'whole' },
        { id: '6', name: 'Vanilla Extract', amount: 1, unit: 'teaspoon' },
        { id: '7', name: 'Baking Soda', amount: 1, unit: 'teaspoon' },
        { id: '8', name: 'Salt', amount: 1, unit: 'teaspoon' },
        { id: '9', name: 'Chocolate Chips', amount: 2, unit: 'cups' },
      ],
      instructions: [
        'Preheat oven to 375°F (190°C)',
        'Cream together butter and both sugars',
        'Beat in eggs and vanilla extract',
        'In another bowl, combine flour, baking soda, and salt',
        'Mix dry ingredients into the butter mixture',
        'Stir in chocolate chips',
        'Drop rounded tablespoons of dough onto ungreased cookie sheets',
        'Bake for 9-12 minutes or until golden brown',
      ],
      createdAt: new Date(),
      updatedAt: new Date(),
    },
  ]);

  public readonly allRecipes = this.recipes.asReadonly();

  public readonly allIngredients = computed(() => {
    const ingredients = new Set<string>();
    this.recipes().forEach(recipe => {
      recipe.ingredients.forEach(ing => {
        ingredients.add(ing.name);
      });
    });
    return Array.from(ingredients).sort();
  });

  constructor() {}

  getRecipes(): Recipe[] {
    return this.recipes();
  }

  getRecipeById(id: string): Recipe | undefined {
    return this.recipes().find(r => r.id === id);
  }

  createRecipe(recipe: Omit<Recipe, 'id' | 'createdAt' | 'updatedAt'>): Recipe {
    const newRecipe: Recipe = {
      ...recipe,
      id: this.generateId(),
      createdAt: new Date(),
      updatedAt: new Date(),
    };
    this.recipes.set([...this.recipes(), newRecipe]);
    return newRecipe;
  }

  updateRecipe(id: string, updates: Partial<Omit<Recipe, 'id' | 'createdAt'>>): Recipe | undefined {
    const recipes = this.recipes();
    const updated = recipes.map(r => 
      r.id === id 
        ? { ...r, ...updates, updatedAt: new Date() }
        : r
    );
    this.recipes.set(updated);
    return updated.find(r => r.id === id);
  }

  deleteRecipe(id: string): boolean {
    const recipes = this.recipes();
    const filtered = recipes.filter(r => r.id !== id);
    const deleted = filtered.length < recipes.length;
    this.recipes.set(filtered);
    return deleted;
  }

  searchRecipes(query: string): Recipe[] {
    const lowerQuery = query.toLowerCase();
    return this.recipes().filter(recipe =>
      recipe.name.toLowerCase().includes(lowerQuery) ||
      recipe.description.toLowerCase().includes(lowerQuery)
    );
  }

  filterByIngredient(ingredientName: string): Recipe[] {
    return this.recipes().filter(recipe =>
      recipe.ingredients.some(ing => ing.name.toLowerCase() === ingredientName.toLowerCase())
    );
  }

  scaleIngredients(recipe: Recipe, servings: number): Ingredient[] {
    const scale = servings / recipe.servings;
    return recipe.ingredients.map(ing => ({
      ...ing,
      amount: ing.amount * scale,
    }));
  }

  private generateId(): string {
    return Date.now().toString(36) + Math.random().toString(36).substr(2);
  }
}

