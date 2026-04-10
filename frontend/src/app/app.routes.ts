import { Routes } from '@angular/router';
import { RecipeListComponent } from './components/recipe-list/recipe-list';
import { RecipeDetailComponent } from './components/recipe-detail/recipe-detail';
import { RecipeFormComponent } from './components/recipe-form/recipe-form';
import { ShoppingListComponent } from './components/shopping-list/shopping-list';
import { AdminComponent } from './components/admin/admin';
import { WheelOfFortuneComponent } from './components/wheel-of-fortune/wheel-of-fortune';
import { authGuard } from './guards/auth.guard';
import { adminGuard } from './guards/admin.guard';

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
    canActivate: [authGuard],
  },
  {
    path: 'recipe/:id',
    component: RecipeDetailComponent,
  },
  {
    path: 'recipe/:id/edit',
    component: RecipeFormComponent,
    canActivate: [authGuard],
  },
  {
    path: 'shopping-list',
    component: ShoppingListComponent,
  },
  {
    path: 'wheel',
    component: WheelOfFortuneComponent,
  },
  {
    path: 'admin',
    component: AdminComponent,
    canActivate: [adminGuard],
  },
  {
    path: '**',
    redirectTo: 'recipes',
  },
];

