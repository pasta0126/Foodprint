namespace Foodprint.Core.Auth;

public enum ActivationError { None, InvalidLink, WeakPassword, InvalidName }

public enum SignInError { None, InvalidCredentials, RateLimited }

public enum PasswordChangeError { None, WrongCurrent, WeakPassword }

public sealed record ActivationResult(ActivationError Error, Guid UserId = default, string? SessionToken = null)
{
    public bool Ok => Error == ActivationError.None;
    public static ActivationResult Fail(ActivationError e) => new(e);
}

public sealed record SignInResult(SignInError Error, Guid UserId = default, string? SessionToken = null)
{
    public bool Ok => Error == SignInError.None;
    public static SignInResult Fail(SignInError e) => new(e);
}

public static class PasswordRules
{
    public const int MinLength = 10;
    public static bool IsAcceptable(string? password) => password is not null && password.Length >= MinLength;
}
