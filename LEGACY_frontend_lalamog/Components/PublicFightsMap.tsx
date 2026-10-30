import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { WebView } from 'react-native-webview';
import FontAwesome from '@expo/vector-icons/FontAwesome';
import { FightScheduleData } from '@/api/axiosInstance';

interface PublicFightsMapProps {
    fights: FightScheduleData[];
    userLocation: {
        latitude: number;
        longitude: number;
    } | null;
}

const PublicFightsMap: React.FC<PublicFightsMapProps> = ({ fights, userLocation }) => {
    if (fights.length === 0) {
        return (
            <View style={styles.emptyContainer}>
                <FontAwesome name="map-o" size={64} color="#ccc" />
                <Text style={styles.emptyText}>No public fights to display</Text>
                <Text style={styles.emptySubText}>Public fights will appear here</Text>
            </View>
        );
    }

    const userLat = userLocation?.latitude || 0;
    const userLng = userLocation?.longitude || 0;
    const apiBaseUrl = process.env.EXPO_PUBLIC_API_BASE_URL || 'http://localhost:5000';
    // Remove /api suffix for static files (they're served from wwwroot at server root)
    const staticBaseUrl = apiBaseUrl.replace(/\/api\/?$/, '');

    // Debug logging
    console.log('PublicFightsMap - API Base URL:', apiBaseUrl);
    console.log('PublicFightsMap - Static Base URL:', staticBaseUrl);
    console.log('PublicFightsMap - Fights with GIFs:', fights.filter(f => f.fightGifUrl).map(f => ({
        name: f.locationName,
        status: f.status,
        gifUrl: f.fightGifUrl
    })));

    return (
        <WebView
            style={styles.map}
            onMessage={(event) => {
                console.log('WebView message:', event.nativeEvent.data);
            }}
            onError={(syntheticEvent) => {
                const { nativeEvent } = syntheticEvent;
                console.error('WebView error:', nativeEvent);
            }}
            source={{
                html: `
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset="utf-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
                    <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
                    <style>
                        body { margin: 0; padding: 0; font-family: Arial, sans-serif; }
                        #map { height: 100vh; width: 100vw; }
                    </style>
                </head>
                <body>
                    <div id="map"></div>
                    <script>
                        // Initialize map centered on user's current location
                        var fights = ${JSON.stringify(fights)};
                        var staticBaseUrl = "${staticBaseUrl}";
                        
                        console.log('WebView - Static Base URL:', staticBaseUrl);
                        
                        // region current location as map center
                        var userLat = ${userLat};
                        var userLng = ${userLng};
                        
                        var map = L.map('map').setView([userLat, userLng], 10);
                        
                        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                            attribution: '© OpenStreetMap contributors'
                        }).addTo(map);
                        
                        // Add markers for each fight
                        fights.forEach(function(fight) {
                            var status = fight.status.toLowerCase();
                            var color = status === 'scheduled' ? 'green' : 
                                       status === 'completed' ? 'blue' : 
                                       status === 'canceled' ? 'red' : 'gray';
                            
                            console.log('Fight:', fight.locationName, 'Status:', status, 'GIF URL:', fight.fightGifUrl);
                            
                            var gifHtml = '';
                            if (fight.fightGifUrl && status === 'completed') {
                                var fullGifUrl = staticBaseUrl + fight.fightGifUrl;
                                console.log('Full GIF URL:', fullGifUrl);
                                gifHtml = \`
                                    <div style="margin-top: 10px; text-align: center; border-top: 1px solid #eee; padding-top: 10px;">
                                        <p style="margin: 5px 0 10px 0; font-weight: bold; color: #9C27B0;">Fight Highlights</p>
                                        <img src="\${fullGifUrl}" alt="Fight GIF" 
                                            style="max-width: 250px; max-height: 250px; border-radius: 8px; border: 2px solid #9C27B0; cursor: pointer; display: block; margin: 0 auto;" 
                                            onclick="window.open('\${fullGifUrl}', '_blank')" 
                                            onload="console.log('GIF loaded successfully:', '\${fullGifUrl}');"
                                            onerror="console.error('Failed to load GIF:', '\${fullGifUrl}'); this.style.display='none'; this.nextElementSibling.textContent='Failed to load GIF - URL: \${fullGifUrl}';" />
                                        <p style="margin: 5px 0; font-size: 11px; color: #666; font-style: italic;">Click image to view full size</p>
                                    </div>
                                \`;
                            } else {
                                console.log('No GIF for this fight or not completed');
                            }

                            // Fight result details
                            var resultHtml = '';
                            if (status === 'completed' && fight.fightResult && fight.fightResult !== 'pending') {
                                var resultText = '';
                                var resultColor = '#4CAF50';
                                
                                if (fight.fightResult === 'user1_win') {
                                    resultText = 'User 1 Won';
                                } else if (fight.fightResult === 'user2_win') {
                                    resultText = 'User 2 Won';
                                } else if (fight.fightResult === 'draw') {
                                    resultText = 'Draw';
                                    resultColor = '#FF9800';
                                } else if (fight.fightResult === 'no_contest') {
                                    resultText = 'No Contest';
                                    resultColor = '#999';
                                }
                                
                                resultHtml = \`
                                    <div style="margin-top: 10px; padding: 10px; background: #f5f5f5; border-radius: 6px; border-left: 4px solid \${resultColor};">
                                        <p style="margin: 0 0 8px 0; font-weight: bold; color: \${resultColor}; font-size: 14px;">Fight Result</p>
                                        <p style="margin: 3px 0;"><strong>Outcome:</strong> \${resultText}</p>
                                        \${fight.winMethod ? \`<p style="margin: 3px 0;"><strong>Method:</strong> \${fight.winMethod}</p>\` : ''}
                                        \${fight.fightDurationMinutes ? \`<p style="margin: 3px 0;"><strong>Duration:</strong> \${fight.fightDurationMinutes} minutes</p>\` : ''}
                                        \${fight.resultNotes ? \`<p style="margin: 3px 0;"><strong>Notes:</strong> \${fight.resultNotes}</p>\` : ''}
                                    </div>
                                \`;
                            }

                            var marker = L.marker([fight.locationCoordinates.y, fight.locationCoordinates.x])
                                .addTo(map)
                                .bindPopup(\`
                                    <div style="min-width: 250px; max-width: 300px;">
                                        <h3 style="margin: 0 0 10px 0; color: \${color};">\${fight.locationName}</h3>
                                        <p style="margin: 5px 0;"><strong>Status:</strong> \${fight.status.toUpperCase()}</p>
                                        <p style="margin: 5px 0;"><strong>Date:</strong> \${new Date(fight.scheduledDateTime).toLocaleString()}</p>
                                        <p style="margin: 5px 0;"><strong>Address:</strong> \${fight.locationAddress}</p>
                                        \${fight.status === 'canceled' ? \`<p style="margin: 5px 0; color: red;"><strong>Reason:</strong> \${fight.cancellationReason}</p>\` : ''}
                                        \${resultHtml}
                                        \${gifHtml}
                                    </div>
                                \`, { maxWidth: 320 });
                        });
                    </script>
                </body>
                </html>
                `
            }}
            javaScriptEnabled={true}
            domStorageEnabled={true}
            startInLoadingState={true}
            scalesPageToFit={true}
        />
    );
};

const styles = StyleSheet.create({
    map: {
        flex: 1,
    },
    emptyContainer: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
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
});

export default PublicFightsMap;
