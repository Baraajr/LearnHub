namespace LearnHub.Services.Exceptions;

public class UnauthorizedException(string message) : AppException(message, 401, "UNAUTHORIZED");
