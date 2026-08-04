import {
  ChangeDetectionStrategy,
  Component,
  computed,
  Directive,
  ElementRef,
  HostListener,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { LanguageService } from '../../services/language-service';
import { RouterLink, RouterOutlet } from '@angular/router';

interface FaqItem {
  questionKey: string;
  answerKey: string;
}

interface ModuleCard {
  icon: 'grid' | 'cart' | 'box' | 'users' | 'receipt' | 'chart';
  titleKey: string;
  descriptionKey: string;
  statKey: string;
}

@Directive({
  selector: '[appRevealOnScroll]',
  standalone: true,
  host: {
    class: 'reveal-on-scroll',
    '[class.in-view]': 'visible()',
  },
})
export class RevealOnScrollDirective {
  private readonly el = inject(ElementRef<HTMLElement>);
  readonly visible = signal(false);

  private observer = new IntersectionObserver(
    (entries) => {
      for (const entry of entries) {
        if (entry.isIntersecting) {
          this.visible.set(true);
          this.observer.disconnect();
        }
      }
    },
    { threshold: 0.15, rootMargin: '0px 0px -40px 0px' },
  );

  constructor() {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      this.visible.set(true);
      return;
    }
    this.observer.observe(this.el.nativeElement);
  }
}

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [CommonModule, TranslocoDirective, RouterLink, RouterOutlet, RevealOnScrollDirective],
  templateUrl: './landing-page.html',
  styleUrl: './landing-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[attr.dir]': 'dir()',
  },
})
export class LandingPage {
  private readonly translocoService = inject(TranslocoService);
  private readonly languageService = inject(LanguageService);

  readonly lang = this.languageService.currentLang;
  readonly dir = computed(() => (this.lang() === 'ur' ? 'rtl' : 'ltr'));

  readonly mobileMenuOpen = signal(false);
  readonly openFaqIndex = signal<number | null>(0);

  readonly scrolled = signal(false);

  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.scrolled.set(window.scrollY > 8);
  }

  readonly modules: ModuleCard[] = [
    {
      icon: 'grid',
      titleKey: 'landing.modules.dashboard.title',
      descriptionKey: 'landing.modules.dashboard.description',
      statKey: 'landing.modules.dashboard.stat',
    },
    {
      icon: 'cart',
      titleKey: 'landing.modules.sales.title',
      descriptionKey: 'landing.modules.sales.description',
      statKey: 'landing.modules.sales.stat',
    },
    {
      icon: 'box',
      titleKey: 'landing.modules.products.title',
      descriptionKey: 'landing.modules.products.description',
      statKey: 'landing.modules.products.stat',
    },
    {
      icon: 'users',
      titleKey: 'landing.modules.customers.title',
      descriptionKey: 'landing.modules.customers.description',
      statKey: 'landing.modules.customers.stat',
    },
    {
      icon: 'receipt',
      titleKey: 'landing.modules.expenses.title',
      descriptionKey: 'landing.modules.expenses.description',
      statKey: 'landing.modules.expenses.stat',
    },
    {
      icon: 'chart',
      titleKey: 'landing.modules.reports.title',
      descriptionKey: 'landing.modules.reports.description',
      statKey: 'landing.modules.reports.stat',
    },
  ];

  readonly faqs: FaqItem[] = [
    { questionKey: 'landing.faq.offline.question', answerKey: 'landing.faq.offline.answer' },
    { questionKey: 'landing.faq.invite.question', answerKey: 'landing.faq.invite.answer' },
    { questionKey: 'landing.faq.udhar.question', answerKey: 'landing.faq.udhar.answer' },
    { questionKey: 'landing.faq.share.question', answerKey: 'landing.faq.share.answer' },
    { questionKey: 'landing.faq.urdu.question', answerKey: 'landing.faq.urdu.answer' },
    { questionKey: 'landing.faq.pricing.question', answerKey: 'landing.faq.pricing.answer' },
  ];

  toggleFaq(index: number): void {
    this.openFaqIndex.set(this.openFaqIndex() === index ? null : index);
  }

  onLanguageChange(lang: string): void {
    localStorage.setItem('lang', lang);
    this.translocoService.setActiveLang(lang);
  }

  toggleLang(): void {
    this.onLanguageChange(this.lang() === 'en' ? 'ur' : 'en');
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen.set(!this.mobileMenuOpen());
  }
}
