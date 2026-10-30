import React, { useState, useEffect } from 'react';
import {
    View,
    Text,
    StyleSheet,
    TouchableOpacity,
    ScrollView,
    Alert,
    ActivityIndicator,
    Modal,
    TextInput,
    FlatList,
    RefreshControl
} from 'react-native';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useAuth } from '@/lib/AuthProvider';
import FontAwesome from '@expo/vector-icons/FontAwesome';
import { Avatar } from '@rneui/themed';
import { WebView } from 'react-native-webview';
import * as Location from 'expo-location';
import * as DocumentPicker from 'expo-document-picker';
import PublicFightsMap from '@/Components/PublicFightsMap';
import InteractiveMapPicker from '@/Components/InteractiveMapPicker';
import FightResultModal from '@/Components/FightResultModal';
import DateTimePicker from '@react-native-community/datetimepicker';
import { 
    getMatchesDataAPI, 
    ProfileData, 
    getFightSchedulesByMatchIdAPI,
    FightScheduleData,
    createFightScheduleAPI,
    cancelFightScheduleAPI,
    confirmFightScheduleAPI,
    uploadFightGifAPI,
    CreateFightScheduleData,
    CancelFightScheduleData,
    ConfirmFightData,
    getAllPublicFights,

} from '@/api/axiosInstance';

interface MatchDetails {
    id: number;
    status: string;
    createdAt: string;
    user1: ProfileData;
    user2: ProfileData;
}



/* HOW TO createFightScheduleAPI
using <MapView></MapView> (initalized by region state.) we are going to click the 
map react native to get LAT LONG then save to coordinates (state)

and then use that coordinates (state) to send data to reverseGeocode which uses openstreetmap...
*/


/* HOW TO createFightScheduleAPI
using <MapView></MapView> (initalized by region state.) we are going to click the 
map react native to get LAT LONG then save to coordinates (state)

and then use that coordinates (state) to send data to reverseGeocode which uses openstreetmap...
*/

// Helper function for reverse geocoding
async function reverseGeocode(lat: number, lng: number) {
    const url = `https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&zoom=18&addressdetails=1`;
    const response = await fetch(url, {
        headers: { 'User-Agent': 'MyApp/1.0' } // Required by OSM
    });
    const data = await response.json();

    const address = data.address;

    const parts = [
        address.road,
        address.suburb,
        address.city || address.town || address.village,
        address.state,
        address.country,
    ].filter(Boolean);

    return parts.join(', ');
}

