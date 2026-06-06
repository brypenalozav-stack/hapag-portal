namespace HapagPortal.Application.Common.Messaging;

using HapagPortal.Domain.Results;
using MediatR;

public interface ICommand : IRequest<Result>;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
