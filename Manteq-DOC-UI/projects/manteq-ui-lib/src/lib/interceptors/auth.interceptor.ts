import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { USER_CONTEXT } from '../tokens/config.tokens';

/**
 * HTTP Interceptor to add user context headers to all requests
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const userContext = inject(USER_CONTEXT, { optional: true });
  
  if (userContext) {
    const clonedReq = req.clone({
      setHeaders: {
        'X-SME-UserId': userContext.email
      }
    });
    return next(clonedReq);
  }
  
  return next(req);
};
