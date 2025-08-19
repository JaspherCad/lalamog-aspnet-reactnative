using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using MyApi.DTOs;
using MyApi.Services;
using MyApi.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;




// ✅✅SUMMARY OF WEBSOCKET✅✅

// 1 create the HUB as central logic, similar to BaseController

// 2 then after boiler plate codes + overriding we implement our custom logic such as;
//          OnConnectedAsync, OnDisconnectedAsync, SendMessage

// 3 FRONTEND PART

// 4 BUILD SOMETHING LIKE AxiosInstance, but since we are using SignalR, we need to create a custom hook or service to manage the connection and messaging.... or be specific the SignalRService.ts


// 4.a: in SignalRService we built the HubConnectionBuilder with url settings and other config
// 4.b: onReceiveMessage and onSendMessage is inside that SignalRService. 
// The critical realization: All messages for the user come through one connection






// FOR MORE INFO VISIT Frontned's SignalRService and MessageContent. 👈🏽👈🏻👈🏻👈🏻
//🌸🌸🌸🌸
// Typical data flow

// Component => MessageContext.sendMessage => SignalRService.send => server => broadcast
// Server => SignalRService.on('message') => MessageContext updates state => Components re-render


// The Message Flow Pattern
// How messages actually travel:

// 1 User A sends message → Your frontend calls sendMessage(receiverId, content, matchId)
// 2 This invokes the SendMessage method on your hub
// 3 Hub saves to database via SendMessageAsync
// 4 Hub sends to:
//      Receiver's group (so User B gets it)
//      Sender's connection (so User A sees their message immediately)
// 5 Both clients receive the message in their ReceiveMessage handler
// 6 Your filtering logic determines if it's relevant for the current screen
//🌸🌸🌸🌸

namespace MyApi.Controllers
{
    public class MessageHub : Hub
    {
        private readonly IMessageService _messageService;
        private readonly IUserService _userService;

        public MessageHub(IMessageService messageService, IUserService userService)
        {
            _messageService = messageService;
            _userService = userService;
        }


        // ON CONNECT; initially we add the user to a group based on their ID
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                // await Clients.All.SendAsync("ReceiveMessage", "SYSTEM", $"{userId} connected"); //debig

            }

            // am I replacing the entire BUILT IN CODE?(OnConnectedAsync) -> No, I'm extending it.
            //  because of this base.OnConnectedAsync(); --> without this we entirely replace the code kasi OVERRIDE ito
            // nagdagdag lang ako sa paunang code.
            await base.OnConnectedAsync();
        }


        // to send message using frontned this is what WE invoke: 
        // REACT NATIVE CODE
        // ... useEffect then 
        //       const hubConnection = new SignalR.HubConnectionBuilder()
        //           .withUrl("/hubs/messagehub")
        //           .build();
        //       setConnection(hubConnection);

        //         await connection.invoke("SendMessage", input); 

        // **^^ Are frontend code

        // since this is EXTENDED to HUB, maybe that's how it works: idk tbh
        public async Task SendMessage(Guid receiverId, string content, long matchId)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(senderId) || !Guid.TryParse(senderId, out Guid senderGuid))
            {
                throw new HubException("Invalid sender ID");
            }

            try
            {
                // Save message to database
                var message = await _messageService.SendMessageAsync(
                    senderGuid,
                    receiverId,
                    content,
                    matchId
                ); //returns MessageDto


                //after saving to DB, share the output to websocket:



                Console.WriteLine($"Sending message to method: ReceiveMessage");

                // Send var message to group of receivers
                await Clients.Group(receiverId.ToString()).SendAsync("ReceiveMessage", message);

                // ECHOED: Also send to sender so they see their own message immediately
                // without this, delayed or chunky UI meaning my messages I send is not realtime to my end.
                await Clients.Caller.SendAsync("ReceiveMessage", message);
            }
            catch (Exception ex)
            {
                throw new HubException($"Failed to send message: {ex.Message}");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
            // await Clients.All.SendAsync("ReceiveMessage", "SYSTEM", $"{userId} disconnected");


            await base.OnDisconnectedAsync(exception);
        }
    }
}