namespace TaskManager.Models
{
    // Только то, что клиент может прислать при создании
    public record TaskCreateDto(string Title, string? Status);

    // Для частичного обновления: любое поле опционально
    public record TaskUpdateDto(string? Title, string? Status);
   
    public record TaskReadDto(int Id, string? Title, string? Status, DateTimeOffset CreatedAt);
}
