namespace HapagPortal.Application.Payments.Commands.Webhooks;

using HapagPortal.Application.Common.Messaging;

public sealed record KhipuWebhookCommand(
    string NotificationToken,
    string ExternalReference,
    string Status) : ICommand;
