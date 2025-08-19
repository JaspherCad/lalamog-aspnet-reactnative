import React, { useState, useEffect, useRef } from 'react';
import { View, Text, TextInput, Button, FlatList, StyleSheet, Keyboard, ActivityIndicator, TouchableOpacity } from 'react-native';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useAuth } from '@/lib/AuthProvider';
import { useMessages } from '@/lib/MessageContext';
import { getMatchesDataAPI, MessageData, ProfileData } from '@/api/axiosInstance';
import { getMatchesAPI } from '@/api/axiosInstance';
import { Avatar } from '@rneui/themed';

interface MatchDetails {
    id: number;
    status: string;
    createdAt: string;
    user1: ProfileData;  // Direct profile data, not nested
    user2: ProfileData;  // Direct profile data, not nested
}

// THIS IS THE ACTUAL RESPONSE OF MATCH DATA, fix the MatchDetails above
// [
//     {
//         "user1": {
//             "id": "30639e44-d0b0-4aed-8069-a3751deece4b",
//             "userId": "9718c9be-c9b1-4eed-89c4-cbf81d7f0e34",
//             "nickname": "JaysonBoxer",
//             "bio": "Jayson's Bio",
//             "location": {
//                 "x": 121.1870867,
//                 "y": 14.5456633
//             },
//             "fightingStyle": "Boxing",
//             "experienceLevel": 1,
//             "profilePictureUrl": null,
//             "createdAt": "2025-07-27T07:17:36.772417Z",
//             "updatedAt": "2025-07-27T08:15:52.39309Z",
//             "availability": {
//                 "days": [
//                     "Sat",
//                     "Wed"
//                 ],
//                 "time": "21:00-22:00"
//             },
//             "jwtToken": null
//         },
//         "user2": {
//             "id": "4b97819e-3a0d-4af0-9df9-694920d783d8",
//             "userId": "f2603802-deb3-4377-8651-1134db7837a7",
//             "nickname": "JnaeDog",
//             "bio": "Jane's Bio update haha",
//             "location": {
//                 "x": 121.1871405,
//                 "y": 14.5856713
//             },
//             "fightingStyle": "Brawler Type",
//             "experienceLevel": 3,
//             "profilePictureUrl": "/uploads/profile-images/f2603802-deb3-4377-8651-1134db7837a7/profile_1754030873.jpeg",
//             "createdAt": "2025-07-25T10:14:13.519477Z",
//             "updatedAt": "2025-08-02T08:33:03.636492Z",
//             "availability": {
//                 "days": [
//                     "Sat",
//                     "Sun"
//                 ],
//                 "time": "10:00-22:00"
//             },
//             "jwtToken": null
//         },
//         "createdAt": "2025-08-09T10:35:43.306924Z",
//         "id": 5,
//         "status": "active"
//     }
// ]

