namespace AgroControl.Application.Common;

public enum OperationErrorKind
{
    None = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Forbidden = 4
}

public sealed record OperationResult<T>(
    bool Succeeded,
    T? Value,
    OperationErrorKind ErrorKind,
    string? Error)
{
    public static OperationResult<T> Success(T value) => new(true, value, OperationErrorKind.None, null);
    public static OperationResult<T> Validation(string error) => new(false, default, OperationErrorKind.Validation, error);
    public static OperationResult<T> NotFound(string error) => new(false, default, OperationErrorKind.NotFound, error);
    public static OperationResult<T> Conflict(string error) => new(false, default, OperationErrorKind.Conflict, error);
    public static OperationResult<T> Forbidden(string error) => new(false, default, OperationErrorKind.Forbidden, error);
}
