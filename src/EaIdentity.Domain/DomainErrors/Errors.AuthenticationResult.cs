using ErrorOr;

namespace EmbassyAirlines.Domain.DomainErrors;

public static partial class Errors
{
    public static class AuthenticationResult
    {
        public static Error UserConflict => Error.Conflict(
            code: "AuthenticationResult.Conflict",
            description: "User with this email address already exists");
        public static Error RegistrationFailed => Error.Validation(
            code: "AuthenticationResult.Validation",
            description: "Registration failed");
        public static Error MissingEmail => Error.Validation(
            code: "AuthenticationResult.Validation",
            description: "User does not have an email address");
        public static Error UserNotFound => Error.NotFound(
            code: "AuthenticationResult.NotFound",
            description: "User does not exist");
        public static Error WrongCredentials => Error.Unauthorized(
        code: "AuthenticationResult.Unauthorized",
        description: "User/password combination is wrong");
        public static Error InvalidToken => Error.Unauthorized(
        code: "AuthenticationResult.Unauthorized",
        description: "Invalid token");
        public static Error TokenMismatch => Error.Unauthorized(
        code: "AuthenticationResult.Unauthorized",
        description: "This refresh token does not match this JWT");
        public static Error UsedRefreshToken => Error.Unauthorized(
        code: "AuthenticationResult.Unauthorized",
        description: "This refresh token has been used");
        public static Error InvalidRefreshToken => Error.Unauthorized(
        code: "AuthenticationResult.Unauthorized",
        description: "This refresh token has been invalidated");
        public static Error ExpiredRefreshToken => Error.Unauthorized(
        code: "AuthenticationResult.Unauthorized",
        description: "This refresh token has expired");
        public static Error NonExistentRefreshToken => Error.Unauthorized(
        code: "AuthenticationResult.Unauthorized",
        description: "This refresh token does not exist");
        public static Error StillValid => Error.Validation(
        code: "AuthenticationResult.Validation",
        description: "This token hasn't expired yet");
    }
}