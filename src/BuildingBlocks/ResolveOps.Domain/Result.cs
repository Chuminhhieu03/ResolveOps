using System.Diagnostics.CodeAnalysis;

namespace ResolveOps.Domain;

/// <summary>
/// A result monad used to return either a success value or a domain error.
/// Used at the Application boundary instead of throwing exceptions for expected business rule violations.
/// </summary>
public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly DomainError? _error;

    public bool IsSuccess { get; }
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsFailure => !IsSuccess;

    public DomainError Error => _error ?? throw new InvalidOperationException("Result is successful, no error available.");
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException($"Result is failure, cannot access value. Error: {Error.Code}");

    private Result(T value)
    {
        IsSuccess = true;
        _value = value;
        _error = null;
    }

    private Result(DomainError error)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

#pragma warning disable CA1000 // Do not declare static members on generic types
    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(DomainError error) => new(error);
#pragma warning restore CA1000

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(DomainError error) => Failure(error);

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<DomainError, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }
}

public readonly struct Result
{
    private readonly DomainError? _error;

    public bool IsSuccess { get; }
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsFailure => !IsSuccess;

    public DomainError Error => _error ?? throw new InvalidOperationException("Result is successful, no error available.");

    private Result(bool isSuccess, DomainError? error)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(DomainError error) => new(false, error);

    public static implicit operator Result(DomainError error) => Failure(error);

    public TResult Match<TResult>(
        Func<TResult> onSuccess,
        Func<DomainError, TResult> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Error);
    }
}
