import { Component, computed, EventEmitter, inject, Input, Output } from '@angular/core';
import { SUPER_ADMIN, SHOP_OWNER, SidebarItem } from '../../../core/models/sidebarModel';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TranslocoDirective } from '@jsverse/transloco';
import { ProductService } from '../../../services/product-service';
import { PaginatedResponse } from '../../../core/models/paginated-response-model';
import { Product } from '../../../core/models/product-models';
import { AuthService } from '../../../services/auth-service';
import { LanguageService } from '../../../services/language-service';
import { TokenStorageService } from '../../../services/token-storage-service';
import { BusinessSubscriptionService } from '../../../services/business-subscription-service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, TranslocoDirective],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css',
})
export class Sidebar {
  @Input() collapsed = false;
  @Input() mobileOpen = false;
  @Output() tabSelected = new EventEmitter<string>();
  @Output() closeMobile = new EventEmitter<void>(); // NEW

  router = inject(Router);
  private productSrv = inject(ProductService);
  productCount = 0;
  private FETCH_ALL_PAGE_SIZE: number = 100;
  private authService = inject(AuthService);
  private languageService = inject(LanguageService);
  private tokenStorage = inject(TokenStorageService);
  private subscriptionService = inject(BusinessSubscriptionService);

  readonly lang = this.languageService.currentLang;
  readonly isPremium = this.subscriptionService.isPremium;

  private readonly role = this.authService.getRole();

  private readonly baseMenu: SidebarItem[] = (() => {
    const SIDEBAR_MAP: Record<string, SidebarItem[]> = {
      SuperAdmin: SUPER_ADMIN,
      Owner: SHOP_OWNER,
      Manager: SHOP_OWNER,
      Staff: SHOP_OWNER,
    };
    return SIDEBAR_MAP[this.role!] || SHOP_OWNER;
  })();

  readonly menuItems = computed(() =>
    this.baseMenu.filter((item) => {
      const roleOk = !item.roles || item.roles.includes(this.role as any);
      const premiumOk = !item.premiumOnly || this.isPremium();
      return roleOk && premiumOk;
    }),
  );

  ngOnInit() {
    this.getProductCount();
  }

  getProductCount() {
    this.productSrv.getPaged(1, this.FETCH_ALL_PAGE_SIZE).subscribe({
      next: (res: { data: PaginatedResponse<Product> }) => {
        this.productCount = res.data.totalCount;
      },
      error: (err) => console.error('Failed to load products count', err),
    });
  }

  onTabSelect(item: SidebarItem) {
    this.tabSelected.emit(item.labelKey);

    // Close the mobile drawer after navigating, since we're in the
    // narrow (< sm) viewport where the sidebar overlays content.
    if (this.mobileOpen) {
      this.closeMobile.emit();
    }
  }

  logout() {
    this.tokenStorage.clear();

    this.authService.logout().subscribe({
      next: () => {
        this.subscriptionService.refresh();
        this.router.navigate(['/login']);
      },
      error: () => {
        this.subscriptionService.refresh();
        this.router.navigate(['/login']);
      },
    });
  }
}