import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { LocalizedTextPipe } from '../../shared/pipes/localized-text-pipe';
import { CustomerLedgerViewService } from '../../services/customer-ledger-view-service';
import { LanguageService } from '../../services/language-service';

@Component({
  selector: 'app-public-ledger-view',
  standalone: true,
  imports: [CommonModule, TranslocoDirective, LocalizedTextPipe],
  templateUrl: './public-ledger-view.html',
  styleUrl: './public-ledger-view.css',
})
export class PublicLedgerView implements OnInit {
  private route = inject(ActivatedRoute);
  private ledgerService = inject(CustomerLedgerViewService);
  private languageService = inject(LanguageService);
  private translocoService = inject(TranslocoService);

  readonly lang = this.languageService.currentLang;

  state = this.ledgerService.state;
  ledger = this.ledgerService.ledger;
  isLoading = this.ledgerService.isLoading;
  notFound = this.ledgerService.notFound;

  ngOnInit(): void {
    const token = this.route.snapshot.paramMap.get('token');
    if (!token) {
      return;
    }
    this.ledgerService.getByToken(token).subscribe();
  }

  
  switchLanguage(langCode: 'en' | 'ur'): void {
    if (this.lang() === langCode) return;
    this.translocoService.setActiveLang(langCode);
  }

  initials(name: string): string {
    return (name || '?').trim().charAt(0).toUpperCase();
  }
}