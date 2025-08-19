using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using MyApi.DTOs;
using MyApi.Models;
using MyApi.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite;
using NpgsqlTypes;
using MyApi.Interfaces;



namespace MyApi.Services
{
    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;

        public MessageService(ApplicationDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        //jane doe: f2603802-deb3-4377-8651-1134db7837a7
        //jaspher: 5c344aee-e3c1-4faf-846b-73cb28574d76
        //postman sample 
        // {
        //     "SwipeeId":"5c344aee-e3c1-4faf-846b-73cb28574d76",
        //     "Direction": "right"
        // }



        public async Task<MessageDto> SendMessageAsync(Guid senderId, Guid receiverId, string content, long matchId)
        {
            // Validate sender and receiver
            if (senderId == receiverId)
            {
                throw new ArgumentException("You cannot send a message to yourself.");
            }




            // Create a new message
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                MatchId = matchId,
                Read = false
            };

            // Add the message to the database
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Return the message DTO
            return new MessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Content = message.Content,
                CreatedAt = message.CreatedAt
            };
        }

        //fetch all message by matchId
        public async Task<List<MessageDto>> GetMessagesByMatchIdAsync(long matchId)
        {
            var messages = await _context.Messages
                .Where(m => m.MatchId == matchId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            return messages.Select(m => new MessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            }).ToList();
        }
    }


}