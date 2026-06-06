namespace HapagPortal.Application.Common.Messaging;

using HapagPortal.Domain.Results;
using MediatR;

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
