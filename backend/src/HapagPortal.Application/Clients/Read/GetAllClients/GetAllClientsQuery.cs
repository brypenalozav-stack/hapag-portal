namespace HapagPortal.Application.Clients.Read.GetAllClients;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetAllClientsQuery(string? Country) : IQuery<List<ClientResponseDto>>;
