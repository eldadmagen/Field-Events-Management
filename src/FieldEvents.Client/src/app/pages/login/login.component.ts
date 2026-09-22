import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  userName = '';
  password = '';
  readonly error = signal<string | null>(null);

  constructor(private readonly auth: AuthService, private readonly router: Router) {}

  submit(): void {
    this.error.set(null);
    this.auth.login(this.userName, this.password).subscribe({
      next: (res) => {
        this.router.navigate([res.role === 'Dispatcher' ? '/dispatcher' : '/technician']);
      },
      error: () => this.error.set('Invalid username or password.')
    });
  }
}
