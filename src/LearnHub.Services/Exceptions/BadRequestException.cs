namespace LearnHub.Services.Exceptions;

public class BadRequestException(string message) : AppException(message, 400, "BAD_REQUEST");
