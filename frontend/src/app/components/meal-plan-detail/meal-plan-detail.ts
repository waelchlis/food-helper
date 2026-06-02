import { Component, OnInit, computed, signal } from '@angular/core';
import { takeUntil } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MealEntry, MealPlan, MealPlanService } from '../../services/meal-plan';
import { AuthService } from '../../services/auth';
import { AddMealDialogComponent, AddMealDialogData } from '../add-meal-dialog/add-meal-dialog';
import {
  EditMealPlanDialogComponent,
  EditMealPlanDialogData,
  EditMealPlanDialogResult,
} from '../edit-meal-plan-dialog/edit-meal-plan-dialog';
import {
  DayDetailDialogComponent,
  DayDetailDialogData,
  DayDetailDialogResult,
} from '../day-detail-dialog/day-detail-dialog';

interface CalendarDay {
  date: Date;
  dateKey: string; // YYYY-MM-DD
  isCurrentMonth: boolean;
  isToday: boolean;
}

@Component({
  selector: 'app-meal-plan-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTooltipModule,
    MatDialogModule,
  ],
  templateUrl: './meal-plan-detail.html',
  styleUrl: './meal-plan-detail.scss',
})
export class MealPlanDetailComponent implements OnInit {
  plan = signal<MealPlan | null>(null);
  entries = signal<MealEntry[]>([]);
  loading = signal(true);
  savingEntry = signal(false);
  deletingEntryId = signal<string | null>(null);

  viewYear = signal(new Date().getFullYear());
  viewMonth = signal(new Date().getMonth()); // 0-based

  newCollaboratorEmail = signal('');
  addingCollaborator = signal(false);
  removingEmail = signal<string | null>(null);

  readonly weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

  calendarDays = computed<CalendarDay[]>(() => {
    const year = this.viewYear();
    const month = this.viewMonth();
    const today = this.todayKey();

    // First day of month — ISO week starts on Monday (0=Mon)
    const firstOfMonth = new Date(year, month, 1);
    // getDay() returns 0=Sun, so we shift: Mon=0, Tue=1, ... Sun=6
    let startDow = firstOfMonth.getDay() - 1;
    if (startDow < 0) startDow = 6;

    // Last day of month
    const daysInMonth = new Date(year, month + 1, 0).getDate();

    const days: CalendarDay[] = [];

    // Leading days from previous month
    for (let i = startDow - 1; i >= 0; i--) {
      const d = new Date(year, month, -i);
      days.push({ date: d, dateKey: this.toDateKey(d), isCurrentMonth: false, isToday: this.toDateKey(d) === today });
    }

    // Days in this month
    for (let d = 1; d <= daysInMonth; d++) {
      const date = new Date(year, month, d);
      const key = this.toDateKey(date);
      days.push({ date, dateKey: key, isCurrentMonth: true, isToday: key === today });
    }

    // Trailing days to fill last row (max 6 rows × 7 = 42 cells, minimum fill to a full row)
    const remaining = 7 - (days.length % 7);
    if (remaining < 7) {
      for (let i = 1; i <= remaining; i++) {
        const d = new Date(year, month + 1, i);
        days.push({ date: d, dateKey: this.toDateKey(d), isCurrentMonth: false, isToday: this.toDateKey(d) === today });
      }
    }

    return days;
  });

  entriesByDay = computed(() => {
    const map = new Map<string, MealEntry[]>();
    for (const entry of this.entries()) {
      const list = map.get(entry.date) ?? [];
      list.push(entry);
      map.set(entry.date, list);
    }
    return map;
  });

  /** Days that have entries, sorted by date, for the mobile list view */
  daysWithEntries = computed(() => {
    const year = this.viewYear();
    const month = this.viewMonth();
    const monthStr = `${year}-${String(month + 1).padStart(2, '0')}`;
    const map = this.entriesByDay();
    return Array.from(map.entries())
      .filter(([key]) => key.startsWith(monthStr))
      .sort(([a], [b]) => a.localeCompare(b));
  });

  monthLabel = computed(() => {
    return new Date(this.viewYear(), this.viewMonth(), 1).toLocaleDateString(undefined, {
      month: 'long',
      year: 'numeric',
    });
  });

  isOwner = computed(() => {
    const p = this.plan();
    if (!p) return false;
    const email = this.authService.userEmail();
    return p.ownerEmail.toLowerCase() === email?.toLowerCase();
  });

  private todayKey(): string {
    return this.toDateKey(new Date());
  }

  constructor(
    private route: ActivatedRoute,
    private mealPlanService: MealPlanService,
    private authService: AuthService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.mealPlanService.reloadMealPlan(id).subscribe({
      next: plan => {
        if (plan) this.plan.set(plan);
        else this.snackBar.open('Meal plan not found.', 'Dismiss', { duration: 4000 });
      },
    });
    this.mealPlanService.getEntries(id).subscribe({
      next: entries => {
        this.entries.set(entries);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load entries.', 'Dismiss', { duration: 4000 });
      },
    });
  }

