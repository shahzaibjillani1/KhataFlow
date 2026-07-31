import { HttpInterceptorFn } from '@angular/common/http';

export const langInterceptor: HttpInterceptorFn = (req, next) => {
  const lang = localStorage.getItem('lang') === 'ur' ? 'ur' : 'en';

  const cloned = req.clone({
    headers: req.headers.set('Accept-Language', lang),
  });

  return next(cloned);
};