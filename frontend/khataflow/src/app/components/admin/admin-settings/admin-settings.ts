import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../services/auth-service';
import { UserService } from '../../../services/user-service';
import { PlatformReportService } from '../../../services/platform-report-service';
import { ReportPeriod } from '../../../core/enums/report-period';
import { User, UserUpdateRequest } from '../../../core/models/user-model';

@Component({
  selector: 'app-admin-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-settings.html',
  styleUrl: './admin-settings.css',
})
export class AdminSettings implements OnInit {
  private authService = inject(AuthService);
  private userService = inject(UserService);
  private reportService = inject(PlatformReportService);

  // ---- Admin account (UserService) ----
  private currentUser = signal<User | null>(null);

  accountLoading = signal(false);
  accountSaving = signal(false);
  accountError = signal<string | null>(null);
  accountSaved = signal(false);

  account = {
    fullName: '',
    email: '',
    phoneNumber: '',
  };

  // No self-service "set new password" endpoint exists — only forgotPassword
  // (sends a reset link) / resetPassword (consumes a token). This triggers
  // that same email flow rather than accepting a raw new-password value.
  resetSendingEmail = signal(false);
  resetEmailSent = signal(false);

  // ---- Recent Admin Activity (PlatformReportService) ----
  activityLoading = this.reportService.loading;
  activityLog = computed(() =>
    this.reportService.recentActivity().map((event) => ({
      id: event.id,
      message: event.message,
      time: this.formatRelativeTime(event.timestamp),
      ...this.iconFor(event.type),
    }))
  );

  ngOnInit(): void {
    this.loadAccount();
    this.reportService.loadPlatformReport(ReportPeriod.Week);
  }

  private loadAccount(): void {
    const userId = this.authService.getCurrentUserId();
    if (!userId) {
      this.accountError.set('No authenticated user found.');
      return;
    }

    this.accountLoading.set(true);
    this.accountError.set(null);

    this.userService.getUserById(userId).subscribe({
      next: (res) => {
        this.currentUser.set(res.data);
        this.applyUserToForm(res.data);
      },
      error: () => this.accountError.set('Failed to load account details.'),
      complete: () => this.accountLoading.set(false),
    });
  }

  private applyUserToForm(user: User): void {
    this.account = {
      fullName: user.fullName,
      email: user.email,
      phoneNumber: user.phoneNumber ?? '',
    };
  }

  saveAccount(): void {
    const userId = this.authService.getCurrentUserId();
    if (!userId) {
      this.accountError.set('No authenticated user found.');
      return;
    }

    const request: UserUpdateRequest = {
      fullName: this.account.fullName,
      email: this.account.email,
      phoneNumber: this.account.phoneNumber || undefined,
    };

    this.accountSaving.set(true);
    this.accountSaved.set(false);
    this.accountError.set(null);

    this.userService.updateUser(userId, request).subscribe({
      next: (res) => {
        this.currentUser.set(res.data);
        this.applyUserToForm(res.data);
        this.accountSaved.set(true);
      },
      error: () => this.accountError.set('Failed to save changes.'),
      complete: () => this.accountSaving.set(false),
    });
  }

  resetAccount(): void {
    const user = this.currentUser();
    if (!user) return;
    this.applyUserToForm(user);
    this.accountSaved.set(false);
    this.accountError.set(null);
    this.resetEmailSent.set(false);
  }

  sendPasswordResetEmail(): void {
    if (!this.account.email) return;

    this.resetSendingEmail.set(true);
    this.resetEmailSent.set(false);
    this.accountError.set(null);

    this.authService.forgotPassword({ email: this.account.email }).subscribe({
      next: () => this.resetEmailSent.set(true),
      error: () => this.accountError.set('Failed to send password reset email.'),
      complete: () => this.resetSendingEmail.set(false),
    });
  }

  // ---- Formatting helpers ----

  private formatRelativeTime(iso: string): string {
    const then = new Date(iso).getTime();
    const diffMs = Date.now() - then;
    const mins = Math.floor(diffMs / 60000);
    if (mins < 1) return 'Just now';
    if (mins < 60) return `${mins} min ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return `${hrs} hr ago`;
    const days = Math.floor(hrs / 24);
    if (days === 1) return 'Yesterday';
    return `${days} days ago`;
  }

  private iconFor(type: string): { icon: string; iconBg: string; iconColor: string } {
    const map: Record<string, { icon: string; iconBg: string; iconColor: string }> = {
      UserSignup: { icon: 'fa-solid fa-user-plus', iconBg: 'bg-indigo-50', iconColor: 'text-indigo-500' },
      SubscriptionUpgraded: { icon: 'fa-solid fa-arrow-up', iconBg: 'bg-amber-50', iconColor: 'text-amber-500' },
      SubscriptionCancelled: { icon: 'fa-solid fa-circle-xmark', iconBg: 'bg-red-50', iconColor: 'text-red-400' },
      SubscriptionRenewed: { icon: 'fa-solid fa-rotate-right', iconBg: 'bg-green-50', iconColor: 'text-green-500' },
      BusinessRegistered: { icon: 'fa-solid fa-building', iconBg: 'bg-indigo-50', iconColor: 'text-indigo-500' },
    };
    return map[type] ?? { icon: 'fa-solid fa-bolt', iconBg: 'bg-hover-bg', iconColor: 'text-text-muted' };
  }
}