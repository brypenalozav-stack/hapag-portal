import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { DepositProofUpload, PaymentDepositProofs } from '../models/deposit-proof.model';

const BASE = API_ENDPOINTS.PAYMENTS;

/**
 * Comprobantes de depósito de un pago con boleta (Fase 2, Ola H, M5-06): los ve el pagador, el mandante y Finanzas;
 * los adjunta la organización pagadora con un perfil que opera. Adjuntarlo a un pago pendiente emite la boleta (M5-02).
 * La verificación y el rechazo de Finanzas están en PaymentConfigService.
 */
@Injectable({ providedIn: 'root' })
export class DepositProofService {
  private readonly api = inject(ApiService);

  get(paymentId: string): Observable<PaymentDepositProofs> {
    return this.api.get<PaymentDepositProofs>(`${BASE}/${paymentId}/deposit-proofs`);
  }

  /** PDF, PNG o JPEG de hasta 10 MB, con los datos del depósito que el cliente quiera informar. */
  upload(paymentId: string, data: DepositProofUpload): Observable<PaymentDepositProofs> {
    const body = new FormData();
    body.append('file', data.file, data.file.name);
    const optional: [string, string | undefined][] = [
      ['bankName', data.bankName],
      ['bankReference', data.bankReference],
      ['depositDate', data.depositDate],
      ['depositAmount', data.depositAmount],
      ['notes', data.notes],
    ];
    for (const [key, value] of optional) {
      if (value && value.trim()) body.append(key, value.trim());
    }
    return this.api.post<PaymentDepositProofs>(`${BASE}/${paymentId}/deposit-proofs`, body);
  }

  download(paymentId: string, proofId: string): Observable<Blob> {
    return this.api.getBlob(`${BASE}/${paymentId}/deposit-proofs/${proofId}/file`);
  }
}
