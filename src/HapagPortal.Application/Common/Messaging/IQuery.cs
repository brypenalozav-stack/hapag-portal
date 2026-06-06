namespace HapagPortal.Application.Common.Messaging;

using HapagPortal.Domain.Results;
using MediatR;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
