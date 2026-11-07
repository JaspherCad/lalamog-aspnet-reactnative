export interface AreaOption { code: string; name: string }
export interface ReverseGeocodeResult {
  address: string; road: string | null; suburb: string | null; city: string | null; state: string | null; country: string | null;
  provider: string; streetNumber: string | null; postalCode: string | null; countryCode: string | null;
  resultType: string | null; confidence: string | null; matchCodes: string[];
  features: AzureMapsFeature[];
}
export interface AzureMapsFeature {
  type: 'Feature'; id?: string; bbox?: number[];
  geometry?: { type: 'Point'; coordinates: number[] };
  properties?: {
    address?: {
      formattedAddress?: string; addressLine?: string; streetNumber?: string; streetName?: string;
      neighborhood?: string; locality?: string; postalCode?: string;
      countryRegion?: { name?: string; ISO?: string };
      adminDistricts?: { name?: string; shortName?: string }[];
      intersection?: Record<string, unknown>;
    };
    type?: string; confidence?: string; matchCodes?: string[];
    geocodePoints?: { geometry: { type: 'Point'; coordinates: number[] }; calculationMethod?: string; usageTypes?: string[] }[];
    [key: string]: unknown;
  };
  [key: string]: unknown;
}
export interface MapLocation {
  regionCode?: string | null; provinceCode?: string | null; cityCode?: string | null; barangayCode?: string | null;
  psgcVersion?: string | null; latitude?: number | null; longitude?: number | null;
}
export interface Session { userId: string; roles?: string[]; permissions?: string[] }
export interface PublicFight { id: number; scheduledAt: string; status: Fight['status']; fighter1Name?: string | null; fighter2Name?: string | null; fighter1AvatarUrl?: string | null; fighter2AvatarUrl?: string | null }
export interface Gym {
  slug: string; description: string; contactInformation: string; sessionPrice: number; sessionDurationMinutes: number; isPublished: boolean; version: string; coverUrl: string;
  id: number; name: string; address: string; latitude: number; longitude: number; isDemo: boolean;
  areaPath: { code: string; name: string; level: string }[]; fights: PublicFight[];
}
export interface Profile {
  userId: string; nickname: string; bio: string; fightingStyle: string;
  fighterName?: string; mapLocation?: MapLocation | null;
  experienceLevel: number; location: string; availability: string; avatarUrl: string | null;
}
export interface Match { id: number; opponent: Profile; createdAt: string }
export interface Message { id: number; matchId: number; senderId: string; content: string; createdAt: string }
export interface Confirmation { allowPublicIdentity?: boolean; userId: string; emergencyContactName: string; emergencyContactPhone: string; acceptedAt: string }
export interface Fight {
  gymId?: number | null; quotedPrice?: number | null; sessionDurationMinutes?: number | null;
  id: number; matchId: number; user1Id: string; user2Id: string; scheduledAt: string;
  mapLocation?: MapLocation | null;
  location: string; address: string; status: 'pending_approval' | 'scheduled' | 'confirmed' | 'canceled' | 'completed';
  user1Confirmed: boolean; user2Confirmed: boolean; cancellationReason: string | null; confirmations: Confirmation[];
}

export interface GymBooking { id: number; gymId: number; gymName: string; fighter1Name: string; fighter2Name: string; scheduledAt: string; quotedPrice: number; sessionDurationMinutes: number; status: Fight['status']; confirmationCount: number; cancellationReason: string | null }
