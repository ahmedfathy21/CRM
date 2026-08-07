using System.Reflection;
using CRM.Common.Wrappers;
using FluentValidation;
using MediatR;

namespace CRM.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var failures = _validators
            .Select(v => v.Validate(request))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var message = string.Join(" | ", failures.Select(f => f.ErrorMessage));
        return ConstructFailure<TResponse>(Error.Validation(message));
    }

    private static TResponse ConstructFailure<T>(Error error)
    {
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = typeof(T).GetGenericArguments()[0];
            var method = typeof(Result).GetMethod(nameof(Result.Failure), BindingFlags.Public | BindingFlags.Static)!;
            return (TResponse)method.MakeGenericMethod(valueType).Invoke(null, [error])!;
        }

        return (TResponse)(object)Result.Failure(error);
    }
}
