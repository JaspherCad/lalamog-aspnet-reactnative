import React, { useState, useEffect } from 'react';
import { View, Text, StyleSheet, ActivityIndicator, Alert } from 'react-native';
import * as Location from 'expo-location';
import PublicFightsMap from '@/Components/PublicFightsMap';
import { getAllPublicFights } from '@/api/axiosInstance';

export default function MyPage() {
    const [publicFights, setPublicFights] = useState([]);
    const [userLocation, setUserLocation] = useState<{latitude: number; longitude: number} | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        initPage();
    }, []);

    const initPage = async () => {
        try {
            setLoading(true);
            
            // Get user location
            const { status } = await Location.requestForegroundPermissionsAsync();
            if (status === 'granted') {
                const loc = await Location.getCurrentPositionAsync({});
                setUserLocation({
                    latitude: loc.coords.latitude,
                    longitude: loc.coords.longitude
                });
            }

            // Fetch public fights
            const response = await getAllPublicFights();
            setPublicFights(response.data);
            
        } catch (error) {
            console.error('Error loading public fights:', error);
            Alert.alert('Error', 'Failed to load public fights');
        } finally {
            setLoading(false);
        }
    };

    if (loading) {
        return (
            <View style={styles.loaderContainer}>
                <ActivityIndicator size="large" color="#007AFF" />
                <Text style={styles.loadingText}>Loading public fights...</Text>
            </View>
        );
    }

    return (
        <View style={styles.container}>
            <Text style={styles.title}>Public Fights Map</Text>
            <View style={styles.mapContainer}>
                <PublicFightsMap fights={publicFights} userLocation={userLocation} />
            </View>
        </View>
    );
}

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#f5f5f5',
    },
    title: {
        fontSize: 20,
        fontWeight: 'bold',
        textAlign: 'center',
        paddingVertical: 15,
        backgroundColor: '#fff',
        borderBottomWidth: 1,
        borderBottomColor: '#ddd',
    },
    mapContainer: {
        flex: 1,
    },
    loaderContainer: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: '#f5f5f5',
    },
    loadingText: {
        marginTop: 10,
        fontSize: 16,
        color: '#666',
    },
});
