//ERROR: Warning: tried to subscribe multiple times. 'subscribe' can only be called a single time per channel instance

import { Session } from '@supabase/supabase-js';
import React, { createContext, useContext, useEffect, useRef, useState } from 'react';
import { Alert } from 'react-native';
import { supabase } from './supabase';
import { getProfileAPI, loginAPI, setToken, setUserInfo, getUserInfo, removeToken, getToken, removeUserInfo, isTokenExpired, validateSessionAPI, getProfileAuthAPI } from '@/api/axiosInstance';
import { SessionData, LoginCredentials, UserInfo } from '@/api/axiosInstance';

interface AuthContextType {
    session: SessionData | null;  // Changed to SessionData
    isLoading: boolean;
    signInEmail: (credentials: LoginCredentials) => Promise<{ data: any }>;
    signOut: () => Promise<void>;
    refreshSession: () => Promise<void>;  // Add refresh function
}

const AuthContext = createContext<AuthContextType>({
    session: null,
    isLoading: true,
    signInEmail: async () => ({ data: null }),
    signOut: async () => { },
    refreshSession: async () => { },
});

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
    const [session, setSession] = useState<SessionData | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    //onount auth state on component mount
    useEffect(() => {
        const initAuth = async () => {
            console.log('Initializing auth...');
            try {
                const token = await getToken();
                if (token && !isTokenExpired(token)) {
                    console.log('Token found and valid, validating with backend...');
                    try {
                        const response = await validateSessionAPI();
                        const profile = response.data;
                        
                        //do sessionData from validated profile
                        const sessionData: SessionData = {
                            user: {
                                id: profile.id,
                                userId: profile.userId,
                                nickname: profile.nickname,
                                bio: profile.bio,
                                location: profile.location,
                                fightingStyle: profile.fightingStyle,
                                experienceLevel: profile.experienceLevel,
                                profilePictureUrl: profile.profilePictureUrl,
                                createdAt: profile.createdAt,
                                updatedAt: profile.updatedAt,
                                availability: profile.availability,
                            },
                            jwt: token
                        };

                        await setUserInfo(sessionData);
                        setSession(sessionData);
                    } catch (validationError) {
                        console.log('Session validation failed:', validationError);
                        // Token TRUE but validation failed THEN clear everything
                        await removeToken();
                        await removeUserInfo();
                        setSession(null);
                    }
                } else {
                    // Token is expired or doesn't exist
                    await removeToken();
                    await removeUserInfo();
                    setSession(null);
                }
            } catch (error) {
                console.error('Auth initialization error:', error);
                await removeToken();
                await removeUserInfo();
                setSession(null);
            } finally {
                setIsLoading(false);
            }
        };

        initAuth();
    }, []);

    const signInEmail = async (credentials: LoginCredentials) => {
        setIsLoading(true);
        try {
            console.log(credentials)
            const response = await loginAPI(credentials);
            const profile = response.data;
            console.log(profile);
            
            // Store the token
            await setToken(profile.jwtToken);

            // Create SessionData object
            const sessionData: SessionData = {
                user: {
                    id: profile.id,           // Profile ID (GUID)
                    userId: profile.userId,   // User ID (GUID)
                    nickname: profile.nickname,
                    bio: profile.bio,
                    location: profile.location,
                    fightingStyle: profile.fightingStyle,
                    experienceLevel: profile.experienceLevel,
                    profilePictureUrl: profile.profilePictureUrl,
                    createdAt: profile.createdAt,
                    updatedAt: profile.updatedAt,
                    availability: profile.availability,
                },
                jwt: profile.jwtToken
            };

            // Store session data
            await setUserInfo(sessionData);
            setSession(sessionData);
            
            return response;
        } catch (error) {
            console.error('Sign in error:', error);
            throw error;
        } finally {
            setIsLoading(false);
        }
    };

    const signOut = async () => {
        try {
            setIsLoading(true);
            // Clear tokens and user info
            await removeToken();
            await removeUserInfo();
            // Update state
            setSession(null);
        } catch (error) {
            console.error('Sign out error:', error);
        } finally {
            setIsLoading(false);
        }
    };

    const refreshSession = async () => {
        try {
            const token = await getToken();
            if (!token || isTokenExpired(token)) {
                await signOut();
                return;
            }

            // Fetch fresh profile data from backend
            const response = await getProfileAuthAPI();
            const profile = response.data;

            // Update session with fresh data
            const sessionData: SessionData = {
                user: {
                    id: profile.id,
                    userId: profile.userId,
                    nickname: profile.nickname,
                    bio: profile.bio,
                    location: profile.location,
                    fightingStyle: profile.fightingStyle,
                    experienceLevel: profile.experienceLevel,
                    profilePictureUrl: profile.profilePictureUrl,
                    createdAt: profile.createdAt,
                    updatedAt: profile.updatedAt,
                    availability: profile.availability,
                },
                jwt: token
            };

            await setUserInfo(sessionData);
            setSession(sessionData);
        } catch (error) {
            console.error('Session refresh error:', error);
            await signOut();
        }
    };

    // Token validation effect
    useEffect(() => {
        const checkToken = async () => {
            console.log('=== Token check running ===');
            const token = await getToken();
            if (token) {
                console.log('Token found, checking expiration...');
                if (isTokenExpired(token)) {
                    console.log('Token is EXPIRED - showing alert');
                    Alert.alert(
                        'Session Expired',
                        'Your session has expired. Please log in again.',
                        [
                            {
                                text: 'OK',
                                onPress: async () => {
                                    await signOut();
                                }
                            }
                        ]
                    );
                } else {
                    console.log('Token is still valid');
                    // Periodically validate session with backend
                    try {
                        const response = await validateSessionAPI();
                        const profile = response.data;
                        
                        // Update session with fresh data
                        const sessionData: SessionData = {
                            user: {
                                id: profile.id,
                                userId: profile.userId,
                                nickname: profile.nickname,
                                bio: profile.bio,
                                location: profile.location,
                                fightingStyle: profile.fightingStyle,
                                experienceLevel: profile.experienceLevel,
                                profilePictureUrl: profile.profilePictureUrl,
                                createdAt: profile.createdAt,
                                updatedAt: profile.updatedAt,
                                availability: profile.availability,
                            },
                            jwt: token
                        };

                        await setUserInfo(sessionData);
                        setSession(sessionData);
                    } catch (validationError) {
                        console.log('Periodic session validation failed:', validationError);
                        Alert.alert(
                            'Session Invalid',
                            'Your session is no longer valid. Please log in again.',
                            [
                                {
                                    text: 'OK',
                                    onPress: async () => {
                                        await signOut();
                                    }
                                }
                            ]
                        );
                    }
                }
            }
        };

        checkToken();
        // Check token validity every 5 minutes
        const interval = setInterval(checkToken, 5 * 60 * 1000);
        return () => clearInterval(interval);
    }, []);

    // Create the context value with all required properties
    const value = {
        session,
        isLoading,
        signInEmail,
        signOut,
        refreshSession
    };

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => useContext(AuthContext);