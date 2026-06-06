namespace HapagPortal.Domain.Common;

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class AuditLoggablePropertyAttribute : Attribute;
