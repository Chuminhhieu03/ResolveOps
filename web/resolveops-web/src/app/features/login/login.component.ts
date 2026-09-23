import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../core/services/auth.service';
import { TenantService } from '../../core/services/tenant.service';
import { DemoPreset } from '../../core/models/auth.models';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatDividerModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="login-wrapper" style="min-height: 100vh; background: radial-gradient(circle at top left, #1e293b 0%, #0f172a 100%); display: flex; align-items: center; justify-content: center; padding: 24px;">
      <div style="width: 100%; max-width: 480px;">
        <!-- Brand Header -->
        <div style="text-align: center; margin-bottom: 28px;">
          <div style="display: inline-flex; background: linear-gradient(135deg, #4f46e5 0%, #06b6d4 100%); width: 52px; height: 52px; border-radius: 14px; align-items: center; justify-content: center; box-shadow: 0 4px 20px rgba(79, 70, 229, 0.4); margin-bottom: 12px;">
            <mat-icon style="color: #ffffff; font-size: 28px; width: 28px; height: 28px;">hub</mat-icon>
          </div>
          <h1 style="color: #ffffff; font-size: 1.85rem; font-weight: 700; margin: 0; letter-spacing: -0.02em;">ResolveOps</h1>
          <p style="color: #94a3b8; font-size: 0.95rem; margin-top: 6px;">Logistics Exception &amp; Carrier Claims Platform</p>
        </div>

        <!-- Auth Card -->
        <div class="card-enterprise" style="background: #ffffff; border-radius: 16px; padding: 32px; box-shadow: 0 20px 40px rgba(0,0,0,0.25);">
          <div style="margin-bottom: 20px;">
            <h2 style="font-size: 1.3rem; font-weight: 600; margin: 0; color: #0f172a;">Sign in to your tenant</h2>
            <p style="font-size: 0.85rem; color: #64748b; margin-top: 4px;">Enter your credentials or choose a quick demo persona.</p>
          </div>

          <div *ngIf="errorMessage()" style="background: #fef2f2; border: 1px solid #fecaca; color: #991b1b; padding: 12px; border-radius: 8px; font-size: 0.85rem; margin-bottom: 20px; display: flex; align-items: center; gap: 8px;">
            <mat-icon style="font-size: 18px; width: 18px; height: 18px;">error</mat-icon>
            <span>{{ errorMessage() }}</span>
          </div>

          <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
            <!-- Tenant Selector -->
            <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 8px;">
              <mat-label>Active Organization / Tenant</mat-label>
              <mat-select formControlName="tenantId">
                <mat-option *ngFor="let t of tenantService.availableTenants()" [value]="t.id">
                  {{ t.name }} ({{ t.code }})
                </mat-option>
              </mat-select>
              <mat-icon matPrefix style="color: #64748b; margin-right: 8px;">business</mat-icon>
            </mat-form-field>

            <!-- Email -->
            <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 8px;">
              <mat-label>Email Address</mat-label>
              <input matInput type="email" formControlName="email" placeholder="name@resolveops.local" autocomplete="username">
              <mat-icon matPrefix style="color: #64748b; margin-right: 8px;">email</mat-icon>
            </mat-form-field>

            <!-- Password -->
            <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 16px;">
              <mat-label>Password</mat-label>
              <input matInput [type]="hidePassword() ? 'password' : 'text'" formControlName="password" autocomplete="current-password">
              <mat-icon matPrefix style="color: #64748b; margin-right: 8px;">lock</mat-icon>
              <button mat-icon-button matSuffix type="button" (click)="hidePassword.set(!hidePassword())">
                <mat-icon style="color: #94a3b8;">{{ hidePassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
              </button>
            </mat-form-field>

            <button mat-flat-button color="primary" type="submit" [disabled]="loginForm.invalid || isLoading()" style="width: 100%; height: 48px; font-size: 1rem; font-weight: 600; border-radius: 8px; background: #4f46e5;">
              <mat-spinner *ngIf="isLoading()" diameter="24" style="margin: 0 auto;"></mat-spinner>
              <span *ngIf="!isLoading()">Sign In</span>
            </button>
          </form>

          <mat-divider style="margin: 24px 0 20px 0;"></mat-divider>

          <!-- Quick Demo Presets -->
          <div>
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px;">
              <span style="font-size: 0.8rem; font-weight: 600; text-transform: uppercase; color: #64748b; letter-spacing: 0.04em;">Demo User Presets</span>
              <span style="font-size: 0.75rem; color: #94a3b8;">1-click login</span>
            </div>

            <div style="display: flex; flex-direction: column; gap: 8px;">
              <button *ngFor="let p of authService.demoPresets" type="button" (click)="fillPreset(p)" mat-stroked-button style="text-align: left; height: auto; padding: 8px 12px; display: flex; align-items: center; justify-content: space-between; border-radius: 8px;">
                <div style="display: flex; align-items: center; gap: 10px;">
                  <span style="display: inline-block; width: 10px; height: 10px; border-radius: 50%;" [style.background]="p.badgeColor"></span>
                  <div style="display: flex; flex-direction: column;">
                    <strong style="font-size: 0.85rem; color: #0f172a;">{{ p.name }}</strong>
                    <span style="font-size: 0.75rem; color: #64748b;">{{ p.email }}</span>
                  </div>
                </div>
                <mat-icon style="font-size: 18px; color: #94a3b8;">chevron_right</mat-icon>
              </button>
            </div>
          </div>
        </div>

        <!-- Spec Notice -->
        <div style="text-align: center; margin-top: 20px; font-size: 0.8rem; color: #64748b;">
          ResolveOps v1.0 • Master Spec §24 Phase 15 • Tenant Isolation Enforced
        </div>
      </div>
    </div>
  `
})
export class LoginComponent implements OnInit {
  loginForm: FormGroup;
  isLoading = signal<boolean>(false);
  hidePassword = signal<boolean>(true);
  errorMessage = signal<string | null>(null);

  constructor(
    private fb: FormBuilder,
    public authService: AuthService,
    public tenantService: TenantService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      tenantId: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
      return;
    }

    this.tenantService.fetchTenants().subscribe({
      next: res => {
        if (res.tenants.length > 0) {
          this.loginForm.patchValue({ tenantId: res.tenants[0].id });
        }
      }
    });
  }

  fillPreset(preset: DemoPreset): void {
    this.loginForm.patchValue({
      email: preset.email,
      password: 'DevPassword123!'
    });
    this.onSubmit();
  }

  onSubmit(): void {
    if (this.loginForm.invalid) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { email, password, tenantId } = this.loginForm.value;

    this.authService.login(email, password, tenantId).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: err => {
        this.isLoading.set(false);
        this.errorMessage.set(err?.error?.detail || err?.error?.title || 'Invalid credentials or tenant membership.');
      }
    });
  }
}
