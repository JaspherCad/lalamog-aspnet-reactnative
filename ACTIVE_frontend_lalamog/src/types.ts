export interface Session { userId: string }
export interface Profile {
  userId: string; nickname: string; bio: string; fightingStyle: string;
  experienceLevel: number; location: string; availability: string; avatarUrl: string | null;
}
export interface Match { id: number; opponent: Profile; createdAt: string }
export interface Message { id: number; matchId: number; senderId: string; content: string; createdAt: string }
export interface Confirmation { userId: string; emergencyContactName: string; emergencyContactPhone: string; acceptedAt: string }
export interface Fight {
  id: number; matchId: number; user1Id: string; user2Id: string; scheduledAt: string;
  location: string; address: string; status: 'scheduled' | 'confirmed' | 'canceled' | 'completed';
  user1Confirmed: boolean; user2Confirmed: boolean; cancellationReason: string | null; confirmations: Confirmation[];
}