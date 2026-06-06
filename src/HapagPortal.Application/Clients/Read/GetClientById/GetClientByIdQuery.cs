namespace HapagPortal.Application.Clients.Read.GetClientById;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetClientByIdQuery(Guid Id) : IQuery<ClientResponseDto>;
