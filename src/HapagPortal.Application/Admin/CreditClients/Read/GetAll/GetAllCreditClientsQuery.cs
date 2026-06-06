namespace HapagPortal.Application.Admin.CreditClients.Read.GetAll;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetAllCreditClientsQuery() : IQuery<List<CreditClientResponseDto>>;
