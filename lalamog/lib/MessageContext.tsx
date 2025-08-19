// ⚠️⚠️⚠️AGAIN: Since subscribing to websocket is always STATEFUL (meaning we carry data throought the system UNLIKE typical program like service/controller/component when ONCE DONE or executed the data is not going to be persisted or saved or remembered)

// ⚠️⚠️⚠️Since websocket is stateful we need to architect this using Context API to share throughout the application








// SignalRService

// Purpose: Transport/client layer.
// What it does:
// Manages a single real-time connection (connect, reconnect, disconnect).
// Sends/receives raw events to/from your SignalR hub.
// Exposes low-level methods: start(), stop(), on(event, handler), off(), sendMessage(...).
// Characteristics:
// Singleton, side-effectful, framework-agnostic (doesn’t know React).
// No UI state; just networking and event wiring.


//📍 MessageContext

// Purpose: React state/coordination layer.
// What it does:
// Subscribes to SignalRService events and stores them in React state.
// Exposes high-level, UI-friendly state and actions via a custom hook: useMessages().
// messages, unreadCount, isConnected
// sendMessage(text), markAsRead(id), loadHistory(matchId)
// Characteristics:
// React-only, declarative, drives re-renders.
// Handles lifecycle (subscribe on mount, unsubscribe on unmount).


// Typical data flow

// Component => MessageContext.sendMessage => SignalRService.send => server => broadcast
// Server => SignalRService.on('message') => MessageContext updates state => Components re-render



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














import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { signalRService } from '@/api/signalRService';
import { getMessagesByMatchIdAPI, MessageData } from '@/api/axiosInstance';


interface MessageContextType {
    messages: MessageData[];
    sendMessage: (receiverId: string, content: string, matchId: number) => Promise<void>;
    loadMessages: (matchId: number) => Promise<void>;
    clearMessages: () => void;
}

const MessageContext = createContext<MessageContextType | undefined>(undefined);



export const MessageProvider = ({ children }: { children: ReactNode }) => {
    const [messages, setMessages] = useState<MessageData[]>([]);

    //listen to port channel
    useEffect(() => {

        // Start SignalR connection when component mounts
        signalRService.startConnection().catch(console.error);


        //#warning: THIS IS HOW WE RECIEVE REALTIME MSGS.

        const handleMessage = (message: MessageData) => {
            console.log('Received message:', message);
            setMessages(prev => [...prev, message]);
        };


        signalRService.onReceiveMessage(handleMessage);
        // we trigger the handleMessage() inside onReceiveMessage().... where onReceiveMessage is built in by library.


        return () => {
            // Clean up !"WHEN"! component unmounts
            signalRService.offReceiveMessage();
            signalRService.stopConnection().catch(console.error);
        };

    }, []);

    // THIS IS using "HUB" not BaseController. so axios calls are not releavant

    const sendMessage = async (receiverId: string, content: string, matchId: number) => {
        try {
            //public async Task SendMessage(Guid receiverId, string content, long matchId)
            await signalRService.sendMessage(receiverId, content, matchId);
        } catch (error) {
            console.error('Failed to send message:', error);
            throw error;
        }
    };

    const loadMessages = async (matchId: number) => {
        try {
            const response = await getMessagesByMatchIdAPI(matchId);
            setMessages(response.data);
        } catch (error) {
            console.error('Failed to load messages:', error);
            throw error;
        }
    };

    const clearMessages = () => {
        setMessages([]);
    };











    const value = {
        messages,
        sendMessage,
        loadMessages,
        clearMessages
    };

    return (
        <MessageContext.Provider value={value}>
            {children}
        </MessageContext.Provider>
    );
};

export const useMessages = () => {
    const context = useContext(MessageContext);
    if (context === undefined) {
        throw new Error('useMessages must be used within a MessageProvider');
    }
    return context;
};


