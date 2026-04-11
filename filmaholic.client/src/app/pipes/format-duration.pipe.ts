import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'formatDuration' })
export class FormatDurationPipe implements PipeTransform {
  /**
   * @param treatZeroAsUnknown quando true, null/0/inválido não mostra texto (duração ainda não divulgada pelo TMDB).
   */
  transform(
    value: number | null | undefined,
    unit: 'min' | 'hours' = 'min',
    treatZeroAsUnknown = false,
    showDays = true,
    compact = false
  ): string {
    if (value == null || value < 0 || isNaN(value)) {
      return treatZeroAsUnknown ? '' : (compact ? '0m' : '0 min');
    }
    let totalMin = unit === 'hours' ? Math.round(value * 60) : Math.round(value);
    if (totalMin === 0) {
      return treatZeroAsUnknown ? '' : (compact ? '0m' : '0 min');
    }

    const min = totalMin % 60;
    const totalHours = Math.floor(totalMin / 60);
    const hours = totalHours % 24;
    const totalDays = Math.floor(totalHours / 24);
    const days = totalDays % 30;
    const months = Math.floor(totalDays / 30);

    const parts: string[] = [];
    const minUnit = compact ? 'm' : ' min';
    const hourUnit = 'h'; 

    if (showDays && months > 0) {
      parts.push(months === 1 ? '1 mês' : `${months} meses`);
      if (days > 0) parts.push(days === 1 ? '1 dia' : `${days} dias`);
    } else if (showDays && totalDays > 0) {
      parts.push(totalDays === 1 ? '1 dia' : `${totalDays} dias`);
      if (hours > 0 || min > 0) {
        const hPart = hours > 0 ? `${hours}${hourUnit}` : '';
        const mPart = min > 0 ? `${min}${minUnit}` : '';
        if (hPart) parts.push(hPart);
        if (mPart) parts.push(mPart);
      }
    } else if (totalHours > 0) {
      parts.push(`${totalHours}${hourUnit}`);
      if (min > 0) parts.push(`${min}${minUnit}`);
    } else {
      return `${min}${minUnit}`;
    }

    return parts.join(' ');
  }
}
