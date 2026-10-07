namespace HapagPortal.WebApi.Controllers.V1;

using System.Text;
using Asp.Versioning;
using HapagPortal.Application.Config.Features;
using HapagPortal.Application.Payments.Commands.Cancel;
using HapagPortal.Application.Payments.Commands.Confirm;
using HapagPortal.Application.Payments.Commands.Webhooks;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Application.Payments.DepositProofs;
using HapagPortal.Application.Payments.Lifecycle;
using HapagPortal.Application.Payments.Read.GetById;
using HapagPortal.Application.Payments.Read.GetMyPayments;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
[Authorize]
public sealed class PaymentsController : ApiController
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetPaymentByIdQuery(id);
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyPayments(
        CancellationToken cancellationToken)
    {
        var query = new GetMyPaymentsQuery();
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    /// <summary>Estado único del pago con su historial de transiciones (NF-02, NF-12).</summary>
    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> GetStatus(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPaymentStatusQuery(id), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    /// <summary>Emite la boleta de depósito: desde aquí el cliente ya no puede anularla (M5-02).</summary>
    [HttpPost("{id:guid}/issue-slip")]
    public async Task<IActionResult> IssueSlip(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new IssueDepositSlipCommand(id), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    /// <summary>Comprobantes de depósito del pago con su revisión (M5-06).</summary>
    [HttpGet("{id:guid}/deposit-proofs")]
    [RequiresFeature(FeatureNames.DepositProofs)]
    public async Task<IActionResult> GetDepositProofs(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetDepositProofsQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>
    /// Adjunta el comprobante del depósito (multipart: <c>file</c> y, opcionales, <c>bankName</c>, <c>bankReference</c>,
    /// <c>depositDate</c>, <c>depositAmount</c>, <c>notes</c>). PDF, PNG o JPEG hasta 10 MB (M5-06).
    /// </summary>
    [HttpPost("{id:guid}/deposit-proofs")]
    [RequiresFeature(FeatureNames.DepositProofs)]
    [RequestSizeLimit(UploadDepositProofCommandValidator.MaxSizeBytes + 1024 * 1024)]
    public async Task<IActionResult> UploadDepositProof(
        Guid id,
        IFormFile file,
        [FromForm] string? bankName,
        [FromForm] string? bankReference,
        [FromForm] DateOnly? depositDate,
        [FromForm] decimal? depositAmount,
        [FromForm] string? notes,
        CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);

        var result = await Sender.Send(
            new UploadDepositProofCommand(id, file.FileName, file.ContentType, buffer.ToArray(), bankName, bankReference, depositDate,
                depositAmount, notes),
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}/deposit-proofs/{proofId:guid}/file")]
    [RequiresFeature(FeatureNames.DepositProofs)]
    public async Task<IActionResult> DownloadDepositProof(Guid id, Guid proofId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetDepositProofFileQuery(id, proofId), cancellationToken);
        return result.IsSuccess
            ? File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
            : HandleFailure(result);
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Confirm(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmPaymentCommand(id);
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new CancelPaymentCommand(id);
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    /// <summary>
    /// Vuelta del pagador desde la pasarela: consulta a la pasarela el estado del pago en curso y devuelve el estado
    /// (como <c>GET status</c>). La vuelta no confirma por sí sola: confirma solo lo que informa la pasarela.
    /// </summary>
    [HttpPost("{id:guid}/verify")]
    public async Task<IActionResult> Verify(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new VerifyPaymentCommand(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>
    /// Notificación de una pasarela: <c>khipu</c>, <c>getnet</c> (botón Santander), <c>bci</c> (Bci Pagos) o
    /// <c>banco-chile</c>. Se lee el cuerpo crudo sin model binding: la firma se verifica sobre esos bytes. Firma
    /// inválida → 401 sin cambios; pasarela que no responde → 503 (puede reintentar); el resto, 200 (con el texto que
    /// exija la pasarela).
    /// </summary>
    [HttpPost("webhook/{gateway}")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(string gateway, CancellationToken cancellationToken)
    {
        var providerKey = PaymentWebhookRoutes.ProviderFor(gateway);
        if (providerKey is null)
            return NotFound();

        var rawBody = await ReadRawBodyAsync(cancellationToken);
        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        var result = await Sender.Send(
            new PaymentNotificationCommand(providerKey, rawBody, headers, Request.ContentType), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code is "Integration.Unavailable" or "Integration.Timeout"
                ? StatusCode(StatusCodes.Status503ServiceUnavailable)
                : HandleFailure(result);
        }

        return result.Value.Body is { } body ? Content(body, "text/plain", Encoding.UTF8) : Ok();
    }

    /// <summary>
    /// Cuerpo crudo de la notificación, exactamente como llegó (UTF-8). Program.cs habilita el buffering en
    /// /api/v1/payments/webhook para poder releerlo.
    /// </summary>
    private async Task<string> ReadRawBodyAsync(CancellationToken cancellationToken)
    {
        if (Request.Body.CanSeek)
            Request.Body.Position = 0;

        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        if (Request.Body.CanSeek)
            Request.Body.Position = 0;

        return rawBody;
    }
}
