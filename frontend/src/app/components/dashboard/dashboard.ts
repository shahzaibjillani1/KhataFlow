import { Component, inject, OnInit, signal } from '@angular/core';
import { Header } from '../../shared/components/header/header';
import { Sidebar } from '../../shared/components/sidebar/sidebar';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from '../../services/auth-service';
import { LanguageService } from '../../services/language-service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [Header, RouterOutlet, Sidebar],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  private router = inject(Router);
  private authService = inject(AuthService);
  private languageService = inject(LanguageService);

  // Language — single source of truth, shared with every other component (e.g. Settings)
  readonly lang = this.languageService.currentLang;

  isCollapsed = false;
  isMobileOpen = false;
  activeTitleKey = 'sidebar.dashboard';

  role = signal<string | null>(null);

  ngOnInit() {
    this.role.set(this.authService.getRole());

    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => {
        this.activeTitleKey = this.titleFromUrl(this.router.url);
      });

    this.activeTitleKey = this.titleFromUrl(this.router.url);
  }

  onTabSelected(titleKey: string) {
    this.activeTitleKey = titleKey;
  }

  toggleSidebar() {
    if (window.innerWidth < 640) {
      this.isMobileOpen = !this.isMobileOpen;
    } else {
      this.isCollapsed = !this.isCollapsed;
    }
  }

  private titleFromUrl(url: string): string {
    const segment = url.split('/').filter(Boolean).pop() ?? '';
    const titles: Record<string, string> = {
      'shop-owner-dashboard': 'sidebar.dashboard',
      'admin-dashboard': 'sidebar.dashboard',
      sales: 'sidebar.sales',
      products: 'sidebar.products',
      customers: 'sidebar.customers',
      reports: 'sidebar.reports',
      invoice: 'invoice.title',
      settings: 'sidebar.settings',
      users: 'sidebar.usersAndBusinesses',
      subscriptions: 'sidebar.subscriptions',
      analytics: 'sidebar.analytics',
      notifications: 'header.notifications',
    };
    return titles[segment] ?? this.activeTitleKey;
  }
}