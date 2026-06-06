namespace HapagPortal.Application.Payments.Commands.Webhooks;

using HapagPortal.Application.Common.Messaging;

public sealed record BancoChileWebhookCommand(
    string TransactionId,
    string ExternalReference,
    string Status,
    decimal? Amount) : ICommand;
