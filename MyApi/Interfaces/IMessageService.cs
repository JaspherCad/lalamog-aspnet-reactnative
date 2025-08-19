using MyApi.DTOs;

namespace MyApi.Interfaces
{
    public interface IMessageService
    {
        Task<MessageDto> SendMessageAsync(Guid senderId, Guid receiverId, string content, long MatchId);

        Task<List<MessageDto>> GetMessagesByMatchIdAsync(long matchId);
    }
}
