import axios from 'axios';
import AsyncStorage from '@react-native-async-storage/async-storage';

const API_BASE_URL = process.env.EXPO_PUBLIC_API_BASE_URL;

// Debug logging
console.log('API_BASE_URL:', API_BASE_URL);

// ALL INTERFACE SHOULD MATCH WHAT BACKEND IS THROWING AT US
export interface RegisterData {
    email: string;
    password: string;
    confirmPassword: string;
    fullName?: string;
    birthDate?: string;
}
// ALL INTERFACE SHOULD MATCH WHAT BACKEND IS THROWING AT US
export interface LoginCredentials {
    email: string;
    password: string;
}


// ALL INTERFACE SHOULD MATCH WHAT BACKEND IS THROWING AT US
export interface MessageData {
    id: number;
    senderId: string;
    receiverId: string;
    content: string;
    matchId: number;
    read: boolean;
    createdAt: string;
}


// ALL INTERFACE SHOULD MATCH WHAT BACKEND IS THROWING AT US
interface LocationDto {
    x: number;
    y: number;
}
// ALL INTERFACE SHOULD MATCH WHAT BACKEND IS THROWING AT US
interface Availability {
    days: string[];
    time: string;
}
// ALL INTERFACE SHOULD MATCH WHAT BACKEND IS THROWING AT US
export interface ProfileData {
    id: string;           // Profile ID (GUID)
    userId: string;       // User ID (GUID) - THIS is what goes in UserInfo.id
    nickname?: string;
    bio?: string;
    location?: LocationDto;
    fightingStyle?: string;
    experienceLevel?: number;
    profilePictureUrl?: string | null;
    createdAt: string;
    updatedAt: string;
    availability?: Availability;
}

// ALL INTERFACE SHOULD MATCH WHAT BACKEND IS THROWING AT US
export interface AllProfileData {
    matchedProfiles: ProfileData[];           // Profile ID (GUID)
    availableProfiles: ProfileData[];       // User ID (GUID) - THIS is what goes in UserInfo.id

}

// ALL INTERFACE SHOULD MATCH WHAT BACKEND IS THROWING AT US
export interface SwipeData {
    SwipeeId: string;
    Direction: string;
}
// ALL INTERFACE SHOULD MATCH WHAT BACKEND IS THROWING AT US
export interface UserInfo {
    email: string;
    name?: string;
    id?: string;
}
// ALL INTERFACE SHOULD MATCH WHAT BACKEND IS THROWING AT US
// New SessionData interface that contains both user profile and JWT token
export interface SessionData {
    user: ProfileData;
    jwt: string;
}

// Create axios instance with base configuration
const axiosInstance = axios.create({
    baseURL: API_BASE_URL,
    timeout: 10000,
    headers: {
        'Content-Type': 'application/json',
    },
});

// Request interceptor to add JWT token to all requests
axiosInstance.interceptors.request.use(
    async (config) => {
        // Get token from localStossrage
        const token = await AsyncStorage.getItem('authToken');

        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }

        console.log('Request:', config.method?.toUpperCase(), config.url);
        return config;
    },
    (error) => {
        console.error('Request error:', error);
        return Promise.reject(error);
    }
);

// Response interceptor to handle errors and token refresh
axiosInstance.interceptors.response.use(
    (response) => {
        console.log('Response:', response.status, response.config.url);
        return response;
    },
    (error) => {
        console.error('Response error:');
        console.error(error);

        // Handle 401 Unauthorized - token expired or invalid
        if (error.response?.status === 401) {
            // Clear token and redirect to login
            AsyncStorage.removeItem('userInfo');
            AsyncStorage.removeItem('authToken');

            // You can redirect to login page here if needed
            // window.location.href = '/login';

            console.log('Token expired or invalid. Please login again.');
        }

        return Promise.reject(error);
    }
);























export const registerAPI = async (userData: RegisterData) => {
    try {
        const response = await axiosInstance.post('/user/register', userData);
        // {
        //   "email": "user@example.com",
        //   "password": "password123",
        //   "confirmPassword": "password123",
        //   "fullName": "John Doe",
        //   "birthDate": "1990-01-01"
        // }
        return response;
    } catch (error) {
        throw error;
    }
};

export const loginAPI = async (credentials: LoginCredentials) => {
    try {
        const response = await axiosInstance.post('/user/login', credentials);
        // {
        //   "email": "user@example.com",
        //   "password": "password123"
        // }
        return response;
    } catch (error) {
        throw error;
    }
};

export const getCurrentUserAPI = async () => {
    try {
        const response = await axiosInstance.get('/user/me');
        return response;
    } catch (error) {
        throw error;
    }
};

export const getCurrentUserAuthAPI = async () => {
    try {
        const response = await axiosInstance.get('/auth/me');
        return response;
    } catch (error) {
        throw error;
    }
};

// ============ PROFILE API CALLS ============

export const getProfileAPI = async () => {
    try {
        const response = await axiosInstance.get('/user/profile');
        return response;
    } catch (error) {
        throw error;
    }
};

