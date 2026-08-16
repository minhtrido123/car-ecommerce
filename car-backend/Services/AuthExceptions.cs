namespace Services;

public sealed class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException() : base("Email already registered") { }
}

public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Invalid email or password") { }
}

public sealed class InvalidRefreshTokenException : Exception
{
    public InvalidRefreshTokenException() : base("Invalid refresh token") { }
}