  prevMonth(): void {
    const m = this.viewMonth();
    if (m === 0) {
      this.viewMonth.set(11);
      this.viewYear.update(y => y - 1);
    } else {
      this.viewMonth.update(m => m - 1);
    }
  }

  nextMonth(): void {
    const m = this.viewMonth();
    if (m === 11) {
      this.viewMonth.set(0);
      this.viewYear.update(y => y + 1);
    } else {
      this.viewMonth.update(m => m + 1);
    }
  }

  goToToday(): void {
    const now = new Date();
    this.viewYear.set(now.getFullYear());
    this.viewMonth.set(now.getMonth());
  }

  openDayDialog(dateKey: string): void {
    const planId = this.plan()?.id;
    if (!planId) return;

    const ref = this.dialog.open<DayDetailDialogComponent, DayDetailDialogData, DayDetailDialogResult>(DayDetailDialogComponent, {
      width: '560px',
      maxWidth: '95vw',
      data: { planId, date: dateKey, entries: this.getEntriesForDay(dateKey) },
    });

    // Keep the calendar in sync while the dialog is still open.
    ref.componentInstance.entriesChange
      .pipe(takeUntil(ref.afterClosed()))
      .subscribe(liveEntries => {
        this.entries.update(prev => [
          ...prev.filter(e => e.date !== dateKey),
          ...liveEntries,
        ]);
      });
  }

  openAddMealDialog(dateKey: string): void {
    const planId = this.plan()?.id;
    if (!planId) return;

    const ref = this.dialog.open<AddMealDialogComponent, AddMealDialogData>(AddMealDialogComponent, {
      width: '540px',
      maxWidth: '95vw',
      data: { planId, date: dateKey },
    });

    ref.afterClosed().subscribe(payload => {
      if (!payload) return;
      this.savingEntry.set(true);
      this.mealPlanService.addEntry(planId, payload).subscribe({
        next: entry => {
          this.entries.update(prev => [...prev, entry]);
          this.savingEntry.set(false);
        },
        error: () => {
          this.savingEntry.set(false);
          this.snackBar.open('Failed to add meal.', 'Dismiss', { duration: 4000 });
        },
      });
    });
  }

  deleteEntry(entry: MealEntry): void {
    const planId = this.plan()?.id;
    if (!planId) return;
    this.deletingEntryId.set(entry.id);
    this.mealPlanService.deleteEntry(planId, entry.id).subscribe({
      next: () => {
        this.entries.update(prev => prev.filter(e => e.id !== entry.id));
        this.deletingEntryId.set(null);
      },
      error: () => {
        this.deletingEntryId.set(null);
        this.snackBar.open('Failed to delete entry.', 'Dismiss', { duration: 4000 });
      },
    });
  }

  openEditDialog(): void {
    const p = this.plan();
    if (!p) return;
    const ref = this.dialog.open<EditMealPlanDialogComponent, EditMealPlanDialogData, EditMealPlanDialogResult>(
      EditMealPlanDialogComponent,
      {
        width: '480px',
        maxWidth: '95vw',
        data: { mode: 'edit', name: p.name, description: p.description },
      }
    );
    ref.afterClosed().subscribe(result => {
      if (!result) return;
      this.mealPlanService.updateMealPlan(p.id, result.name, result.description).subscribe({
        next: updated => this.plan.set(updated),
        error: () => this.snackBar.open('Failed to update plan.', 'Dismiss', { duration: 4000 }),
      });
    });
  }

  addCollaborator(): void {
    const email = this.newCollaboratorEmail().trim();
    if (!email) return;
    const planId = this.plan()?.id;
    if (!planId) return;

    this.addingCollaborator.set(true);
    this.mealPlanService.addCollaborator(planId, email).subscribe({
      next: updated => {
        this.plan.set(updated);
        this.newCollaboratorEmail.set('');
        this.addingCollaborator.set(false);
        this.snackBar.open(`${email} added as collaborator.`, undefined, { duration: 2500, panelClass: 'snack-success' });
      },
      error: () => {
        this.addingCollaborator.set(false);
        this.snackBar.open('Failed to add collaborator.', 'Dismiss', { duration: 4000 });
      },
    });
  }

  removeCollaborator(email: string): void {
    const planId = this.plan()?.id;
    if (!planId) return;
    this.removingEmail.set(email);
    this.mealPlanService.removeCollaborator(planId, email).subscribe({
      next: updated => {
        this.plan.set(updated);
        this.removingEmail.set(null);
      },
      error: () => {
        this.removingEmail.set(null);
        this.snackBar.open('Failed to remove collaborator.', 'Dismiss', { duration: 4000 });
      },
    });
  }

  getEntriesForDay(dateKey: string): MealEntry[] {
    return this.entriesByDay().get(dateKey) ?? [];
  }

  formatDayForList(dateKey: string): string {
    const [y, m, d] = dateKey.split('-').map(Number);
    return new Date(y, m - 1, d).toLocaleDateString(undefined, {
      weekday: 'long', month: 'long', day: 'numeric',
    });
  }

  private toDateKey(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }
}