const MessageScreen = () => {
    const { session } = useAuth();
    const { messages, sendMessage, loadMessages, clearMessages } = useMessages();
    // #note: messages here are filled using websocket


    // #warning: HOW DID WE FILTEREDOUT THE MESSAGES? example:
    //  LOG  Received message: {"content": "Jay", "createdAt": "2025-08-19T08:13:11.4134551Z", "id": 25, "matchId": 0, "read": false, "receiverId": "9718c9be-c9b1-4eed-89c4-cbf81d7f0e34", "senderId": "f2603802-deb3-4377-8651-1134db7837a7"}

    // the backend's signal R:
    // When sending a message, it ONLY goes to specific connections
    // await Clients.Group(receiverId.ToString()).SendAsync("ReceiveMessage", message);
    // await Clients.Caller.SendAsync("ReceiveMessage", message);

    //when frontend recieve signal r, it is automatically filteredout. we only recieve messages that are relevant to the current user...


    // #end-warning


    // #note: loadMessages here are NOT realtime, that's basic API fetch.

    const [newMessage, setNewMessage] = useState('');
    const [matchDetails, setMatchDetails] = useState<MatchDetails | null>(null);
    const [loadingMatch, setLoadingMatch] = useState(true);
    const flatListRef = useRef<FlatList>(null);


    const { matchId } = useLocalSearchParams<{ matchId: string }>();
    const router = useRouter();



    // loads the match details INITIALLY ON MOUNT;
    // note: loadMessages here are NOT realtime, that's basic API fetch.

    useEffect(() => {
        const loadMatchDetails = async () => {
            if (!session || !matchId) return;

            try {
                setLoadingMatch(true);
                const response = await getMatchesDataAPI();
                const matches = response.data;

                const match = matches.find((m: MatchDetails) => m.id === parseInt(matchId));

                if (!match) {
                    throw new Error('Match not found');
                }

                setMatchDetails(match);

                loadMessages(parseInt(matchId));
                // explicitly setMessages(response.data); inside function.
            } catch (error) {
                console.error('Failed to load match details:', error);
                // Go back to matches list if match not found
                router.replace('/(tabs)/messages');
            } finally {
                setLoadingMatch(false);
            }
        };

        loadMatchDetails();
    }, [matchId, session]);


    // required
    useEffect(() => {
        return () => {
            clearMessages();
        };
    }, []);



    // different on index.tsx... 
    // #warning: RETURNS PROFILE NOT USER!
    const getOtherUser = () => {
        if (!matchDetails || !session) return null;
        return session?.user?.id === matchDetails.user1.userId ? matchDetails.user2 : matchDetails.user1;
    };


    const otherUser = getOtherUser();
    const otherUserProfile = otherUser; //ProfileData DTO.


    const handleSend = async () => {
        if (!newMessage.trim() || !session || !matchDetails || !otherUser) return;

        try {
            // #error: ForeignKey error because I am using the ID of MATCH not UserID
            //         Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes. See the inner exception for details.
            //    ---> Npgsql.PostgresException (0x80004005): 23503: insert or update on table "Messages" violates foreign key constraint "FK_Messages_AspNetUsers_ReceiverId"
            // #end-error:



            await sendMessage(otherUser.userId, newMessage.trim(), parseInt(matchId));
            console.log('Sending message:', {
                to: otherUser.userId,
                content: newMessage.trim(),
                matchId: parseInt(matchId)
            });
            setNewMessage('');
            Keyboard.dismiss();



            // go to bottom (scroll)
            setTimeout(() => {
                flatListRef.current?.scrollToEnd({ animated: true });
            }, 100);
        } catch (error) {
            console.error('Failed to send message:', error);
        }
    };



    //USED IN FLATLIST
    const renderMessage = ({ item }: { item: MessageData }) => {
        const isCurrentUser = session?.user?.id === item.senderId;

        return (
            <View style={[
                styles.messageContainer,
                isCurrentUser ? styles.currentUserMessage : styles.otherUserMessage
            ]}>
                <Text style={styles.messageText}>{item.content}</Text>
                <Text style={styles.messageTime}>
                    {new Date(item.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </Text>
            </View>
        );
    };


    //ACTUAL RENDER
    //ACTUAL RENDER
    //ACTUAL RENDER
    //ACTUAL RENDER





    //base case
    if (loadingMatch) {
        return (
            <View style={styles.loaderContainer}>
                <ActivityIndicator size="large" color="#007AFF" />
                <Text>Loading conversation...</Text>
            </View>
        );
    }

    if (!matchDetails || !otherUser) {
        return (
            <View style={styles.errorContainer}>
                <Text style={styles.errorText}>Conversation not found</Text>
                <Button
                    title="Back to Matches"
                    onPress={() => router.replace('/(tabs)/messages')}
                />
            </View>
        );
    }



    return (
        <View style={styles.container}>
            <View style={styles.header}>
                <TouchableOpacity
                    style={styles.backButton}
                    onPress={() => router.replace('/(tabs)/messages')}
                >
                    <Text style={styles.backText}>←</Text>
                </TouchableOpacity>
                <View style={styles.headerContent}>
                    <Avatar
                        size={40}
                        rounded
                        source={otherUserProfile?.profilePictureUrl ? { uri: otherUserProfile.profilePictureUrl } : undefined}
                        icon={!otherUserProfile?.profilePictureUrl ? { type: 'font-awesome', name: 'user' } : undefined}
                        containerStyle={styles.avatar}
                    />
                    <View>
                        <Text style={styles.receiverName}>
                            {otherUserProfile?.nickname || otherUserProfile?.fightingStyle}
                        </Text>
                        <Text style={styles.status}>
                            {matchDetails.status === 'active' ? 'Online' : 'Last seen recently'}
                        </Text>
                    </View>
                </View>
            </View>

            <FlatList
                ref={flatListRef}
                data={messages}
                renderItem={renderMessage}
                keyExtractor={(item) => item.id.toString()}
                style={styles.messagesList}
                onContentSizeChange={() => flatListRef.current?.scrollToEnd({ animated: true })}
            />

            <View style={styles.inputContainer}>
                <TextInput
                    style={styles.input}
                    value={newMessage}
                    onChangeText={setNewMessage}
                    placeholder="Type a message..."
                    onSubmitEditing={handleSend}
                    returnKeyType="send"
                />
                <Button
                    title="Send"
                    disabled={!newMessage.trim()}
                    onPress={handleSend}
                />
            </View>
        </View>
    );
};




const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#fff',
    },
    header: {
        flexDirection: 'row',
        alignItems: 'center',
        padding: 15,
        borderBottomWidth: 1,
        borderBottomColor: '#eee',
        backgroundColor: '#fff',
    },
    backButton: {
        marginRight: 10,
    },
    backText: {
        fontSize: 24,
    },
    headerContent: {
        flexDirection: 'row',
        alignItems: 'center',
    },
    avatar: {
        backgroundColor: '#f0f0f0',
        marginRight: 10,
    },
    receiverName: {
        fontSize: 18,
        fontWeight: 'bold',
    },
    status: {
        color: '#4CAF50',
        fontSize: 12,
    },
    messagesList: {
        flex: 1,
        padding: 10,
    },
    messageContainer: {
        maxWidth: '80%',
        padding: 10,
        marginVertical: 5,
        borderRadius: 15,
    },
    currentUserMessage: {
        alignSelf: 'flex-end',
        backgroundColor: '#007AFF',
    },
    otherUserMessage: {
        alignSelf: 'flex-start',
        backgroundColor: '#f0f0f0',
    },
    messageText: {
        color: '#000',
    },
    messageTime: {
        fontSize: 10,
        opacity: 0.7,
        textAlign: 'right',
        marginTop: 3,
    },
    inputContainer: {
        flexDirection: 'row',
        padding: 10,
        borderTopWidth: 1,
        borderTopColor: '#eee',
    },
    input: {
        flex: 1,
        borderWidth: 1,
        borderColor: '#ddd',
        borderRadius: 20,
        padding: 10,
        marginRight: 10,
    },
    loaderContainer: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
    },
    errorContainer: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
        padding: 20,
    },
    errorText: {
        color: '#D32F2F',
        fontSize: 16,
        marginBottom: 10,
    },
});




export default MessageScreen;