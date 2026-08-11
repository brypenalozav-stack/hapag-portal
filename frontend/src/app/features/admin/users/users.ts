import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AdminUserService } from '../../../core/services/admin-user.service';
import { AdminUser, RoleOption } from '../../../core/models/admin-user.model';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [FormsModule, ReactiveFormsModule, LoadingSpinnerComponent],
  templateUrl: './users.html',
  styles: [':host { display: block; }'],
})
export class UsersComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(AdminUserService);
  private readonly destroyRef = inject(DestroyRef);

  users = signal<AdminUser[]>([]);
  roles = signal<RoleOption[]>([]);
  total = signal(0);
  page = signal(1);
  pageSize = 10;
  loading = signal(false);
  error = signal('');

  // Filtros (pantalla "Búsqueda de usuarios")
  statusFilter = '';   // '' | 'true' | 'false'
  roleFilter = '';
  searchTerm = '';

  // Modal "Nuevo Usuario"
  showForm = signal(false);
  submitting = signal(false);
  formError = signal('');
  formSuccess = signal('');

  form = this.fb.nonNullable.group({
    displayId: [null as number | null],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    roleCode: ['', Validators.required],
  });

  ngOnInit(): void {
    this.service.getRoles().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (r) => this.roles.set(r),
      error: () => { /* roles no críticos para listar */ },
    });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.service.searchUsers({
      isActive: this.statusFilter === '' ? undefined : this.statusFilter === 'true',
      roleCode: this.roleFilter || undefined,
      search: this.searchTerm || undefined,
      page: this.page(),
      pageSize: this.pageSize,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.users.set(res.items);
        this.total.set(res.total);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Error al cargar los usuarios.');
        this.loading.set(false);
      },
    });
  }

  onFilterChange(): void {
    this.page.set(1);
    this.load();
  }

  openForm(): void {
    this.form.reset({ displayId: null, firstName: '', lastName: '', email: '', phone: '', roleCode: '' });
    this.formError.set('');
    this.formSuccess.set('');
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.formError.set('');

    const raw = this.form.getRawValue();
    this.service.createUser({
      firstName: raw.firstName,
      lastName: raw.lastName,
      email: raw.email,
      roleCode: raw.roleCode,
      phone: raw.phone || undefined,
      displayId: raw.displayId ?? undefined,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.submitting.set(false);
        this.formSuccess.set('Usuario creado exitosamente.');
        this.load();
        setTimeout(() => this.showForm.set(false), 1200);
      },
      error: (err) => {
        this.submitting.set(false);
        this.formError.set(err.error?.detail ?? err.error?.title ?? 'Error al crear el usuario.');
      },
    });
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.total() / this.pageSize));
  }

  changePage(delta: number): void {
    const next = this.page() + delta;
    if (next < 1 || next > this.totalPages) return;
    this.page.set(next);
    this.load();
  }
}
