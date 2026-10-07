import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { Workspace, WorkspaceService } from '../../../core/services/workspace.service';
import { menuHasRoute } from '../main-nav/main-nav.config';

const LABELS: Record<Workspace, string> = {
  customer: 'shared.navbar.workspace.customer',
  backoffice: 'shared.navbar.workspace.backoffice',
};

/**
 * Selector de espacio de trabajo de los perfiles internos: "Portal clientes" o "Backoffice". Cambia el menú principal;
 * si la página actual no pertenece al espacio elegido, lleva al inicio. Solo se muestra a quien puede alternar.
 */
@Component({
  selector: 'app-workspace-switcher',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe],
  template: `
    @if (workspaces.canSwitch()) {
      <div class="hl-workspace" [class.hl-workspace--drawer]="variant() === 'drawer'" role="group"
           [attr.aria-label]="'shared.navbar.workspace.label' | transloco" data-testid="workspace-switcher">
        @for (w of options; track w) {
          <button type="button" class="hl-workspace__btn" [class.is-active]="workspaces.current() === w"
                  [attr.aria-pressed]="workspaces.current() === w" [attr.data-testid]="'workspace-' + w" (click)="select(w)">
            {{ labels[w] | transloco }}
          </button>
        }
      </div>
    }
  `,
  styles: [':host { display: contents; }'],
})
export class WorkspaceSwitcherComponent {
  readonly workspaces = inject(WorkspaceService);
  private readonly router = inject(Router);
  private readonly announcer = inject(LiveAnnouncerService);
  readonly options: readonly Workspace[] = ['customer', 'backoffice'];
  readonly labels = LABELS;

  /** `bar`: barra superior; `drawer`: panel lateral móvil. */
  readonly variant = input<'bar' | 'drawer'>('bar');

  select(workspace: Workspace): void {
    if (workspace === this.workspaces.current()) return;
    this.workspaces.set(workspace);
    this.announcer.announce(translate('shared.navbar.workspace.changed', { name: translate(LABELS[workspace]) }));
    if (!menuHasRoute(workspace, this.router.url)) this.router.navigateByUrl('/dashboard');
  }
}
