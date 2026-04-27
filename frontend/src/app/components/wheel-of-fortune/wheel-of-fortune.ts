import {
  Component,
  OnInit,
  AfterViewInit,
  OnDestroy,
  ElementRef,
  viewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatDividerModule } from '@angular/material/divider';
import { MatSelectModule } from '@angular/material/select';
import { RecipeService, Recipe } from '../../services/recipe';
import { IngredientWordService } from '../../services/ingredient-word';
import { CategoryService } from '../../services/category';

const SEGMENT_COLORS = [
  '#ef9a9a', '#ce93d8', '#90caf9', '#80cbc4',
  '#a5d6a7', '#fff59d', '#ffcc80', '#bcaaa4',
  '#ef9a9a', '#ce93d8', '#90caf9', '#80cbc4',
  '#a5d6a7', '#fff59d', '#ffcc80', '#bcaaa4',
];

@Component({
  selector: 'app-wheel-of-fortune',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatAutocompleteModule,
    MatDividerModule,
    MatSelectModule,
  ],
  templateUrl: './wheel-of-fortune.html',
  styleUrl: './wheel-of-fortune.scss',
})
export class WheelOfFortuneComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly canvasRef = viewChild<ElementRef<HTMLCanvasElement>>('wheelCanvas');
  private readonly recipeService = inject(RecipeService);
  private readonly ingredientWordService = inject(IngredientWordService);
  private readonly categoryService = inject(CategoryService);
  private readonly router = inject(Router);

  ingredientInput = signal('');
  selectedIngredients = signal<string[]>([]);
  selectedCategories = signal<string[]>([]);
  maxTotalTime = signal<number>(0);
  spinning = signal(false);
  winner = signal<Recipe | null>(null);

  readonly allCategories = computed(() => this.categoryService.allCategories());

  readonly timeOptions = [
    { label: '15 minutes', value: 15 },
    { label: '30 minutes', value: 30 },
    { label: '45 minutes', value: 45 },
    { label: '60 minutes', value: 60 },
    { label: '90 minutes', value: 90 },
  ];

  filteredRecipes = computed(() => {
    const ingredients = this.selectedIngredients();
    const categories = this.selectedCategories();
    const maxTime = this.maxTotalTime();
    const recipes = this.recipeService.getRecipes();
    return recipes.filter(r => {
      const matchesCategories = !categories.length || categories.includes(r.categoryId ?? '');
      const matchesIngredients = !ingredients.length || ingredients.every(ing =>
        r.ingredients.some(i => i.name.toLowerCase().includes(ing.toLowerCase()))
      );
      const matchesTime = maxTime === 0 || r.prepTime + r.cookTime <= maxTime;
      return matchesCategories && matchesIngredients && matchesTime;
    });
  });

  autocompleteOptions = computed(() => {
    const input = this.ingredientInput().toLowerCase();
    const selected = this.selectedIngredients();
    return this.ingredientWordService.allWords()
      .map(w => w.name)
      .filter(n => !selected.includes(n) && (!input || n.toLowerCase().includes(input)));
  });

  private currentRotation = 0;
  private animationId: number | null = null;

  ngOnInit(): void {
    this.recipeService.refreshRecipes().subscribe(() => this.drawWheel());
    this.ingredientWordService.loadAll();
    this.categoryService.loadAll();
  }

  ngAfterViewInit(): void {
    this.drawWheel();
  }

  ngOnDestroy(): void {
    if (this.animationId !== null) {
      cancelAnimationFrame(this.animationId);
    }
  }

  addIngredient(name?: string): void {
    const trimmed = (name ?? this.ingredientInput()).trim();
    this.ingredientInput.set('');
    if (!trimmed || this.selectedIngredients().includes(trimmed)) return;
    this.selectedIngredients.set([...this.selectedIngredients(), trimmed]);
    this.winner.set(null);
    this.drawWheel();
  }

  removeIngredient(ingredient: string): void {
    this.selectedIngredients.set(this.selectedIngredients().filter(i => i !== ingredient));
    this.winner.set(null);
    this.drawWheel();
  }

  toggleCategory(id: string): void {
    const current = this.selectedCategories();
    const next = current.includes(id) ? current.filter(c => c !== id) : [...current, id];
    this.selectedCategories.set(next);
    this.winner.set(null);
    this.drawWheel();
  }

  resetAllFilters(): void {
    this.selectedCategories.set([]);
    this.selectedIngredients.set([]);
    this.maxTotalTime.set(0);
    this.ingredientInput.set('');
    this.winner.set(null);
    this.drawWheel();
  }

  spin(): void {
    const recipes = this.filteredRecipes();
    if (!recipes.length || this.spinning()) return;

    this.winner.set(null);
    this.spinning.set(true);

    const extraSpins = 6 + Math.floor(Math.random() * 6);
    const randomAngle = Math.random() * Math.PI * 2;
    const targetRotation = this.currentRotation + extraSpins * Math.PI * 2 + randomAngle;
    const duration = 4000 + Math.random() * 2000;
    const startTime = performance.now();
    const startRotation = this.currentRotation;

    const animate = (now: number) => {
      const elapsed = now - startTime;
      const t = Math.min(elapsed / duration, 1);
      const eased = 1 - Math.pow(1 - t, 4); // easeOutQuart
      this.currentRotation = startRotation + (targetRotation - startRotation) * eased;
      this.drawWheel();

      if (t < 1) {
        this.animationId = requestAnimationFrame(animate);
      } else {
        this.currentRotation = targetRotation;
        this.spinning.set(false);
        this.determineWinner();
      }
    };

    this.animationId = requestAnimationFrame(animate);
  }

  goToRecipe(recipe: Recipe): void {
    this.router.navigate(['/recipe', recipe.id]);
  }

  private determineWinner(): void {
    const recipes = this.filteredRecipes();
    if (!recipes.length) return;

    const n = recipes.length;
    const sliceAngle = (Math.PI * 2) / n;
    // Segment i starts at (rotation + i*sliceAngle - PI/2).
    // Pointer is at angle -PI/2. Relative to the start of segment 0:
    // relAngle = (-PI/2) - (rotation - PI/2) = -rotation
    const relAngle = ((-this.currentRotation) % (Math.PI * 2) + Math.PI * 2) % (Math.PI * 2);
    const winnerIndex = Math.floor(relAngle / sliceAngle) % n;
    this.winner.set(recipes[winnerIndex]);
    this.drawWheel();
  }

  protected drawWheel(): void {
    const canvas = this.canvasRef()?.nativeElement;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const size = canvas.width;
    const cx = size / 2;
    const cy = size / 2;
    const outerRadius = cx - 6;

    ctx.clearRect(0, 0, size, size);

    const recipes = this.filteredRecipes();
    const winner = this.winner();

    if (!recipes.length) {
      ctx.beginPath();
      ctx.arc(cx, cy, outerRadius, 0, Math.PI * 2);
      ctx.fillStyle = '#e0e0e0';
      ctx.fill();
      ctx.strokeStyle = '#bdbdbd';
      ctx.lineWidth = 3;
      ctx.stroke();
      ctx.fillStyle = '#757575';
      ctx.font = '15px Roboto, sans-serif';
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillText('No matching recipes', cx, cy);
      this.drawPointer(ctx, cx);
      return;
    }

    const n = recipes.length;
    const sliceAngle = (Math.PI * 2) / n;

    // Outer ring drop shadow
    ctx.save();
    ctx.shadowColor = 'rgba(0,0,0,0.35)';
    ctx.shadowBlur = 18;
    ctx.beginPath();
    ctx.arc(cx, cy, outerRadius, 0, Math.PI * 2);
    ctx.fillStyle = '#fff';
    ctx.fill();
    ctx.restore();

    for (let i = 0; i < n; i++) {
      const startAngle = this.currentRotation + i * sliceAngle - Math.PI / 2;
      const endAngle = startAngle + sliceAngle;
      const color = SEGMENT_COLORS[i % SEGMENT_COLORS.length];
      const isWinner = !!winner && recipes[i].id === winner.id;

      // Segment
      ctx.beginPath();
      ctx.moveTo(cx, cy);
      ctx.arc(cx, cy, outerRadius, startAngle, endAngle);
      ctx.closePath();
      ctx.fillStyle = isWinner ? '#FFD700' : color;
      ctx.fill();
      ctx.strokeStyle = 'rgba(255,255,255,0.6)';
      ctx.lineWidth = 1.5;
      ctx.stroke();

      // Label — hide when there are too many segments to avoid clutter
      if (n <= 24) {
        ctx.save();
        ctx.translate(cx, cy);
        ctx.rotate(startAngle + sliceAngle / 2);
        const fontSize = Math.min(13, Math.max(7, Math.floor(160 / n)));
        ctx.font = `600 ${fontSize}px Roboto, sans-serif`;
        ctx.textAlign = 'right';
        ctx.textBaseline = 'middle';
        ctx.fillStyle = isWinner ? '#4e3000' : '#37474f';
        ctx.shadowColor = 'rgba(255,255,255,0.5)';
        ctx.shadowBlur = 2;
        const label = this.truncateText(ctx, recipes[i].name, outerRadius * 0.72);
        ctx.fillText(label, outerRadius - 14, 0);
        ctx.restore();
      }
    }

    // Outer border ring
    ctx.beginPath();
    ctx.arc(cx, cy, outerRadius, 0, Math.PI * 2);
    ctx.strokeStyle = 'rgba(0,0,0,0.12)';
    ctx.lineWidth = 3;
    ctx.stroke();

    // Center hub
    ctx.beginPath();
    ctx.arc(cx, cy, 18, 0, Math.PI * 2);
    ctx.fillStyle = '#fff';
    ctx.shadowColor = 'rgba(0,0,0,0.2)';
    ctx.shadowBlur = 4;
    ctx.fill();
    ctx.shadowBlur = 0;
    ctx.strokeStyle = 'rgba(0,0,0,0.15)';
    ctx.lineWidth = 2;
    ctx.stroke();

    this.drawPointer(ctx, cx);
  }

  private drawPointer(ctx: CanvasRenderingContext2D, cx: number): void {
    const tipY = 24;
    const baseY = 3;
    const halfBase = 11;

    ctx.save();
    ctx.shadowColor = 'rgba(0,0,0,0.4)';
    ctx.shadowBlur = 6;
    ctx.beginPath();
    ctx.moveTo(cx, tipY);
    ctx.lineTo(cx - halfBase, baseY);
    ctx.lineTo(cx + halfBase, baseY);
    ctx.closePath();
    ctx.fillStyle = '#D32F2F';
    ctx.fill();
    ctx.strokeStyle = '#fff';
    ctx.lineWidth = 1.5;
    ctx.stroke();
    ctx.restore();
  }

  private truncateText(ctx: CanvasRenderingContext2D, text: string, maxWidth: number): string {
    if (ctx.measureText(text).width <= maxWidth) return text;
    let t = text;
    while (t.length > 1 && ctx.measureText(t + '…').width > maxWidth) {
      t = t.slice(0, -1);
    }
    return t + '…';
  }
}
