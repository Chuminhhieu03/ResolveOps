import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'utcToLocal',
  standalone: true
})
export class UtcToLocalPipe implements PipeTransform {
  transform(value: string | Date | null | undefined, format: 'short' | 'medium' | 'full' = 'medium'): string {
    if (!value) return '—';

    const date = typeof value === 'string' ? new Date(value) : value;
    if (isNaN(date.getTime())) return '—';

    switch (format) {
      case 'short':
        return date.toLocaleDateString(undefined, {
          month: 'short',
          day: 'numeric',
          hour: '2-digit',
          minute: '2-digit'
        });
      case 'full':
        return date.toLocaleString(undefined, {
          year: 'numeric',
          month: 'short',
          day: 'numeric',
          hour: '2-digit',
          minute: '2-digit',
          second: '2-digit',
          timeZoneName: 'short'
        });
      case 'medium':
      default:
        return date.toLocaleString(undefined, {
          year: 'numeric',
          month: 'short',
          day: 'numeric',
          hour: '2-digit',
          minute: '2-digit'
        });
    }
  }
}
