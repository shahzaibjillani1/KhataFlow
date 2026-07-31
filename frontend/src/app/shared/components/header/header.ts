import { NgClass } from '@angular/common';
import {
  Component,
  EventEmitter,
  HostListener,
  computed,
  inject,
  Input,
  OnInit,
  Output,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { AuthService } from '../../../services/auth-service';
import { NotificationService } from '../../../services/notification-service';
import { LanguageService } from '../../../services/language-service';
import { LocalizedTextPipe } from '../../../shared/pipes/localized-text-pipe';
import { TokenStorageService } from '../../../services/token-storage-service';
import { BusinessSubscriptionService } from '../../../services/business-subscription-service';

@Component({
  selector: 'app-header',
  imports: [NgClass, RouterLink, TranslocoDirective, LocalizedTextPipe],
  templateUrl: './header.html',
  styleUrl: './header.css',
})
export class Header implements OnInit {
  private router = inject(Router);
  private authService = inject(AuthService);
  private languageService = inject(LanguageService);
  notificationService = inject(NotificationService);
  private tokenStorage = inject(TokenStorageService);
  private subscriptionService = inject(BusinessSubscriptionService);

  @Input() activeTitle = '';
  @Output() toggle = new EventEmitter<void>();

  readonly lang = this.languageService.currentLang;

  isNotifOpen = false;
  isDropdownOpen = false;

  notifications = this.notificationService.notifications;
  unreadCount = this.notificationService.unreadCount;

  avatarInitial = computed(() => {
    const email = this.authService.getCurrentUserEmail();
    return email ? email.charAt(0).toUpperCase() : '?';
  });

  ngOnInit() {
    this.notificationService.fetchAll().subscribe({
      error: (err) => console.error('Failed to load notifications', err),
    });
  }

  toggleNotifications() {
    this.isNotifOpen = !this.isNotifOpen;
    this.isDropdownOpen = false;
  }

  markRead(id: string) {
    this.notificationService.markRead(id).subscribe({
      error: (err) => console.error('Failed to mark notification read', err),
    });
  }

  markAllRead() {
    this.notificationService.markAllRead().subscribe({
      error: (err) => console.error('Failed to mark all notifications read', err),
    });
  }

  toggleDropdown() {
    this.isDropdownOpen = !this.isDropdownOpen;
  }

  @HostListener('document:click')
  closeAll() {
    this.isNotifOpen = false;
    this.isDropdownOpen = false;
  }

  toggleSidebar() {
    this.toggle.emit();
  }

  logout() {
    this.tokenStorage.clear(); 

    this.authService.logout().subscribe({
      next: () => { this.subscriptionService.refresh(); this.router.navigate(['/login']) },
      error: () => { this.subscriptionService.refresh(); this.router.navigate(['/login'])},
    });
  }
}
