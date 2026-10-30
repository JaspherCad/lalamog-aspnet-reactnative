// // CREATE TABLE messages (
// //   id bigserial PRIMARY KEY,
// //   match_id bigint REFERENCES matches NOT NULL,
// //   sender_id uuid REFERENCES profiles NOT NULL,
// //   receiver_id uuid REFERENCES profiles NOT NULL,
// //   content TEXT NOT NULL,
// //   read BOOLEAN DEFAULT FALSE,
// //   created_at TIMESTAMPTZ DEFAULT NOW()
// // );

// import { useEffect, useRef, useState } from "react";
// import { supabase } from "./supabase";
// import { useAuth } from "./AuthProvider";

// export type Message = {
//     id: number;
//     match_id: number;
//     sender_id: string;
//     receiver_id: string;
//     content: string;
//     read: boolean;
//     created_at: string;
// }

// interface UseMessagesProps {
//     matchId: number;
//     senderId: string;
//     receiverId: string;
// }




// export function useMessages({ matchId, senderId, receiverId }: UseMessagesProps) {
//     const [messages, setMessages] = useState<Message[]>([]);
//     const [fetching, setFetching] = useState(true);
//     const hasSubscribed = useRef(false);
//     const { session } = useAuth()


//     const load = async () => {
//         try {
//             const { data, error } = await supabase
//                 .from('messages')
//                 .select('*')
//                 .eq('match_id', matchId)
//                 .order('created_at', { ascending: true });

//             if (error) throw error;
//             setMessages(data || []);
//         } catch (error) {
//             console.error(error);
//         } finally {
//             setFetching(false);
//         }
//     };

//     useEffect(() => {
//         if (!matchId || !session?.user) return; // || hasSubscribed.current


//         const channelName = `messages-changes-${session.user.id}-${matchId}`;
//         const channel = supabase
//             .channel(channelName)
//             .on(
//                 'postgres_changes',
//                 {
//                     event: 'INSERT',
//                     schema: 'public',
//                     table: 'messages',
//                     filter: `match_id=eq.${matchId}`,
//                 },
//                 (payload) => {
//                     setMessages((prev) => [
//                         ...prev,
//                         payload.new as Message

//                     ]);
//                 }
//             )
//             .subscribe();

//         //hasSubscribed.current = true;

//         return () => {
//             supabase.removeChannel(channel);
//             //hasSubscribed.current = false;
//         };
//     }, [matchId, session]);

//     // Initial load
//     useEffect(() => {
//         if (matchId) load();
//     }, [matchId]);







//     const sendMessage = async (content: string) => {
//         if (!session?.user) {
//             console.warn('No session found');
//             return;
//         }

//         if (!content.trim()) return;

//         //lexicography
//         // const [userId_1, userId_2] = [senderId, receiverId].sort();

//         const { error } = await supabase.from('messages').insert({
//             match_id: matchId,
//             sender_id: senderId,
//             receiver_id: receiverId,
//             content,
//         });


//         if (error) {
//             console.error('Message failed:', error.message);
//         }

//     }
//     return { messages, fetching, sendMessage };
// }