const MatchSettingsScreen = () => {
    const { matchId } = useLocalSearchParams();
    const router = useRouter();
    const { session } = useAuth();

    const [fightSchedules, setFightSchedules] = useState<FightScheduleData[]>([]);
    const [matchData, setMatchData] = useState<{ user1Id: string; user2Id: string } | null>(null);
    const [loading, setLoading] = useState(true);
    const [refreshing, setRefreshing] = useState(false);
    
    const [showScheduleModal, setShowScheduleModal] = useState(false);
    const [showCancelModal, setShowCancelModal] = useState(false);
    const [showWaiverModal, setShowWaiverModal] = useState(false);
    const [showResultModal, setShowResultModal] = useState(false);
    const [selectedSchedule, setSelectedSchedule] = useState<FightScheduleData | null>(null);
    const [cancelReason, setCancelReason] = useState('');
    const [waiverData, setWaiverData] = useState({
        emergencyContactName: '',
        emergencyContactPhone: ''
    });

    const [listOfPublicFights, setListOfPublicFights] = useState<FightScheduleData[]>([]);
    const [showMapView, setShowMapView] = useState(false);

    // Location and map states
    //marker is just lat long:
    //but region is lat long deltas... idky
    const [marker, setMarker] = useState<{ latitude: number; longitude: number } | null>(null);
    const [region, setRegion] = useState<{
        latitude: number;
        longitude: number;
        latitudeDelta: number;
        longitudeDelta: number;
    } | null>(null);
    const [loadingLocation, setLoadingLocation] = useState(false);
    
    // Time picker states
    const [scheduledDateTime, setScheduledDateTime] = useState(new Date());
    const [showDatePicker, setShowDatePicker] = useState(false);
    const [showTimePicker, setShowTimePicker] = useState(false);
    
    // New schedule form data
    const [newSchedule, setNewSchedule] = useState({
        locationName: '',
        locationAddress: '',
    });

    useEffect(() => {
        loadData();
        initLocation();
    }, [matchId]);

    const initLocation = async () => {
        try {
            setLoadingLocation(true);
            // Request location permission
            let { status } = await Location.requestForegroundPermissionsAsync();
            if (status !== 'granted') {
                Alert.alert('Permission denied', 'Location permission is required to set fight location');
                return;
            }

            // if ok, get current position
            const loc = await Location.getCurrentPositionAsync({});
            const newRegion = {
                latitude: loc.coords.latitude,
                longitude: loc.coords.longitude,
                latitudeDelta: 0.01,
                longitudeDelta: 0.01,
            };

            setMarker({ latitude: loc.coords.latitude, longitude: loc.coords.longitude });
            setRegion(newRegion);
        } catch (error) {
            console.error('Error getting location:', error);
            Alert.alert('Error', 'Failed to get current location');
        } finally {
            setLoadingLocation(false);
        }
    };


    

    const loadData = async () => {
        try {
            setLoading(true);
            if (!session?.user || !matchId) return;

            await Promise.all([
                loadMatchDetails(),
                loadFightSchedules()
            ]);

            // Load match data to get user IDs
            try {
                const matchResponse = await getMatchesDataAPI();
                const currentMatch = matchResponse.data.find((m: any) => m.id === Number(matchId));
                if (currentMatch) {
                    setMatchData({
                        user1Id: currentMatch.user1.userId,
                        user2Id: currentMatch.user2.userId
                    });
                }
            } catch (error) {
                console.error('Error loading match data:', error);
            }

            const publicFights = await getAllPublicFights();
            setListOfPublicFights(publicFights.data);
        } catch (error) {
            console.error('Error loading data:', error);
            Alert.alert('Error', 'Failed to load data');
        } finally {
            setLoading(false);
        }
    };

    const loadMatchDetails = async () => {
        
    };

    const loadFightSchedules = async () => {
        try {
            if (!matchId) return;
            
            const response = await getFightSchedulesByMatchIdAPI(Number(matchId));
            setFightSchedules(response.data);
        } catch (error) {
            console.error('Error loading fight schedules:', error);
            Alert.alert('Error', 'Failed to load fight schedules');
        }
    };

    const onRefresh = async () => {
        setRefreshing(true);
        await loadFightSchedules();
        setRefreshing(false);
    };



    //final phase:
    const handleCreateSchedule = async () => {
        try {
            if (!matchId || !newSchedule.locationName || !marker) {
                Alert.alert('Error', 'Please fill in all required fields and select a location on the map');
                return;
            }

            // Validate that scheduled date is in the future
            const now = new Date();
            if (scheduledDateTime <= now) {
                Alert.alert('Invalid Date', 'Fight schedule must be set for a future date and time. Please select a later date.');
                return;
            }

            const scheduleData: CreateFightScheduleData = {
                matchId: Number(matchId),
                scheduledDateTime: scheduledDateTime.toISOString(),
                locationName: newSchedule.locationName,
                locationAddress: newSchedule.locationAddress,
                locationCoordinates: {
                    x: marker.longitude,
                    y: marker.latitude
                }
            };

            await createFightScheduleAPI(scheduleData);
            setShowScheduleModal(false);
            setNewSchedule({
                locationName: '',
                locationAddress: '',
            });
            setMarker(null);
            setScheduledDateTime(new Date());
            await loadFightSchedules();
            Alert.alert('Success', 'Fight schedule created successfully');
        } catch (error: any) {
            console.error('Error creating schedule:', error);
            
            // Check for specific error messages from backend
            let errorMessage = 'Failed to create fight schedule';
            
            if (error.response?.data?.message) {
                errorMessage = error.response.data.message;
            } else if (error.response?.data?.errors) {
                // Handle validation errors
                const errors = error.response.data.errors;
                const firstError = Object.values(errors)[0];
                if (Array.isArray(firstError) && firstError.length > 0) {
                    errorMessage = firstError[0];
                }
            } else if (error.message) {
                errorMessage = error.message;
            }
            
            // Check for specific constraint violations
            if (errorMessage.includes('CK_FightSchedule_FutureTime') || 
                errorMessage.toLowerCase().includes('future') ||
                errorMessage.toLowerCase().includes('scheduled date')) {
                Alert.alert('Invalid Date', 'The scheduled date and time must be in the future. Please select a later date.');
            } else {
                Alert.alert('Error', errorMessage);
            }
        }
    };

    const handleCancelSchedule = async () => {
        try {
            if (!selectedSchedule || !cancelReason.trim()) {
                Alert.alert('Error', 'Please provide a cancellation reason');
                return;
            }

            const cancelData: CancelFightScheduleData = {
                Reason: cancelReason,
                FightScheduleId: selectedSchedule.id
            };
            console.log(cancelData)
            await cancelFightScheduleAPI(cancelData);
            setShowCancelModal(false);
            setSelectedSchedule(null);
            setCancelReason('');
            await loadFightSchedules();
            Alert.alert('Success', 'Fight schedule cancelled successfully');
        } catch (error: any) {
            console.error('Error cancelling schedule:', error);
            
            // Check for specific error messages from backend
            let errorMessage = 'Failed to cancel fight schedule';
            
            if (error.response?.data?.message) {
                errorMessage = error.response.data.message;
            } else if (error.response?.data?.errors) {
                // Handle validation errors
                const errors = error.response.data.errors;
                const firstError = Object.values(errors)[0];
                if (Array.isArray(firstError) && firstError.length > 0) {
                    errorMessage = firstError[0];
                }
            } else if (error.message) {
                errorMessage = error.message;
            }
            
            Alert.alert('Error', errorMessage);
        }
    };

    const handleSubmitWaiver = async () => {
        try {
            if (!selectedSchedule) {
                Alert.alert('Error', 'No schedule selected');
                return;
            }

            if (!waiverData.emergencyContactName.trim() || !waiverData.emergencyContactPhone.trim()) {
                Alert.alert('Error', 'Please fill in all waiver fields');
                return;
            }

            const confirmData: ConfirmFightData = {
                fightScheduleId: selectedSchedule.id,
                safetyWaiver: {
                    emergencyContactName: waiverData.emergencyContactName,
                    emergencyContactPhone: waiverData.emergencyContactPhone
                }
            };

            await confirmFightScheduleAPI(confirmData);
            setShowWaiverModal(false);
            setSelectedSchedule(null);
            setWaiverData({
                emergencyContactName: '',
                emergencyContactPhone: ''
            });
            await loadFightSchedules();
            Alert.alert('Success', 'Safety waiver submitted successfully. Fight will be confirmed when both participants submit their waivers.');
        } catch (error: any) {
            console.error('Error submitting waiver:', error);
            
            let errorMessage = 'Failed to submit safety waiver';
            
            if (error.response?.data?.message) {
                errorMessage = error.response.data.message;
            } else if (error.response?.data?.errors) {
                const errors = error.response.data.errors;
                const firstError = Object.values(errors)[0];
                if (Array.isArray(firstError) && firstError.length > 0) {
                    errorMessage = firstError[0];
                }
            } else if (error.message) {
                errorMessage = error.message;
            }
            
            Alert.alert('Error', errorMessage);
        }
    };

    const handleUploadGif = async (fightScheduleId: number) => {
        try {
            // Pick GIF file
            const result = await DocumentPicker.getDocumentAsync({
                type: 'image/gif',
                copyToCacheDirectory: true,
            });

            if (result.canceled) {
                return;
            }

            const file = result.assets[0];

            // Create FormData
            const formData = new FormData();
            formData.append('file', {
                uri: file.uri,
                type: 'image/gif',
                name: file.name || 'fight.gif',
            } as any);

            // Upload
            const response = await uploadFightGifAPI(fightScheduleId, formData);
            
            if (response.data) {
                Alert.alert('Success', 'Fight GIF uploaded successfully!');
                await loadFightSchedules(); // Refresh list
            }
        } catch (error: any) {
            console.error('Error uploading GIF:', error);
            const errorMsg = error.response?.data?.message || 'Failed to upload GIF';
            Alert.alert('Error', errorMsg);
        }
    };

    const handleMapPress = async (coordinate: { latitude: number; longitude: number }) => {
        setMarker(coordinate);
        console.log(coordinate)
        
        try {
            setLoadingLocation(true);
            const address = await reverseGeocode(coordinate.latitude, coordinate.longitude);
            setNewSchedule(prev => ({
                ...prev,
                locationAddress: address
            }));
        } catch (error) {
            console.error('Error getting address:', error);
            Alert.alert('Error', 'Failed to get address for selected location');
        } finally {
            setLoadingLocation(false);
        }
    };

    const formatDateTime = (dateTimeString: string) => {
        const date = new Date(dateTimeString);
        return date.toLocaleString();
    };

    const formatDateTimeFromDate = (date: Date) => {
        return date.toLocaleString();
    };

    const getStatusColor = (status: string) => {
        switch (status.toLowerCase()) {
            case 'scheduled':
                return '#4CAF50';
            case 'completed':
                return '#2196F3';
            case 'canceled':
                return '#F44336';
            default:
                return '#9E9E9E';
        }
    };

    const renderScheduleItem = ({ item }: { item: FightScheduleData }) => (
        <TouchableOpacity 
            style={styles.scheduleItem}
            onPress={() => setSelectedSchedule(item)}
        >
            <View style={styles.scheduleHeader}>
                <Text style={styles.scheduleName}>{item.locationName}</Text>
                <View style={[styles.statusBadge, { backgroundColor: getStatusColor(item.status) }]}>
                    <Text style={styles.statusText}>{item.status.toUpperCase()}</Text>
                </View>
            </View>
            
            <Text style={styles.scheduleDateTime}>{formatDateTime(item.scheduledDateTime)}</Text>
            <Text style={styles.scheduleAddress}>{item.locationAddress}</Text>
            
            {/* Show waiver status */}
            <View style={styles.waiverStatusContainer}>
                <View style={styles.waiverStatus}>
                    <FontAwesome 
                        name={item.isSafetyWaiverAcceptedByUser1 ? "check-circle" : "circle-o"} 
                        size={16} 
                        color={item.isSafetyWaiverAcceptedByUser1 ? "#4CAF50" : "#999"} 
                    />
                    <Text style={styles.waiverStatusText}>User 1 Waiver</Text>
                </View>
                <View style={styles.waiverStatus}>
                    <FontAwesome 
                        name={item.isSafetyWaiverAcceptedByUser2 ? "check-circle" : "circle-o"} 
                        size={16} 
                        color={item.isSafetyWaiverAcceptedByUser2 ? "#4CAF50" : "#999"} 
                    />
                    <Text style={styles.waiverStatusText}>User 2 Waiver</Text>
                </View>
            </View>
            
            {item.status === 'scheduled' && (
                <View style={styles.actionButtonsContainer}>
                    <TouchableOpacity 
                        style={styles.waiverButton}
                        onPress={() => {
                            setSelectedSchedule(item);
                            setShowWaiverModal(true);
                        }}
                    >
                        <FontAwesome name="file-text-o" size={14} color="#fff" />
                        <Text style={styles.waiverButtonText}>Submit Waiver</Text>
                    </TouchableOpacity>
                    <TouchableOpacity 
                        style={styles.cancelButton}
                        onPress={() => {
                            setSelectedSchedule(item);
                            setShowCancelModal(true);
                        }}
                    >
                        <Text style={styles.cancelButtonText}>Cancel</Text>
                    </TouchableOpacity>
                </View>
            )}

            {item.status === 'confirmed' && (
                <View style={styles.actionButtonsContainer}>
                    <TouchableOpacity 
                        style={styles.completeFightButton}
                        onPress={() => {
                            setSelectedSchedule(item);
                            setShowResultModal(true);
                        }}
                    >
                        <FontAwesome name="trophy" size={14} color="#fff" />
                        <Text style={styles.completeFightButtonText}>Complete Fight</Text>
                    </TouchableOpacity>
                    <TouchableOpacity 
                        style={styles.cancelButton}
                        onPress={() => {
                            setSelectedSchedule(item);
                            setShowCancelModal(true);
                        }}
                    >
                        <Text style={styles.cancelButtonText}>Cancel</Text>
                    </TouchableOpacity>
                </View>
            )}

            {item.status === 'completed' && (
                <View style={styles.actionButtonsContainer}>
                    <TouchableOpacity 
                        style={styles.uploadGifButton}
                        onPress={() => handleUploadGif(item.id)}
                    >
                        <FontAwesome name="film" size={14} color="#fff" />
                        <Text style={styles.uploadGifButtonText}>
                            {item.fightGifUrl ? 'Update GIF' : 'Upload GIF'}
                        </Text>
                    </TouchableOpacity>
                    {item.fightGifUrl && (
                        <View style={styles.gifIndicator}>
                            <FontAwesome name="check-circle" size={14} color="#4CAF50" />
                            <Text style={styles.gifIndicatorText}>GIF Uploaded</Text>
                        </View>
                    )}
                </View>
            )}
        </TouchableOpacity>
    );

    if (loading) {
        return (
            <View style={styles.loaderContainer}>
                <ActivityIndicator size="large" color="#007AFF" />
                <Text style={styles.loadingText}>Loading...</Text>
            </View>
        );
    }

    return (
        <View style={styles.container}>
            {/* Header */}
            <View style={styles.header}>
                <TouchableOpacity 
                    style={styles.backButton}
                    onPress={() => router.back()}
                >
                    <FontAwesome name="arrow-left" size={24} color="#333" />
                </TouchableOpacity>
                <Text style={styles.headerTitle}>Fight Schedules</Text>
                <View style={styles.headerButtons}>
                    <TouchableOpacity 
                        style={styles.mapToggleButton}
                        onPress={() => setShowMapView(!showMapView)}
                    >
                        <FontAwesome name={showMapView ? "list" : "map"} size={20} color="#007AFF" />
                    </TouchableOpacity>
                    <TouchableOpacity 
                        style={styles.addButton}
                        onPress={() => setShowScheduleModal(true)}
                    >
                        <FontAwesome name="plus" size={24} color="#007AFF" />
                    </TouchableOpacity>
                </View>
            </View>






            

            {/* MAP PUBLIC FIGHTS */}
            {showMapView ? (
                <View style={styles.mapViewContainer}>
                    <Text style={styles.mapViewTitle}>All Public Fights Map</Text>
                    <View style={styles.publicMapContainer}>
                        <PublicFightsMap 
                            fights={listOfPublicFights} 
                            userLocation={region ? { latitude: region.latitude, longitude: region.longitude } : null}
                        />
                    </View>
                </View>
            ) : (
                // List View - Show user's fight schedules
                <FlatList
                    data={fightSchedules}
                    renderItem={renderScheduleItem}
                    keyExtractor={(item) => item.id.toString()}
                    contentContainerStyle={styles.listContainer}
                    refreshControl={
                        <RefreshControl
                            refreshing={refreshing}
                            onRefresh={onRefresh}
                            colors={['#007AFF']}
                        />
                    }
                    ListEmptyComponent={
                        <View style={styles.emptyContainer}>
                            <FontAwesome name="calendar" size={64} color="#ccc" />
                            <Text style={styles.emptyText}>No fight schedules yet</Text>
                            <Text style={styles.emptySubText}>Tap the + button to create one</Text>
                        </View>
                    }
                />
            )}

            {/* Create Schedule Modal */}
            <Modal
                visible={showScheduleModal}
                animationType="slide"
                transparent={true}
                onRequestClose={() => setShowScheduleModal(false)}
            >
                <View style={styles.modalOverlay}>
                    <View style={styles.modalContent}>
                        <View style={styles.modalHeader}>
                            <Text style={styles.modalTitle}>Schedule New Fight</Text>
                            <TouchableOpacity onPress={() => setShowScheduleModal(false)}>
                                <FontAwesome name="times" size={24} color="#333" />
                            </TouchableOpacity>
                        </View>

                        <ScrollView 
                            style={styles.modalBody}
                            contentContainerStyle={{ paddingBottom: 20 }}
                            showsVerticalScrollIndicator={true}
                        >
                            <Text style={styles.inputLabel}>Date & Time *</Text>
                            <View style={styles.dateTimeContainer}>
                                <TouchableOpacity 
                                    style={styles.dateTimeButton}
                                    onPress={() => setShowDatePicker(true)}
                                >
                                    <FontAwesome name="calendar" size={16} color="#007AFF" />
                                    <Text style={styles.dateTimeText}>
                                        {scheduledDateTime.toDateString()}
                                    </Text>
                                </TouchableOpacity>
                                
                                <TouchableOpacity 
                                    style={styles.dateTimeButton}
                                    onPress={() => setShowTimePicker(true)}
                                >
                                    <FontAwesome name="clock-o" size={16} color="#007AFF" />
                                    <Text style={styles.dateTimeText}>
                                        {scheduledDateTime.toLocaleTimeString()}
                                    </Text>
                                </TouchableOpacity>
                            </View>

                            {showDatePicker && (
                                <DateTimePicker
                                    value={scheduledDateTime}
                                    mode="date"
                                    display="default"
                                    onChange={(event, selectedDate) => {
                                        setShowDatePicker(false);
                                        if (selectedDate) {
                                            setScheduledDateTime(selectedDate);
                                        }
                                    }}
                                />
                            )}

                            {showTimePicker && (
                                <DateTimePicker
                                    value={scheduledDateTime}
                                    mode="time"
                                    display="default"
                                    onChange={(event, selectedTime) => {
                                        setShowTimePicker(false);
                                        if (selectedTime) {
                                            setScheduledDateTime(selectedTime);
                                        }
                                    }}
                                />
                            )}

                            <Text style={styles.inputLabel}>Specific Location Name *</Text>
                            <TextInput
                                style={styles.textInput}
                                placeholder="Downtown Boxing Gym"
                                value={newSchedule.locationName}
                                onChangeText={(text) => setNewSchedule(prev => ({ ...prev, locationName: text }))}
                            />

                            <Text style={styles.inputLabel}>Select Location on Map *</Text>
                            <Text style={styles.helpText}>Tap on the map to select fight location</Text>
                            
                            <View style={styles.mapContainer}>
                                <InteractiveMapPicker 
                                    region={region}
                                    marker={marker}
                                    onMapPress={handleMapPress}
                                />
                            </View>

                            {/* Fallback manual coordinate input if map doesn't work */}
                            <Text style={styles.inputLabel}>Or Enter Coordinates Manually</Text>
                            <View style={styles.coordinateContainer}>
                                <TextInput
                                    style={[styles.textInput, styles.coordinateInput]}
                                    placeholder="Latitude"
                                    value={marker?.latitude.toString() || ''}
                                    onChangeText={(text) => {
                                        const lat = parseFloat(text);
                                        if (!isNaN(lat) && marker) {
                                            setMarker({ ...marker, latitude: lat });
                                        } else if (!isNaN(lat)) {
                                            setMarker({ latitude: lat, longitude: 0 });
                                        }
                                    }}
                                    keyboardType="numeric"
                                />
                                <TextInput
                                    style={[styles.textInput, styles.coordinateInput]}
                                    placeholder="Longitude"
                                    value={marker?.longitude.toString() || ''}
                                    onChangeText={(text) => {
                                        const lng = parseFloat(text);
                                        if (!isNaN(lng) && marker) {
                                            setMarker({ ...marker, longitude: lng });
                                        } else if (!isNaN(lng)) {
                                            setMarker({ latitude: 0, longitude: lng });
                                        }
                                    }}
                                    keyboardType="numeric"
                                />
                            </View>

                            {marker && (
                                <TouchableOpacity 
                                    style={styles.getAddressButton}
                                    onPress={async () => {
                                        if (marker) {
                                            try {
                                                setLoadingLocation(true);
                                                const address = await reverseGeocode(marker.latitude, marker.longitude);
                                                setNewSchedule(prev => ({
                                                    ...prev,
                                                    locationAddress: address
                                                }));
                                            } catch (error) {
                                                console.error('Error getting address:', error);
                                                Alert.alert('Error', 'Failed to get address for coordinates');
                                            } finally {
                                                setLoadingLocation(false);
                                            }
                                        }
                                    }}
                                >
                                    <FontAwesome name="map-marker" size={16} color="#007AFF" />
                                    <Text style={styles.getAddressButtonText}>Get Address from Coordinates</Text>
                                </TouchableOpacity>
                            )}

                            <Text style={styles.inputLabel}>Location Address</Text>
                            <TextInput
                                style={[styles.textInput, styles.addressInput]}
                                placeholder="Address will be auto-filled when you select location"
                                value={newSchedule.locationAddress}
                                onChangeText={(text) => setNewSchedule(prev => ({ ...prev, locationAddress: text }))}
                                multiline
                                numberOfLines={3}
                            />

                            {loadingLocation && (
                                <View style={styles.loadingContainer}>
                                    <ActivityIndicator size="small" color="#007AFF" />
                                    <Text style={styles.loadingText}>Getting location details...</Text>
                                </View>
                            )}
                        </ScrollView>

                        <View style={styles.modalButtons}>
                            <TouchableOpacity 
                                style={[styles.modalButton, styles.cancelModalButton]}
                                onPress={() => setShowScheduleModal(false)}
                            >
                                <Text style={styles.cancelModalButtonText}>Cancel</Text>
                            </TouchableOpacity>


                            
                            <TouchableOpacity 
                                style={[styles.modalButton, styles.createButton]}
                                onPress={handleCreateSchedule}
                            >
                                <Text style={styles.createButtonText}>ADD</Text>
                            </TouchableOpacity>
                        </View>
                    </View>
                </View>
            </Modal>

            {/* Cancel Schedule Modal */}
            <Modal
                visible={showCancelModal}
                animationType="slide"
                transparent={true}
                onRequestClose={() => setShowCancelModal(false)}
            >
                <View style={styles.modalOverlay}>
                    <View style={styles.modalContent}>
                        <Text style={styles.modalTitle}>Cancel Fight Schedule</Text>
                        <Text style={styles.modalText}>
                            Are you sure you want to cancel this fight schedule?
                        </Text>
                        
                        <Text style={styles.inputLabel}>Cancellation Reason *</Text>
                        <TextInput
                            style={[styles.textInput, styles.textArea]}
                            placeholder="Please provide a reason for cancellation..."
                            value={cancelReason}
                            onChangeText={setCancelReason}
                            multiline={true}
                            numberOfLines={4}
                        />

                        <View style={styles.modalButtons}>
                            <TouchableOpacity 
                                style={[styles.modalButton, styles.cancelModalButton]}
                                onPress={() => {
                                    setShowCancelModal(false);
                                    setSelectedSchedule(null);
                                    setCancelReason('');
                                }}
                            >
                                <Text style={styles.cancelModalButtonText}>Back</Text>
                            </TouchableOpacity>
                            <TouchableOpacity 
                                style={[styles.modalButton, styles.deleteButton]}
                                onPress={handleCancelSchedule}
                            >
                                <Text style={styles.deleteButtonText}>Cancel Schedule</Text>
                            </TouchableOpacity>
                        </View>
                    </View>
                </View>
            </Modal>

            {/* Submit Waiver Modal */}
            <Modal
                visible={showWaiverModal}
                animationType="slide"
                transparent={true}
                onRequestClose={() => setShowWaiverModal(false)}
            >
                <View style={styles.modalOverlay}>
                    <View style={styles.modalContent}>
                        <View style={styles.modalHeader}>
                            <Text style={styles.modalTitle}>Submit Safety Waiver</Text>
                            <TouchableOpacity onPress={() => setShowWaiverModal(false)}>
                                <FontAwesome name="times" size={24} color="#333" />
                            </TouchableOpacity>
                        </View>

                        <ScrollView 
                            style={styles.modalBody}
                            contentContainerStyle={{ paddingBottom: 20 }}
                        >
                            <Text style={styles.modalText}>
                                Please provide emergency contact information for this fight.
                            </Text>
                            
                            <Text style={styles.inputLabel}>Emergency Contact Name *</Text>
                            <TextInput
                                style={styles.textInput}
                                placeholder="John Smith"
                                value={waiverData.emergencyContactName}
                                onChangeText={(text) => setWaiverData(prev => ({ ...prev, emergencyContactName: text }))}
                            />

                            <Text style={styles.inputLabel}>Emergency Contact Phone *</Text>
                            <TextInput
                                style={styles.textInput}
                                placeholder="+1-555-123-4567"
                                value={waiverData.emergencyContactPhone}
                                onChangeText={(text) => setWaiverData(prev => ({ ...prev, emergencyContactPhone: text }))}
                                keyboardType="phone-pad"
                            />

                            <View style={styles.waiverInfoBox}>
                                <FontAwesome name="info-circle" size={16} color="#007AFF" />
                                <Text style={styles.waiverInfoText}>
                                    Once both participants submit their safety waivers, the fight status will change to "confirmed".
                                </Text>
                            </View>
                        </ScrollView>

                        <View style={styles.modalButtons}>
                            <TouchableOpacity 
                                style={[styles.modalButton, styles.cancelModalButton]}
                                onPress={() => {
                                    setShowWaiverModal(false);
                                    setSelectedSchedule(null);
                                    setWaiverData({
                                        emergencyContactName: '',
                                        emergencyContactPhone: ''
                                    });
                                }}
                            >
                                <Text style={styles.cancelModalButtonText}>Cancel</Text>
                            </TouchableOpacity>
                            <TouchableOpacity 
                                style={[styles.modalButton, styles.createButton]}
                                onPress={handleSubmitWaiver}
                            >
                                <Text style={styles.createButtonText}>Submit Waiver</Text>
                            </TouchableOpacity>
                        </View>
                    </View>
                </View>
            </Modal>

            {/* Fight Result Modal */}
            {selectedSchedule && matchData && (
                <FightResultModal
                    visible={showResultModal}
                    onClose={() => {
                        setShowResultModal(false);
                        setSelectedSchedule(null);
                    }}
                    fight={selectedSchedule}
                    currentUserId={session?.user?.userId || ''}
                    matchUserId1={matchData.user1Id}
                    matchUserId2={matchData.user2Id}
                    onSuccess={async () => {
                        await loadFightSchedules();
                    }}
                />
            )}
        </View>
    );
};

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#f5f5f5',
    },
    header: {
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: 15,
        backgroundColor: '#fff',
        borderBottomWidth: 1,
        borderBottomColor: '#eee',
    },
    backButton: {
        padding: 5,
    },
    headerTitle: {
        fontSize: 18,
        fontWeight: 'bold',
        color: '#333',
        flex: 1,
        textAlign: 'center',
    },
    addButton: {
        padding: 5,
    },
    headerButtons: {
        flexDirection: 'row',
        alignItems: 'center',
    },
    mapToggleButton: {
        padding: 5,
        marginRight: 10,
    },
    listContainer: {
        padding: 15,
    },
    scheduleItem: {
        backgroundColor: '#fff',
        borderRadius: 12,
        padding: 15,
        marginBottom: 15,
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 2 },
        shadowOpacity: 0.1,
        shadowRadius: 4,
        elevation: 3,
    },
    scheduleHeader: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: 8,
    },
    scheduleName: {
        fontSize: 18,
        fontWeight: 'bold',
        color: '#333',
        flex: 1,
    },
    statusBadge: {
        paddingHorizontal: 8,
        paddingVertical: 4,
        borderRadius: 12,
    },
    statusText: {
        color: '#fff',
        fontSize: 12,
        fontWeight: 'bold',
    },
    scheduleDateTime: {
        fontSize: 16,
        color: '#666',
        marginBottom: 4,
    },
    scheduleAddress: {
        fontSize: 14,
        color: '#999',
        marginBottom: 10,
    },
    actionButtonsContainer: {
        flexDirection: 'row',
        gap: 10,
        marginTop: 10,
    },
    waiverButton: {
        backgroundColor: '#007AFF',
        paddingVertical: 8,
        paddingHorizontal: 16,
        borderRadius: 6,
        flexDirection: 'row',
        alignItems: 'center',
        gap: 6,
        flex: 1,
        justifyContent: 'center',
    },
    waiverButtonText: {
        color: '#fff',
        fontSize: 14,
        fontWeight: '600',
    },
    completeFightButton: {
        backgroundColor: '#4CAF50',
        paddingVertical: 8,
        paddingHorizontal: 16,
        borderRadius: 6,
        flexDirection: 'row',
        alignItems: 'center',
        gap: 6,
        flex: 1,
        justifyContent: 'center',
    },
    completeFightButtonText: {
        color: '#fff',
        fontSize: 14,
        fontWeight: '600',
    },
    cancelButton: {
        backgroundColor: '#FF3B30',
        paddingVertical: 8,
        paddingHorizontal: 16,
        borderRadius: 6,
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
    },
    cancelButtonText: {
        color: '#fff',
        fontSize: 14,
        fontWeight: '600',
    },
    uploadGifButton: {
        backgroundColor: '#9C27B0',
        paddingVertical: 8,
        paddingHorizontal: 16,
        borderRadius: 6,
        flexDirection: 'row',
        alignItems: 'center',
        gap: 6,
        flex: 1,
        justifyContent: 'center',
    },
    uploadGifButtonText: {
        color: '#fff',
        fontSize: 14,
        fontWeight: '600',
    },
    gifIndicator: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: 6,
        paddingHorizontal: 12,
        paddingVertical: 8,
        backgroundColor: '#E8F5E9',
        borderRadius: 6,
    },
    gifIndicatorText: {
        color: '#4CAF50',
        fontSize: 12,
        fontWeight: '600',
    },
    waiverStatusContainer: {
        flexDirection: 'row',
        gap: 15,
        marginTop: 10,
        paddingTop: 10,
        borderTopWidth: 1,
        borderTopColor: '#eee',
    },
    waiverStatus: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: 6,
    },
    waiverStatusText: {
        fontSize: 13,
        color: '#666',
    },
    waiverInfoBox: {
        flexDirection: 'row',
        backgroundColor: '#E3F2FD',
        padding: 12,
        borderRadius: 8,
        marginTop: 15,
        gap: 10,
    },
    waiverInfoText: {
        flex: 1,
        fontSize: 14,
        color: '#1976D2',
        lineHeight: 20,
    },
    emptyContainer: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
        paddingTop: 100,
    },
    emptyText: {
        fontSize: 18,
        color: '#666',
        marginTop: 15,
        fontWeight: '600',
    },
    emptySubText: {
        fontSize: 14,
        color: '#999',
        marginTop: 5,
    },
    loaderContainer: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
    },
    loadingText: {
        marginTop: 10,
        fontSize: 16,
        color: '#666',
    },
    modalOverlay: {
        flex: 1,
        backgroundColor: 'rgba(0,0,0,0.5)',
        justifyContent: 'center',
        alignItems: 'center',
    },
    modalContent: {
        backgroundColor: '#fff',
        margin: 20,
        borderRadius: 12,
        width: '90%',
        maxHeight: '90%',
        flex: 1,
    },
    modalHeader: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
        padding: 20,
        borderBottomWidth: 1,
        borderBottomColor: '#eee',
    },
    modalTitle: {
        fontSize: 18,
        fontWeight: 'bold',
        color: '#333',
    },
    modalBody: {
        flex: 1,
        padding: 20,
    },
    modalText: {
        fontSize: 16,
        color: '#666',
        textAlign: 'center',
        margin: 20,
    },
    inputLabel: {
        fontSize: 16,
        fontWeight: '600',
        color: '#333',
        marginBottom: 8,
        marginTop: 15,
    },
    textInput: {
        borderWidth: 1,
        borderColor: '#ddd',
        borderRadius: 8,
        padding: 12,
        fontSize: 16,
        backgroundColor: '#fff',
    },
    textArea: {
        minHeight: 80,
        textAlignVertical: 'top',
    },
    addressInput: {
        minHeight: 60,
        maxHeight: 80,
        textAlignVertical: 'top',
    },
    modalButtons: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        padding: 20,
        borderTopWidth: 1,
        borderTopColor: '#eee',
    },
    modalButton: {
        flex: 1,
        paddingVertical: 12,
        borderRadius: 8,
        alignItems: 'center',
    },
    cancelModalButton: {
        backgroundColor: '#f0f0f0',
        marginRight: 10,
    },
    cancelModalButtonText: {
        color: '#333',
        fontSize: 16,
        fontWeight: '600',
    },
    createButton: {
        backgroundColor: '#007AFF',
        marginLeft: 10,
    },
    createButtonText: {
        color: '#fff',
        fontSize: 16,
        fontWeight: '600',
    },
    deleteButton: {
        backgroundColor: '#FF3B30',
        marginLeft: 10,
    },
    deleteButtonText: {
        color: '#fff',
        fontSize: 16,
        fontWeight: '600',
    },
    dateTimeContainer: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        marginBottom: 15,
    },
    dateTimeButton: {
        flexDirection: 'row',
        alignItems: 'center',
        backgroundColor: '#f0f0f0',
        padding: 12,
        borderRadius: 8,
        flex: 0.48,
    },
    dateTimeText: {
        marginLeft: 8,
        fontSize: 14,
        color: '#333',
    },
    helpText: {
        fontSize: 14,
        color: '#666',
        marginBottom: 10,
        fontStyle: 'italic',
    },
    mapContainer: {
        height: 250,
        marginVertical: 10,
        borderRadius: 8,
        overflow: 'hidden',
        borderWidth: 1,
        borderColor: '#ddd',
    },
    map: {
        flex: 1,
    },
    mapLoader: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: '#f5f5f5',
    },
    loadingContainer: {
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'center',
        padding: 10,
    },
    coordinateContainer: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        marginBottom: 15,
    },
    coordinateInput: {
        flex: 0.48,
    },
    getAddressButton: {
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: '#f0f0f0',
        padding: 12,
        borderRadius: 8,
        marginBottom: 15,
    },
    getAddressButtonText: {
        marginLeft: 8,
        fontSize: 14,
        color: '#007AFF',
        fontWeight: '600',
    },
    markerContainer: {
        alignItems: 'center',
        justifyContent: 'center',
        width: 30,
        height: 30,
    },
    mapViewContainer: {
        flex: 1,
        padding: 15,
    },
    mapViewTitle: {
        fontSize: 18,
        fontWeight: 'bold',
        color: '#333',
        marginBottom: 15,
        textAlign: 'center',
    },
    publicMapContainer: {
        flex: 1,
        borderRadius: 12,
        overflow: 'hidden',
        borderWidth: 1,
        borderColor: '#ddd',
        backgroundColor: '#f5f5f5',
    },
});

export default MatchSettingsScreen;
