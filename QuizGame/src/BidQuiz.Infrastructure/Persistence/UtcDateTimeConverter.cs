using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BidQuiz.Infrastructure.Persistence;

/// <summary>Marks every DateTime read back from the database as UTC.</summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value,
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
