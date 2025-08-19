// singleton communication, ensuring a single connection per user
// so class based

// THIS IS using "HUB" not BaseController. so axios calls are not releavant



// #info📍 SignalRService

// Purpose: Transport/client layer.
// What it does:
// Manages a single real-time connection (connect, reconnect, disconnect).
// Sends/receives raw events to/from Our SignalR hub.
// Exposes low-level methods: start(), stop(), on(event, handler), off(), sendMessage(...).
// Characteristics:
// Singleton, side-effectful, framework-agnostic (doesn’t know React).
// No UI state; just networking and event wiring.
// #end-info:


// #info: MessageContext

// Purpose: React state/coordination layer.
// What it does:
// Subscribes to SignalRService events and stores them in React state.
// Exposes high-level, UI-friendly state and actions via a custom hook: useMessages().
// messages, unreadCount, isConnected
// sendMessage(text), markAsRead(id), loadHistory(matchId)
// Characteristics:
// React-only, declarative, drives re-renders.
// Handles lifecycle (subscribe on mount, unsubscribe on unmount).
// #end-info:


//#info
// Typical data flow

// sending msg
// Component => MessageContext.sendMessage => SignalRService.send => server => broadcast
// receving msg
// Server(c#) => SignalRService.on('message') => MessageContext updates state => Components re-render


// The Message Flow Pattern
// How messages actually travel:

// 1 User A sends message → Our frontend calls sendMessage(receiverId, content, matchId)
// 2 This invokes the SendMessage method on our hub
// 3 Hub saves to database via SendMessageAsync
// 4 Hub sends to:
//      Receiver's group (so User B gets it)
//      Sender's connection (so User A sees their message immediately)
// 5 Both clients receive the message in their ReceiveMessage handler
// 6 Our filtering logic determines if it's relevant for the current screen
//#end-info








import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { Alert } from 'react-native';

class SignalRService {
    private connection: HubConnection | null = null;
    private reconnectAttempts = 0;
    private maxReconnectAttempts = 5;
    private messageCallback: ((message: any) => void) | null = null;

    // Store the callback so we can set it up before connection starts (ai fix)
    // private setupMessageHandlers(): void {
    //     if (this.connection && this.messageCallback) {
    //         this.connection.on('ReceiveMessage', this.messageCallback);
    //     }
    // }

    async startConnection(): Promise<void> {
        try {
            const token = await AsyncStorage.getItem('authToken');
            if (!token) {
                throw new Error('No authentication token found');
            }

            // little reviwer: this refers to specific instance of class (signalRService). 
            // ⚠️LOOK BELOW: 
            // const signalRService = new SignalRService();
            // const signalRService2 = new SignalRService();
            // const signalRService3 = new SignalRService();


            this.connection = new HubConnectionBuilder()
                .withUrl(`${process.env.EXPO_PUBLIC_API_BASE_URL?.replace('/api', '')}/hubs/messagehub`, {
                    accessTokenFactory: () => token
                })

                .withAutomaticReconnect()
                .configureLogging(LogLevel.Information)
                .build();







            // Event handlers:
            // TIP: always CLOSE, RECONNECT, OPEN websockets because they are stateful and persistent. 
            // meaning yung memory is na ke-keep like session. may dinadalang data across functions unlike typical compoment such sa controller/serivce they are stateless, once function RUNS and EXECUTED dun lang memory nia, 
            // unlike stateful components hangang "EXPLICIT OFF"
            this.connection.onreconnecting((error) => {
                console.log('Connection lost due to error: ', error);
                this.reconnectAttempts++;
                if (this.reconnectAttempts > this.maxReconnectAttempts) {
                    Alert.alert('Connection Error', 'Failed to reconnect to messaging service');
                }
            });

            this.connection.onreconnected((connectionId) => {
                console.log('Reconnected with connection ID: ', connectionId);
                this.reconnectAttempts = 0;
            });

            this.connection.onclose((error) => {
                console.log('Connection closed due to error: ', error);
                this.connection = null;
            });

            // Set up message handler BEFORE starting connection
            // #warning set up all Our event handlers (.on()) before calling .start().
            //include this setupMessageHandlers -> to readily receive msgs from ReceiveMessage
            // #end-warning

            if (this.connection && this.messageCallback) {
                this.connection.on('ReceiveMessage', this.messageCallback);
            }

            


            await this.connection.start();
            console.log('SignalR connection established');
            this.reconnectAttempts = 0;
        } catch (error) {
            console.error('Error starting SignalR connection:', error);
            throw error;
        }
    }





    //DEFINES HERE: but triggered on MessageContext
    // main logic: this.connection.stop();
    async stopConnection(): Promise<void> {
        if (this.connection) {
            try {
                await this.connection.stop();
                this.connection = null;
                console.log('SignalR connection stopped');
            } catch (error) {
                console.error('Error stopping SignalR connection:', error);
            }
        }
    }




    //DEFINES HERE: but triggered on MessageContext
    // the listener to channel of ReceiveMessage
    // main logic: this.connection.on();

    //https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client?view=aspnetcore-9.0&tabs=visual-studio
    onReceiveMessage(callback: (message: any) => void): void {
        this.messageCallback = callback;

        // If connection is already established, set up the handler immediately
        if (this.connection) {
            this.connection.on('ReceiveMessage', callback);
        }
        // If not connected yet, the handler will be set up in setupMessageHandlers() when connection starts
    }

    // Clean up function when component unmounts
    //DEFINES HERE: but triggered on MessageContext
    // main logic: this.connection.off();

    offReceiveMessage(): void {
        this.messageCallback = null;
        if (this.connection) {
            this.connection.off('ReceiveMessage');
        }
    }


    //public async Task SendMessage(Guid receiverId, string content, long matchId)
    // main logic: this.connection.invoke();

    async sendMessage(receiverId: string, content: string, matchId: number): Promise<void> {
        if (!this.connection) {
            await this.startConnection();
        }

        try {
            await this.connection!.invoke('SendMessage', receiverId, content, matchId);
        } catch (error) {
            console.error('Error sending message:', error);
            throw error;
        }
    }

    isConnected(): boolean {
        return this.connection !== null &&
            this.connection.state === HubConnectionState.Connected;
    }
}

export const signalRService = new SignalRService();