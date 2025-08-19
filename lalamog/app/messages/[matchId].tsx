
import { useLocalSearchParams } from 'expo-router';

export default function ChatScreen() {
  const { matchId } = useLocalSearchParams<{ matchId: string }>();
  console.log(matchId)
}