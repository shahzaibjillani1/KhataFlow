import { Routes } from '@angular/router';
import { guestGuard } from './core/guards/guest-guard';
import { authGuard } from './core/guards/auth-guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/landing-page/landing-page').then((m) => m.LandingPage),
    pathMatch: 'full',
    canActivate: [guestGuard],
  },
  {
    path: 'login',
    loadComponent: () => import('./components/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    loadComponent: () => import('./components/auth/register/register').then((m) => m.Register),
    canActivate: [guestGuard],
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./components/auth/forgot-password/forgot-password').then((m) => m.ForgotPassword),
    canActivate: [guestGuard],
  },
  {
    path: 'notifications',
    loadComponent: () =>
      import('./shared/components/notifications/notifications').then((m) => m.Notifications),
    canActivate: [authGuard],
  },
  {
    path: 'ledger/:token',
    loadComponent: () =>
      import('./components/public-ledger-view/public-ledger-view').then((m) => m.PublicLedgerView),
  },
  {
    path: 'shop-owner-dashboard',
    loadComponent: () => import('./components/dashboard/dashboard').then((m) => m.Dashboard),
    canActivate: [authGuard],
    data: { roles: ['Owner', 'Manager', 'Staff'] },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./components/shop-owner/shop-owner-dashboard/shop-owner-dashboard').then(
            (m) => m.ShopOwnerDashboard,
          ),
      },
      {
        path: 'sales',
        loadComponent: () => import('./components/shop-owner/sales/sales').then((m) => m.Sales),
      },
      {
        path: 'products',
        loadComponent: () =>
          import('./components/shop-owner/products/products').then((m) => m.Products),
      },
      {
        path: 'customers',
        loadComponent: () =>
          import('./components/shop-owner/customers/customers').then((m) => m.Customers),
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('./components/shop-owner/reports/reports').then((m) => m.Reports),
      },
      {
        path: 'invoice/:id',
        loadComponent: () =>
          import('./components/shop-owner/invoice/invoice').then((m) => m.Invoice),
      },
      {
        path: 'expenses',
        loadComponent: () =>
          import('./components/shop-owner/expense/expense').then((m) => m.Expense),
      },
      {
        path: 'staff-invite',
        loadComponent: () =>
          import('./components/shop-owner/staff-invite/staff-invite').then((m) => m.StaffInvite),
        canActivate: [authGuard],
        data: { roles: ['Owner'] },
      },
      {
        path: 'invoice',
        loadComponent: () =>
          import('./components/shop-owner/invoice/invoice').then((m) => m.Invoice),
      },
      {
        path: 'invoice-settings',
        loadComponent: () =>
          import('./components/shop-owner/invoice-settings/invoice-settings').then(
            (m) => m.InvoiceSettings,
          ),
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./components/shop-owner/settings/settings').then((m) => m.Settings),
      },
    ],
  },
  {
    path: 'admin-dashboard',
    loadComponent: () => import('./components/dashboard/dashboard').then((m) => m.Dashboard),
    canActivate: [authGuard],
    data: { roles: ['SuperAdmin'] },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./components/admin/admin-dashboard/admin-dashboard').then(
            (m) => m.AdminDashboard,
          ),
      },
      {
        path: 'users',
        loadComponent: () =>
          import('./components/admin/businesses/businesses').then((m) => m.Businesses),
      },
      {
        path: 'subscriptions',
        loadComponent: () =>
          import('./components/admin/subscriptions/subscriptions').then((m) => m.Subscriptions),
      },
      {
        path: 'analytics',
        loadComponent: () =>
          import('./components/admin/analytics/analytics').then((m) => m.Analytics),
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./components/admin/admin-settings/admin-settings').then((m) => m.AdminSettings),
      },
      {
        path: 'notifications',
        loadComponent: () =>
          import('./shared/components/notifications/notifications').then((m) => m.Notifications),
      },
    ],
  },
  {
    path: '**',
    redirectTo: 'login',
  },
];
