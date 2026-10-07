import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthService } from '../../core/services/auth.service';
import { PortalLinkService } from '../../core/services/portal-link.service';
import { StateMessageComponent } from '../../shared/components/state-message/state-message';

/** Trazos de los íconos de Bootstrap Icons (16×16) que usa la página. */
const ICONS = {
  external: [
    'M8.636 3.5a.5.5 0 0 0-.5-.5H1.5A1.5 1.5 0 0 0 0 4.5v10A1.5 1.5 0 0 0 1.5 16h10a1.5 1.5 0 0 0 1.5-1.5V7.864a.5.5 0 0 0-1 0V14.5a.5.5 0 0 1-.5.5h-10a.5.5 0 0 1-.5-.5v-10a.5.5 0 0 1 .5-.5h6.636a.5.5 0 0 0 .5-.5',
    'M16 .5a.5.5 0 0 0-.5-.5h-5a.5.5 0 0 0 0 1h3.793L6.146 9.146a.5.5 0 1 0 .708.708L15 1.707V5.5a.5.5 0 0 0 1 0z',
  ],
  info: [
    'M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14m0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16',
    'm8.93 6.588-2.29.287-.082.38.45.083c.294.07.352.176.288.469l-.738 3.468c-.194.897.105 1.319.808 1.319.545 0 1.178-.252 1.465-.598l.088-.416c-.2.176-.492.246-.686.246-.275 0-.375-.193-.304-.533zM9 4.5a1 1 0 1 1-2 0 1 1 0 0 1 2 0',
  ],
  refund: [
    'M8 3a5 5 0 1 1-4.546 2.914.5.5 0 0 0-.908-.417A6 6 0 1 0 8 2z',
    'M8 4.466V.534a.25.25 0 0 0-.41-.192L5.23 2.308a.25.25 0 0 0 0 .384l2.36 1.966A.25.25 0 0 0 8 4.466',
  ],
  coin: [
    'M5.5 9.511c.076.954.83 1.697 2.182 1.785V12h.6v-.709c1.4-.098 2.218-.846 2.218-1.932 0-.987-.626-1.496-1.745-1.76l-.473-.112V5.57c.6.068.982.396 1.074.85h1.052c-.076-.919-.864-1.638-2.126-1.716V4h-.6v.719c-1.195.117-2.01.836-2.01 1.853 0 .9.606 1.472 1.613 1.707l.397.098v2.034c-.615-.093-1.022-.43-1.114-.9zm2.177-2.166c-.59-.137-.91-.416-.91-.836 0-.47.345-.822.915-.925v1.76h-.005zm.692 1.193c.717.166 1.048.435 1.048.91 0 .542-.412.914-1.135.982V8.518z',
  ],
  mail: [
    'M0 4a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2zm2-1a1 1 0 0 0-1 1v.217l7 4.2 7-4.2V4a1 1 0 0 0-1-1zm13 2.383-4.708 2.825L15 11.105zm-.034 6.876-5.64-3.471L8 9.583l-1.326-.795-5.64 3.47A1 1 0 0 0 2 13h12a1 1 0 0 0 .966-.741M1 11.105l4.708-2.897L1 5.383z',
  ],
  phone: [
    'M3.654 1.328a.678.678 0 0 0-1.015-.063L1.605 2.3c-.483.484-.661 1.169-.45 1.77a17.6 17.6 0 0 0 4.168 6.608 17.6 17.6 0 0 0 6.608 4.168c.601.211 1.286.033 1.77-.45l1.034-1.034a.678.678 0 0 0-.063-1.015l-2.307-1.794a.68.68 0 0 0-.58-.122l-2.19.547a1.75 1.75 0 0 1-1.657-.459L5.482 8.062a1.75 1.75 0 0 1-.46-1.657l.548-2.19a.68.68 0 0 0-.122-.58zM1.884.511a1.745 1.745 0 0 1 2.612.163L6.29 2.98c.329.423.445.974.315 1.494l-.547 2.19a.68.68 0 0 0 .178.643l2.457 2.457a.68.68 0 0 0 .644.178l2.189-.547a1.75 1.75 0 0 1 1.494.315l2.306 1.794c.829.645.905 1.87.163 2.611l-1.034 1.034c-.74.74-1.846 1.065-2.877.702a18.6 18.6 0 0 1-7.01-4.42 18.6 18.6 0 0 1-4.42-7.009c-.362-1.03-.037-2.137.703-2.877z',
  ],
  person: [
    'M8 8a3 3 0 1 0 0-6 3 3 0 0 0 0 6m2-3a2 2 0 1 1-4 0 2 2 0 0 1 4 0m4 8c0 1-1 1-1 1H3s-1 0-1-1 1-4 6-4 6 3 6 4m-1-.004c-.001-.246-.154-.986-.832-1.664C11.516 10.68 10.289 10 8 10s-3.516.68-4.168 1.332c-.678.678-.83 1.418-.832 1.664z',
  ],
} as const;

/**
 * Devoluciones de dinero. El portal de devoluciones es externo y se muestra incrustado (iframe con `sandbox`, sin
 * referer y con título accesible) para que el cliente no deje el sitio, con la opción de abrirlo en una ventana nueva:
 * algunos sitios no permiten mostrarse dentro de otro. Mientras el país no tenga la URL configurada se muestra un
 * estado "disponible pronto" con los canales de Atención a Clientes, sin marco vacío ni enlaces.
 */
@Component({
  selector: 'app-refunds',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe, StateMessageComponent],
  templateUrl: './refunds.html',
  styleUrl: './refunds.scss',
})
export class RefundsComponent {
  private readonly links = inject(PortalLinkService);
  private readonly auth = inject(AuthService);
  private readonly sanitizer = inject(DomSanitizer);

  readonly icons = ICONS;
  readonly state = this.links.externalLinksState;

  /** URL del portal (solo https absoluto); null si el país aún no la tiene configurada. */
  readonly url = computed(() => {
    const refunds = this.links.externalLinks()?.refunds;
    if (!refunds?.configured || !refunds.url) return null;
    try {
      return new URL(refunds.url).protocol === 'https:' ? refunds.url : null;
    } catch {
      return null;
    }
  });

  /** La URL viene de la configuración del portal (validada como https en el servidor y aquí). */
  readonly frameSrc = computed<SafeResourceUrl | null>(() => {
    const url = this.url();
    return url ? this.sanitizer.bypassSecurityTrustResourceUrl(url) : null;
  });

  /** País de los canales de contacto: el de la respuesta o, si no llegó, el de la sesión. */
  readonly country = computed(() => this.links.externalLinks()?.country ?? this.auth.getCountry());

  /** El marco terminó de cargar (oculta el aviso de carga). */
  readonly frameLoaded = signal(false);

  constructor() {
    effect(() => {
      if (this.state() === 'idle') untracked(() => this.links.loadExternalLinks());
    });
    // Un portal nuevo (otro país o sesión) vuelve a mostrar el aviso de carga.
    effect(() => {
      this.url();
      untracked(() => this.frameLoaded.set(false));
    });
  }

  onFrameLoad(): void {
    this.frameLoaded.set(true);
  }

  retry(): void {
    this.links.loadExternalLinks();
  }
}
