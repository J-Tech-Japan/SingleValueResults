using System.Text.Json.Serialization;
namespace ResultBoxes;

public record ResultBox<TValue>(TValue? Value, Exception? Exception) where TValue : notnull
{

    [JsonIgnore]
    public bool IsSuccess => Exception is null;
    public static ResultBox<TValue> OutOfRange => new(new ResultValueNullException());
    public static ResultBox<TValue> FromValue(TValue value) => new(value, null);
    public static ResultBox<TValue> FromValue(Func<TValue> value) => new(value(), null);
    public static async Task<ResultBox<TValue>> FromValueAsync(Func<Task<TValue>> value) =>
        new(await value(), null);
    public static ResultBox<TValue> FromException(Exception exception) =>
        new(default, exception);
    public Exception GetException() =>
        Exception ?? throw new ResultsInvalidOperationException("no exception");
    public TValue GetValue() =>
        (IsSuccess ? Value : throw new ResultsInvalidOperationException("no value")) ??
        throw new ResultsInvalidOperationException();

    public ResultBox<TValueResult> Handle<TValueResult>(Func<TValue, TValueResult> valueFunc)
        where TValueResult : notnull =>
        this switch
        {
            { Exception: { } error } => error,
            { Value: { } value } => valueFunc(value),
            _ => new ResultValueNullException()
        };
    public ResultBox<TValueResult> Handle<TValueResult>(
        Func<TValue, ResultBox<TValueResult>> valueFunc) where TValueResult : notnull =>
        this switch
        {
            { Exception: { } error } => error,
            { Value: { } value } => valueFunc(value),
            _ => new ResultValueNullException()
        };
    public async Task<ResultBox<TValueResult>> HandleAsync<TValueResult>(
        Func<TValue, Task<ResultBox<TValueResult>>> valueFunc) where TValueResult : notnull =>
        this switch
        {
            { Exception: { } error } => error,
            { Value: { } value } => await valueFunc(value),
            _ => new ResultValueNullException()
        };
    public async Task<ResultBox<TValueResult>> HandleResultAsync<TValueResult>(
        Func<ResultBox<TValue>, Task<ResultBox<TValueResult>>> valueFunc)
        where TValueResult : notnull =>
        this switch
        {
            { Exception: { } error } => error,
            { Value: not null } value => await valueFunc(value),
            _ => new ResultValueNullException()
        };
    public async Task<ResultBox<TValueResult>> HandleAsync<TValueResult>(
        Func<TValue, Task<TValueResult>> valueFunc) where TValueResult : notnull =>
        this switch
        {
            { Exception: { } error } => error,
            { Value: { } value } => await valueFunc(value),
            _ => new ResultValueNullException()
        };

    public static implicit operator ResultBox<TValue>(TValue value) => new(value, null);
    public static implicit operator ResultBox<TValue>(Exception exception) =>
        new(default, exception);

    public ResultBox<TwoValues<TValue, TValue2>> Append<TValue2>(TValue2 value)
        where TValue2 : notnull =>
        this switch
        {
            { Exception: { } error } => error,
            { Value: { } addingValue } => new ResultBox<TwoValues<TValue, TValue2>>(
                new TwoValues<TValue, TValue2>(Value, value),
                null),
            _ => new ResultValueNullException()
        };

    public static ResultBox<TValue> WrapTry(Func<TValue> func)
    {
        try
        {
            return func();
        }
        catch (Exception e)
        {
            return e;
        }
    }

    public static ResultBox<UnitValue> WrapTry(Action action)
    {
        try
        {
            action();
            return new UnitValue();
        }
        catch (Exception e)
        {
            return e;
        }
    }

    public static async Task<ResultBox<TValue>> WrapTry(Func<Task<TValue>> func)
    {
        try
        {
            return await func();
        }
        catch (Exception e)
        {
            return e;
        }
    }
}
public static class ResultBoxTaskHandleExtensions
{
    public static async Task<ResultBox<TValueResult>> Handle<TValue, TValueResult>(
        this Task<ResultBox<TValue>> task,
        Func<TValue, TValueResult> valueFunc) where TValue : notnull where TValueResult : notnull =>
        (await task).Handle(valueFunc);

    public static async Task<ResultBox<TValueResult>> Handle<TValue, TValueResult>(
        this Task<ResultBox<TValue>> task,
        Func<TValue, ResultBox<TValueResult>> valueFunc)
        where TValue : notnull where TValueResult : notnull =>
        (await task).Handle(valueFunc);

    public static async Task<ResultBox<TValueResult>> HandleAsync<TValue, TValueResult>(
        this Task<ResultBox<TValue>> task,
        Func<TValue, Task<ResultBox<TValueResult>>> valueFunc)
        where TValue : notnull where TValueResult : notnull =>
        await (await task).HandleAsync(valueFunc);
    public static async Task<ResultBox<TValueResult>> HandleResultAsync<TValue, TValueResult>(
        this Task<ResultBox<TValue>> task,
        Func<ResultBox<TValue>, Task<ResultBox<TValueResult>>> valueFunc) where TValue : notnull
        where TValueResult : notnull => await (await task).HandleResultAsync(valueFunc);
    public static async Task<ResultBox<TValueResult>> HandleAsync<TValue, TValueResult>(
        this Task<ResultBox<TValue>> task,
        Func<TValue, Task<TValueResult>> valueFunc)
        where TValue : notnull where TValueResult : notnull =>
        await (await task).HandleAsync(valueFunc);
}