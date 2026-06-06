namespace HapagPortal.Application.Clients.Read.GetMyClient;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetMyClientQuery() : IQuery<ClientResponseDto>;
