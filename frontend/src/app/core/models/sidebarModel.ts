import { UserRole } from '../enums/user-role';

export interface SidebarItem {
  labelKey: string;
  route: string;
  icon: string;
  badge?: number | null;
  roles?: UserRole[];
  premiumOnly?: boolean;
}

export const SHOP_OWNER: SidebarItem[] = [
  { labelKey: 'sidebar.dashboard', route: '/shop-owner-dashboard', icon: 'fa-solid fa-house' },
  {
    labelKey: 'sidebar.sales',
    route: '/shop-owner-dashboard/sales',
    icon: 'fa-solid fa-cash-register',
  },
  {
    labelKey: 'sidebar.products',
    route: '/shop-owner-dashboard/products',
    icon: 'fa-solid fa-box',
    badge: 7,
  },
  {
    labelKey: 'sidebar.customers',
    route: '/shop-owner-dashboard/customers',
    icon: 'fa-solid fa-users',
  },
  {
    labelKey: 'sidebar.expenses',
    route: '/shop-owner-dashboard/expenses',
    icon: 'fa-solid fa-coins',
    roles: ['Owner', 'Manager'],
  },
  {
    labelKey: 'sidebar.reports',
    route: '/shop-owner-dashboard/reports',
    icon: 'fa-solid fa-chart-line',
    roles: ['Owner', 'Manager'],
  },
  {
    labelKey: 'sidebar.staffInvite',
    route: '/shop-owner-dashboard/staff-invite',
    icon: 'fa-solid fa-user-plus',
    roles: ['Owner'],
  },
  {
    labelKey: 'sidebar.invoiceSettings',
    route: '/shop-owner-dashboard/invoice-settings',
    icon: 'fa-solid fa-file-invoice-dollar',
    roles: ['Owner'],
    premiumOnly: true,
  },
  {
    labelKey: 'sidebar.settings',
    route: '/shop-owner-dashboard/settings',
    icon: 'fa-solid fa-gear',
  },
];

export const SUPER_ADMIN: SidebarItem[] = [
  { labelKey: 'sidebar.dashboard', icon: 'fa-solid fa-house', route: '/admin-dashboard' },
  {
    labelKey: 'sidebar.usersAndBusinesses',
    icon: 'fa-solid fa-users',
    route: '/admin-dashboard/users',
  },
  {
    labelKey: 'sidebar.subscriptions',
    icon: 'fa-solid fa-layer-group',
    route: '/admin-dashboard/subscriptions',
  },
  {
    labelKey: 'sidebar.analytics',
    icon: 'fa-solid fa-chart-line',
    route: '/admin-dashboard/analytics',
  },
  {
    labelKey: 'sidebar.systemSettings',
    icon: 'fa-solid fa-gear',
    route: '/admin-dashboard/settings',
  },
];
