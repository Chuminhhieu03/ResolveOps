import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { ShellComponent } from './layout/shell/shell.component';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'exceptions',
        loadComponent: () => import('./features/exceptions/exception-list.component').then(m => m.ExceptionListComponent)
      },
      {
        path: 'exceptions/:id',
        loadComponent: () => import('./features/exceptions/exception-detail.component').then(m => m.ExceptionDetailComponent)
      },
      {
        path: 'exceptions/:id/evidence',
        loadComponent: () => import('./features/evidence/evidence-pipeline.component').then(m => m.EvidencePipelineComponent)
      },
      {
        path: 'shipments',
        loadComponent: () => import('./features/shipments/shipment-list.component').then(m => m.ShipmentListComponent)
      },
      {
        path: 'shipments/:id',
        loadComponent: () => import('./features/shipments/shipment-detail.component').then(m => m.ShipmentDetailComponent)
      },
      {
        path: 'tasks',
        loadComponent: () => import('./features/tasks/task-list.component').then(m => m.TaskListComponent)
      },
      {
        path: 'claims',
        loadComponent: () => import('./features/claims/claim-list.component').then(m => m.ClaimListComponent)
      },
      {
        path: 'claims/prepare',
        loadComponent: () => import('./features/claims/claim-prepare.component').then(m => m.ClaimPrepareComponent)
      },
      {
        path: 'claims/:id',
        loadComponent: () => import('./features/claims/claim-detail.component').then(m => m.ClaimDetailComponent)
      },
      {
        path: 'reports/carrier-scorecards',
        loadComponent: () => import('./features/reports/carrier-scorecards.component').then(m => m.CarrierScorecardsComponent)
      },
      {
        path: 'quarantine',
        loadComponent: () => import('./features/quarantine/quarantine-list.component').then(m => m.QuarantineListComponent)
      },
      {
        path: 'admin/policies',
        loadComponent: () => import('./features/admin/policy-admin.component').then(m => m.PolicyAdminComponent),
        canActivate: [roleGuard],
        data: { roles: ['OperationsManager', 'TenantAdmin', 'Admin'] }
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
