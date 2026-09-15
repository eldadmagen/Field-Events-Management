import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export function roleGuard(requiredRole: 'Dispatcher' | 'Technician'): CanMatchFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (auth.isLoggedIn() && auth.role() === requiredRole) return true;

    router.navigate(['/login']);
    return false;
  };
}
