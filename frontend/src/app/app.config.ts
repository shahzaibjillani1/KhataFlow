import { ApplicationConfig, provideBrowserGlobalErrorListeners, isDevMode, provideAppInitializer, inject } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './core/interceptors/auth-interceptor';
import { langInterceptor } from './core/interceptors/lang-interceptor';
import { TranslocoHttpLoader } from './transloco-loader';
import { provideTransloco } from '@jsverse/transloco';
import { BusinessSubscriptionService } from './services/business-subscription-service';
import { AuthService } from './services/auth-service';
import { provideServiceWorker } from '@angular/service-worker';

function getSavedLang(): string {
  const saved = localStorage.getItem('lang');
  return saved === 'ur' ? 'ur' : 'en';
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(withInterceptors([authInterceptor, langInterceptor])),
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideTransloco({
      config: {
        availableLangs: ['en', 'ur'],
        defaultLang: getSavedLang(),
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: TranslocoHttpLoader,
    }),
    provideAppInitializer(() => {
      const subscriptionService = inject(BusinessSubscriptionService);
      const authService = inject(AuthService);

      if (authService.isLoggedIn()) {
        subscriptionService.ensureLoaded();
      }
    }),
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000',
    }),
  ],
};