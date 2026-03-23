import { ChangeDetectionStrategy, Component, ElementRef, effect, inject, viewChild } from '@angular/core';
import { RouterOutlet, RouterModule } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from './services/auth';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterModule, MatToolbarModule, MatIconModule, MatButtonModule, MatTooltipModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  readonly authService = inject(AuthService);
  private readonly googleSignInButton = viewChild<ElementRef<HTMLElement>>('googleSignInButton');
  isDarkMode = false;

  constructor() {
    effect(() => {
      if (!this.authService.isReady() || this.authService.isAuthenticated() || !this.authService.isConfigured()) {
        return;
      }

      const buttonHost = this.googleSignInButton()?.nativeElement;
      if (!buttonHost) {
        return;
      }

      this.authService.renderButton(buttonHost);
    });
  }

  toggleDarkMode() {
    this.isDarkMode = !this.isDarkMode;
    document.documentElement.classList.toggle('dark-mode', this.isDarkMode);
  }

  login(): void {
    this.authService.login();
  }

  logout(): void {
    this.authService.logout();
  }
}
