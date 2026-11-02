namespace Chat.Shared.Contracts;

public sealed record ChatMessage(
    long Id,
    string User,
    string Message,
    DateTimeOffset SentAt);