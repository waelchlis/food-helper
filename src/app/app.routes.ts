import { Routes } from '@angular/router';
import { RecipeListComponent } from './components/recipe-list/recipe-list';
import { RecipeDetailComponent } from './components/recipe-detail/recipe-detail';
import { RecipeFormComponent } from './components/recipe-form/recipe-form';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'recipes',
    pathMatch: 'full',
  },
  {
    path: 'r',
    redirectTo: 'recipes',
  },
  {
    path: 'recipes',
    component: RecipeListComponent,
  },
  {
    path: 'recipe/new',
    component: RecipeFormComponent,
  },
  {
    path: 'recipe/:id',
    component: RecipeDetailComponent,
  },
  {
    path: 'recipe/:id/edit',
    component: RecipeFormComponent,
  },
  {
    path: '**',
    redirectTo: 'recipes',
  },
];