export const getProfileAuthAPI = async () => {
    try {
        const response = await axiosInstance.get('/auth/profile');
        return response;
    } catch (error) {
        throw error;
    }
};

export const updateProfileAPI = async (profileData: ProfileData) => {
    try {
        const response = await axiosInstance.put('/user/profile', profileData);
        //    {
        //     "id": "4d4eb747-0d70-4a23-bf13-b5f1d102d67c",
        //     "userId": "ac4ca98d-1c61-4989-8728-195012bd8632",
        //     "nickname": "JazmineKingCrusher",
        //     "bio": "Jane's Bio",
        //     "location": {
        //         "x": 121.2870867,
        //         "y": 14.1456633
        //     },
        //     "fightingStyle": "Taekwando",
        //     "experienceLevel": 2,
        //     "profilePictureUrl": null,
        //     "createdAt": "2025-07-27T07:16:15.539991Z",
        //     "updatedAt": "2025-07-27T08:22:05.8103404Z",
        //     "availability": {
        //         "days": [
        //             "Sat",
        //             "Sun"
        //         ],
        //         "time": "10:00-22:00"
        //     }
        // }
        return response;
    } catch (error) {
        throw error;
    }
};

export const updateProfileAuthAPI = async (profileData: ProfileData) => {
    try {
        const response = await axiosInstance.put('/auth/profile', profileData);

        //    {
        //     "id": "4d4eb747-0d70-4a23-bf13-b5f1d102d67c",
        //     "userId": "ac4ca98d-1c61-4989-8728-195012bd8632",
        //     "nickname": "JazmineKingCrusher",
        //     "bio": "Jane's Bio",
        //     "location": {
        //         "x": 121.2870867,
        //         "y": 14.1456633
        //     },
        //     "fightingStyle": "Taekwando",
        //     "experienceLevel": 2,
        //     "profilePictureUrl": null,
        //     "createdAt": "2025-07-27T07:16:15.539991Z",
        //     "updatedAt": "2025-07-27T08:22:05.8103404Z",
        //     "availability": {
        //         "days": [
        //             "Sat",
        //             "Sun"
        //         ],
        //         "time": "10:00-22:00"
        //     }
        // }



        return response;
    } catch (error) {
        throw error;
    }
};

export const updateProfileByIdAPI = async (userId: string, profileData: ProfileData) => {
    try {
        const response = await axiosInstance.put(`/user/profileUpdate/${userId}`, profileData);
        //    {
        //     "id": "4d4eb747-0d70-4a23-bf13-b5f1d102d67c",
        //     "userId": "ac4ca98d-1c61-4989-8728-195012bd8632",
        //     "nickname": "JazmineKingCrusher",
        //     "bio": "Jane's Bio",
        //     "location": {
        //         "x": 121.2870867,
        //         "y": 14.1456633
        //     },
        //     "fightingStyle": "Taekwando",
        //     "experienceLevel": 2,
        //     "profilePictureUrl": null,
        //     "createdAt": "2025-07-27T07:16:15.539991Z",
        //     "updatedAt": "2025-07-27T08:22:05.8103404Z",
        //     "availability": {
        //         "days": [
        //             "Sat",
        //             "Sun"
        //         ],
        //         "time": "10:00-22:00"
        //     }
        // }
        return response;
    } catch (error) {
        throw error;
    }
};



export const uploadImageProfile = async (file: File) => {
    try {
        const formData = new FormData();
        formData.append('file', file); // Make sure the key matches your backend ("file")

        const response = await axiosInstance.post('/user/uploadprofileimage', formData, {
            headers: {
                'Content-Type': 'multipart/form-data'
            }
        });


        //    {
        //     "file": "actualImageFileData",
        //   }

        
        return response;
    } catch (error) {
        throw error;
    }
};










export const processSwipeAPI = async (swipeData: SwipeData) => {
    try {
        console.log(swipeData)
        const response = await axiosInstance.post('/swipe/swipes', swipeData);
        // {
        //     "SwipeeId": "5c344aee-e3c1-4faf-846b-73cb28574d76",
        //     "Direction": "right"
        // }
        return response;
    } catch (error) {
        throw error;
    }
};

export const getMatchesAPI = async () => {
    try {
        const response = await axiosInstance.get(`/swipe/all-matched-profiles`);
        return response;
    } catch (error) {
        throw error;
    }
};


export const getMatchesDataAPI = async () => {
    try {
        const response = await axiosInstance.get(`/swipe/all-matches-data`);
        return response;
    } catch (error) {
        throw error;
    }
};


export const getAvailableProfilesAPI = async () => {
    try {
        const response = await axiosInstance.get(`/swipe/all-profiles`);  
        // userId handled by jwt claims in backend AllProfileData
        return response;
    } catch (error) {
        throw error;
    }
};

// Session validation API - validates JWT and returns fresh Profile data
export const validateSessionAPI = async () => {
    try {
        const response = await axiosInstance.get('/user/validate');
        //returns Profile DTO with JWT token.
        return response;
    } catch (error) {
        throw error;
    }
};





