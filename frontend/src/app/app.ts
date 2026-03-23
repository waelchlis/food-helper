import { Component, ElementRef, effect, inject, signal, viewChild } from '@angular/core';
import { RouterOutlet, RouterModule } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { AuthService } from './services/auth';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterModule, MatToolbarModule, MatIconModule, MatButtonModule, MatTooltipModule, MatSidenavModule, MatListModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  readonly authService = inject(AuthService);
  private readonly googleSignInButton = viewChild<ElementRef<HTMLElement>>('googleSignInButton');
  isDarkMode = false;
  mobileMenuOpen = signal(false);

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
