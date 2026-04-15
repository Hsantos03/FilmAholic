import { Injectable } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { SessionTerminationService } from '../services/session-termination.service';

@Injectable()
export class SessionTerminationInterceptor implements HttpInterceptor {
  constructor(private readonly sessionTermination: SessionTerminationService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(req).pipe(
      catchError((err: unknown) => {
        if (err instanceof HttpErrorResponse && err.status === 403) {
          const body = err.error as { sessaoTerminadaMotivo?: string } | null;
          const motivo = body?.sessaoTerminadaMotivo;
          if (motivo === 'bloqueada' || motivo === 'eliminada') {
            this.sessionTermination.notify(motivo);
          }
        }
        return throwError(() => err);
      })
    );
  }
}
