import CustomScreenHeader from "@/Components/CustomScreenHeader";
import { View, Text, FlatList, StyleSheet, TouchableOpacity, ActivityIndicator } from 'react-native';
import { connectionTest, getMatchesAPI, getMatchesDataAPI, ProfileData } from '@/api/axiosInstance';
import { useAuth } from "@/lib/AuthProvider";
import { useEffect, useState } from "react";
import { router } from 'expo-router';

import { Avatar, Image } from '@rneui/themed';

//❓ THIS LISTS all active matches:
// main idea:
// lists of profiles that I can chat, once clicked we redirect into messages/[matchId]



interface MatchItem {
  id: number;
  user1Id?: string; // Optional since backend might not provide it
  user2Id?: string; // Optional since backend might not provide it
  status: string;
  createdAt: string;
  updatedAt?: string;
  user1: ProfileData; // Direct profile data, not nested
  user2: ProfileData; // Direct profile data, not nested
}



export default function Index() {
  const { session } = useAuth();
  const [matches, setMatches] = useState<MatchItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);


  useEffect(() => {
    const loadMatches = async () => {
      if (!session) return;

      try {
        setLoading(true);
        const response = await getMatchesDataAPI();
        setMatches(response.data);
        console.log('Matches loaded successfully:', response.data);
        setError(null);
      } catch (err) {
        setError('Failed to load matches');
        console.error('Error loading matches:', err);
      } finally {
        setLoading(false);
      }
    };

    loadMatches();
  }, [session]);

  //helper function to get the otherUser Except me ofc
  const getOtherUser = (match: MatchItem) => {
    // Debug logging
    console.log('=== DEBUGGING MATCH ===');
    console.log('My session user ID (userId):', session?.user.userId);
    console.log('My session profile ID (id):', session?.user.id);
    console.log('Match ID:', match.id);
    console.log('Match user1Id:', match.user1Id);
    console.log('Match user2Id:', match.user2Id);
    console.log('Match user1.userId:', match.user1.userId);
    console.log('Match user2.userId:', match.user2.userId);
    console.log('Match user1.nickname:', match.user1.nickname);
    console.log('Match user2.nickname:', match.user2.nickname);
    
    // Compare using the userId field from the profile data with session.user.userId (not .id)
    const isUser1Me = match.user1.userId === session?.user.userId;
    console.log('Is user1 me?', isUser1Me);
    
    const otherUser = isUser1Me ? match.user2 : match.user1;
    console.log('Selected other user:', otherUser.nickname, otherUser.userId);
    console.log('=== END DEBUG ===');
    
    return otherUser;
  };


  const renderMatchItem = ({ item }: { item: MatchItem }) => { 
    const otherUser = getOtherUser(item);
    // otherUser is now directly a ProfileData object

    return (
      <TouchableOpacity
        style={styles.matchItem}
        onPress={() => router.push({
          pathname: '/messages/[matchId]',
          params: {
            matchId: item.id,
            receiverId: otherUser.userId, // Use userId from profile
            receiverName: otherUser.nickname || 'Unknown User'
          }
        })}
      >
        <View style={styles.avatarContainer}>
          <Avatar
            size={60}
            rounded
            source={otherUser?.profilePictureUrl ? { uri: otherUser.profilePictureUrl } : undefined}
            icon={!otherUser?.profilePictureUrl ? { type: 'font-awesome', name: 'user' } : undefined}
            containerStyle={styles.avatar}
          />
          {item.status === 'active' && <View style={styles.onlineIndicator} />}
        </View>
        <View style={styles.matchDetails}>
          <Text style={styles.name}>
            {otherUser.nickname || 'Unknown User'}
          </Text>
          <Text style={styles.lastMessage} numberOfLines={1}>
            {otherUser?.bio || 'No bio yet'}
          </Text>
        </View>
        <Text style={styles.time}>
          {new Date(item.createdAt).toLocaleDateString()}
        </Text>
      </TouchableOpacity>
    );
  };









  if (loading) {
    return (
      <View style={styles.loaderContainer}>
        <ActivityIndicator size="large" color="#007AFF" />
        <Text>Loading matches...</Text>
      </View>
    );
  }

  if (error) {
    return (
      <View style={styles.errorContainer}>
        <Text style={styles.errorText}>{error}</Text>
        <TouchableOpacity 
          style={styles.retryButton} 
          onPress={async () => {
            if (!session) return;
            try {
              setLoading(true);
              setError(null);
              const response = await getMatchesDataAPI();
              setMatches(response.data);
              console.log('Matches reloaded successfully:', response.data);
            } catch (err) {
              setError('Failed to load matches');
              console.error('Error reloading matches:', err);
            } finally {
              setLoading(false);
            }
          }}
        >
          <Text style={styles.retryText}>Retry</Text>
        </TouchableOpacity>
      </View>
    );
  }

  if (matches.length === 0) {
    return (
      <View style={styles.emptyContainer}>
        <Text style={styles.emptyText}>No matches yet</Text>
        <Text style={styles.emptySubtext}>
          Start swiping to find people you match with!
        </Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {/* AKA .map() */}
      <FlatList
        data={matches}
        renderItem={renderMatchItem}
        keyExtractor={item => item.id.toString()}
        contentContainerStyle={styles.listContent}
      />
    </View>
  );
};







const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff',
  },
  listContent: {
    padding: 10,
  },
  matchItem: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 15,
    borderBottomWidth: 1,
    borderBottomColor: '#eee',
  },
  avatarContainer: {
    position: 'relative',
  },
  avatar: {
    backgroundColor: '#f0f0f0',
  },
  onlineIndicator: {
    position: 'absolute',
    bottom: 5,
    right: 5,
    width: 12,
    height: 12,
    backgroundColor: '#4CAF50',
    borderRadius: 6,
    borderWidth: 2,
    borderColor: '#fff',
  },
  matchDetails: {
    flex: 1,
    marginLeft: 15,
  },
  name: {
    fontSize: 16,
    fontWeight: 'bold',
    marginBottom: 3,
  },
  lastMessage: {
    color: '#666',
    fontSize: 14,
  },
  time: {
    fontSize: 12,
    color: '#999',
    minWidth: 60,
    textAlign: 'right',
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
  retryButton: {
    backgroundColor: '#007AFF',
    paddingHorizontal: 20,
    paddingVertical: 10,
    borderRadius: 8,
  },
  retryText: {
    color: 'white',
    fontWeight: 'bold',
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
  },
  emptyText: {
    fontSize: 18,
    fontWeight: 'bold',
    marginBottom: 10,
  },
  emptySubtext: {
    color: '#666',
    textAlign: 'center',
    fontSize: 14,
  },
});
    
