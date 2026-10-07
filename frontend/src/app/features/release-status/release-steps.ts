import { ReleaseStep } from '../../core/models/release-status.model';

/** Título de cada requisito de liberación (consulta BL y encabezado del detalle del BL). */
export const RELEASE_STEP_TITLE: Record<string, string> = {
  FREIGHT: 'releaseStatus.step.FREIGHT.title',
  LOCAL_CHARGES: 'releaseStatus.step.LOCAL_CHARGES.title',
  RESPONSIBILITY_LETTER: 'releaseStatus.step.RESPONSIBILITY_LETTER.title',
  DEMURRAGE: 'releaseStatus.step.DEMURRAGE.title',
  ADVANCE_DEMURRAGE: 'releaseStatus.step.ADVANCE_DEMURRAGE.title',
  NO_DEBT_CERTIFICATE: 'releaseStatus.step.NO_DEBT_CERTIFICATE.title',
  RELEASE_LETTER: 'releaseStatus.step.RELEASE_LETTER.title',
};

/** Qué falta en cada requisito pendiente, en palabras del cliente. */
export const RELEASE_STEP_PENDING: Record<string, string> = {
  FREIGHT: 'releaseStatus.step.FREIGHT.pending',
  LOCAL_CHARGES: 'releaseStatus.step.LOCAL_CHARGES.pending',
  RESPONSIBILITY_LETTER: 'releaseStatus.step.RESPONSIBILITY_LETTER.pending',
  DEMURRAGE: 'releaseStatus.step.DEMURRAGE.pending',
  ADVANCE_DEMURRAGE: 'releaseStatus.step.ADVANCE_DEMURRAGE.pending',
  NO_DEBT_CERTIFICATE: 'releaseStatus.step.NO_DEBT_CERTIFICATE.pending',
  RELEASE_LETTER: 'releaseStatus.step.RELEASE_LETTER.pending',
};

/** Texto del botón de cada acción. */
export const RELEASE_ACTION_KEYS: Record<string, string> = {
  PayFreight: 'releaseStatus.action.PayFreight',
  PayCharges: 'releaseStatus.action.PayCharges',
  IssueResponsibilityLetter: 'releaseStatus.action.IssueResponsibilityLetter',
  CalculateDemurrage: 'releaseStatus.action.CalculateDemurrage',
  PayDemurrage: 'releaseStatus.action.PayDemurrage',
  PayAdvanceDemurrage: 'releaseStatus.action.PayAdvanceDemurrage',
  RequestNoDebtCertificate: 'releaseStatus.action.RequestNoDebtCertificate',
  RequestReleaseLetter: 'releaseStatus.action.RequestReleaseLetter',
};

/** Ruta de la pantalla que resuelve el paso. */
export function releaseActionRoute(step: ReleaseStep, bl: string): string[] | null {
  switch (step.action) {
    case 'PayFreight':
      return ['/shipments', bl];
    case 'PayCharges':
      return ['/charges', bl];
    case 'IssueResponsibilityLetter':
    case 'RequestNoDebtCertificate':
      return ['/shipments', bl, 'documents'];
    case 'CalculateDemurrage':
    case 'PayDemurrage':
    case 'PayAdvanceDemurrage':
      return ['/demurrage', bl];
    case 'RequestReleaseLetter':
      return ['/shipments', bl, 'release-letter'];
    default:
      return null;
  }
}
