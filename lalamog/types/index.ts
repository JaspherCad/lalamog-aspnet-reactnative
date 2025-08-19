export type RootStackParamList = {
  'messages/[matchId]': {
    matchId: string;
    receiverId: string;
  };
  
  Tabs: undefined; 
  
  Chat: { matchId: string; receiverId: string };
};