export const connectionTest = async () => {
    try {
        const response = await axiosInstance.get(`/user/test-connection`);
        return response;
    } catch (error) {
        throw error;
    }
};


export const getMessagesByMatchIdAPI = async (matchId: number) => {
    try {
        const response = await axiosInstance.get(`/message/match/${matchId}`);
        return response;
    } catch (error) {
        throw error;
    }
};

export const sendMessageAPI = async (receiverId: string, content: string, matchId: number) => {
  try {
    const response = await axiosInstance.post('/message/sendMessage', {
      receiverId,
      content,
      matchId
    });
    return response;
  } catch (error) {
    throw error;
  }
};

// Google OAuth login (redirects to Google)
export const googleLoginRedirect = () => {
    window.location.href = `${API_BASE_URL}/auth/google`;
};

// TOKEN MANAGEMENT: to be used on authprovider context.
export const setToken = async (token: string): Promise<void> => {
    await AsyncStorage.setItem('authToken', token);
    axiosInstance.defaults.headers.common['Authorization'] = `Bearer ${token}`;
};


export const getToken = async (): Promise<string | null> => {
    return await AsyncStorage.getItem('authToken');
};




export const removeToken = async (): Promise<void> => {
    await AsyncStorage.removeItem('authToken');
    await AsyncStorage.removeItem('userInfo');
    delete axiosInstance.defaults.headers.common['Authorization'];
};

export const isAuthenticated = async (): Promise<boolean> => {
    const token = await getToken();
    if (!token) return false;

    // Check if token is expired
    return !isTokenExpired(token);
};

// Decode JWT token (simple decode without verification)
export const decodeToken = (token: string): any | null => {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function (c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        return JSON.parse(jsonPayload);
    } catch (error) {
        console.error('Error decoding token:', error);
        return null;
    }
};

export const isTokenExpired = (token: string): boolean => {
    const decoded = decodeToken(token);
    if (!decoded) return true;

    const currentTime = Date.now() / 1000;
    return decoded.exp < currentTime;
};

// User info utilities
export const setUserInfo = async (userInfo: SessionData): Promise<void> => {
    await AsyncStorage.setItem('userInfo', JSON.stringify(userInfo));
};

export const getUserInfo = async (): Promise<SessionData | null> => {
    const userInfo = await AsyncStorage.getItem('userInfo');
    return userInfo ? JSON.parse(userInfo) : null;
};

export const removeUserInfo = async (): Promise<void> => {
    await AsyncStorage.removeItem('userInfo');
};

// Error handling utility
export const handleAPIError = (error: any): string => {
    if (error.response) {
        // Server responded with error status
        const { status, data } = error.response;

        switch (status) {
            case 400:
                return data.message || 'Bad request. Please check your input.';
            case 401:
                return 'Unauthorized. Please login again.';
            case 403:
                return 'Forbidden. You don\'t have permission to perform this action.';
            case 404:
                return 'Resource not found.';
            case 500:
                return 'Internal server error. Please try again later.';
            default:
                return data.message || `Error: ${status}`;
        }
    } else if (error.request) {
        // Network error
        return 'Network error. Please check your connection.';
    } else {
        // Other error
        return error.message || 'An unexpected error occurred.';
    }
};

// Initialize token on app start
const initializeAuth = async (): Promise<void> => {
    const token = await AsyncStorage.getItem('authToken');
    if (token) {
        // Check if token is expired
        if (isTokenExpired(token)) {
            removeToken();
            removeUserInfo();
            console.log('Token expired on initialization');
        } else {
            // Set token in axios headers
            axiosInstance.defaults.headers.common['Authorization'] = `Bearer ${token}`;
            console.log('Token initialized');
        }
    }
};

// Call initialization
initializeAuth();

// Export the configured axios instance as default
export default axiosInstance;

// ============ USAGE EXAMPLES ============
/*
// Login example
try {
  const response = await loginAPI({ email: 'user@example.com', password: 'password' });
  const { token } = response.data;
  
  setToken(token);
  setUserInfo({ email: 'user@example.com' });
  
  console.log('Login successful');
} catch (error) {
  console.error('Login error:', handleAPIError(error));
}

// Get profile example
try {
  const response = await getProfileAPI();
  console.log('Profile:', response.data);
} catch (error) {
  console.error('Profile error:', handleAPIError(error));
}

// Update profile example
try {
  const profileData = {
    nickname: 'Fighter123',
    bio: 'MMA enthusiast',
    fightingStyle: 'MMA',
    experienceLevel: 2,
    location: { x: -74.006, y: 40.7128 },
    availability: {
      days: ['Mon', 'Wed', 'Fri'],
      time: '18:00-22:00'
    }
  };
  
  const response = await updateProfileAPI(profileData);
  console.log('Profile updated:', response.data);
} catch (error) {
  console.error('Update error:', handleAPIError(error));
}

// Process swipe example
try {
  const swipeData = {
    targetUserId: 'user-guid-here',
    isLike: true
  };
  
  const response = await processSwipeAPI(swipeData);
  console.log('Swipe result:', response.data);
} catch (error) {
  console.error('Swipe error:', handleAPIError(error));
}
*/
