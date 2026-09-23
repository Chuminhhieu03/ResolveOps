import { Component, Input, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-sla-badge',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  template: `
    <span class="sla-badge" [ngClass]="badgeClass()">
      <mat-icon style="font-size: 14px; width: 14px; height: 14px; line-height: 14px;">{{ badgeIcon() }}</mat-icon>
      <span>{{ displayText() }}</span>
    </span>
  `
})
export class SlaBadgeComponent implements OnInit, OnDestroy {
  @Input() deadlineUtc?: string | null;
  @Input() status?: string | null; // 'Breached', 'Met', 'Paused', etc.

  badgeClass = signal<string>('sla-healthy');
  badgeIcon = signal<string>('schedule');
  displayText = signal<string>('—');

  private timerInterval?: any;

  ngOnInit(): void {
    this.updateClock();
    this.timerInterval = setInterval(() => this.updateClock(), 30000);
  }

  ngOnDestroy(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
    }
  }

  private updateClock(): void {
    if (this.status === 'Breached') {
      this.badgeClass.set('sla-breached');
      this.badgeIcon.set('error');
      this.displayText.set('SLA Breached');
      return;
    }

    if (this.status === 'Paused') {
      this.badgeClass.set('sla-paused');
      this.badgeIcon.set('pause');
      this.displayText.set('Paused');
      return;
    }

    if (this.status === 'Met' || this.status === 'Resolved' || this.status === 'Closed') {
      this.badgeClass.set('sla-healthy');
      this.badgeIcon.set('check_circle');
      this.displayText.set('SLA Met');
      return;
    }

    if (!this.deadlineUtc) {
      this.badgeClass.set('sla-healthy');
      this.badgeIcon.set('schedule');
      this.displayText.set('No SLA');
      return;
    }

    const deadline = new Date(this.deadlineUtc).getTime();
    const now = Date.now();
    const diffMs = deadline - now;

    if (diffMs <= 0) {
      this.badgeClass.set('sla-breached');
      this.badgeIcon.set('error');
      this.displayText.set('Breached');
      return;
    }

    const diffHours = diffMs / (1000 * 60 * 60);
    const hours = Math.floor(diffHours);
    const minutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));

    if (diffHours <= 2) {
      this.badgeClass.set('sla-at-risk');
      this.badgeIcon.set('warning');
      this.displayText.set(`At Risk (${hours}h ${minutes}m)`);
    } else {
      this.badgeClass.set('sla-healthy');
      this.badgeIcon.set('timer');
      this.displayText.set(`${hours}h ${minutes}m remaining`);
    }
  }
}
