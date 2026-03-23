import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { environment } from '../../environments/environment';

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
  creatorName?: string;
  createdAt: Date;
  updatedAt: Date;
}

type RecipeDto = Omit<Recipe, 'createdAt' | 'updatedAt'> & {
  createdAt: string;
  updatedAt: string;
};

@Injectable({
  providedIn: 'root',
})
export class RecipeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/recipes`;
  private recipes = signal<Recipe[]>([]);

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

  refreshRecipes(): Observable<Recipe[]> {
    return this.http.get<RecipeDto[]>(this.apiUrl).pipe(
      map(items => items.map(item => this.fromDto(item))),
      tap(items => this.recipes.set(items)),
      catchError(() => of(this.recipes()))
    );
  }

  getRecipes(): Recipe[] {
    return this.recipes();
  }

  getRecipeById(id: string): Recipe | undefined {
    return this.recipes().find(r => r.id === id);
  }

  loadRecipeById(id: string): Observable<Recipe | undefined> {
    return this.http.get<RecipeDto>(`${this.apiUrl}/${id}`).pipe(
      map(item => this.fromDto(item)),
      tap(recipe => {
        const next = this.upsertInMemory(this.recipes(), recipe);
        this.recipes.set(next);
      }),
      catchError(() => of(this.getRecipeById(id)))
    );
  }

  createRecipe(recipe: Omit<Recipe, 'id' | 'createdAt' | 'updatedAt'>): Observable<Recipe> {
    return this.http.post<RecipeDto>(this.apiUrl, recipe).pipe(
      map(item => this.fromDto(item)),
      tap(created => this.recipes.set([...this.recipes(), created]))
    );
  }

  updateRecipe(id: string, updates: Partial<Omit<Recipe, 'id' | 'createdAt'>>): Observable<Recipe | undefined> {
    const existing = this.getRecipeById(id);
    if (!existing) {
      return of(undefined);
    }

    const payload = {
      name: (updates.name ?? existing.name).trim(),
      description: (updates.description ?? existing.description).trim(),
      servings: updates.servings ?? existing.servings,
      prepTime: updates.prepTime ?? existing.prepTime,
      cookTime: updates.cookTime ?? existing.cookTime,
      ingredients: updates.ingredients ?? existing.ingredients,
      instructions: updates.instructions ?? existing.instructions,
      image: updates.image ?? existing.image,
    };

    return this.http.put<RecipeDto>(`${this.apiUrl}/${id}`, payload).pipe(
      map(item => this.fromDto(item)),
      tap(saved => this.recipes.set(this.upsertInMemory(this.recipes(), saved))),
      map(saved => saved)
    );
  }

  deleteRecipe(id: string): Observable<boolean> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      map(() => true),
      tap(() => this.recipes.set(this.recipes().filter(r => r.id !== id))),
      catchError(() => of(false))
    );
  }

  uploadImage(recipeId: string, file: File): Observable<Recipe> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<RecipeDto>(`${this.apiUrl}/${recipeId}/image`, formData).pipe(
      map(item => this.fromDto(item)),
      tap(saved => this.recipes.set(this.upsertInMemory(this.recipes(), saved)))
    );
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

  private fromDto(recipe: RecipeDto): Recipe {
    return {
      ...recipe,
      createdAt: new Date(recipe.createdAt),
      updatedAt: new Date(recipe.updatedAt),
    };
  }

  private upsertInMemory(recipes: Recipe[], recipe: Recipe): Recipe[] {
    const existing = recipes.some(item => item.id === recipe.id);
    if (!existing) {
      return [...recipes, recipe];
    }

    return recipes.map(item => (item.id === recipe.id ? recipe : item));
  }
}

