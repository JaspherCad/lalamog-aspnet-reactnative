import React from 'react';
import { View, Text, StyleSheet, ActivityIndicator } from 'react-native';
import { WebView } from 'react-native-webview';

interface InteractiveMapPickerProps {
    region: {
        latitude: number;
        longitude: number;
    } | null;
    marker: {
        latitude: number;
        longitude: number;
    } | null;
    onMapPress: (coordinates: { latitude: number; longitude: number }) => void;
}

const InteractiveMapPicker: React.FC<InteractiveMapPickerProps> = ({ 
    region, 
    marker, 
    onMapPress 
}) => {
    if (!region) {
        return (
            <View style={styles.mapLoader}>
                <ActivityIndicator size="large" color="#007AFF" />
                <Text>Loading map...</Text>
            </View>
        );
    }

    return (
        <WebView
            style={styles.map}
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
                        body { margin: 0; padding: 0; }
                        #map { height: 100vh; width: 100vw; }
                    </style>
                </head>
                <body>
                    <div id="map"></div>
                    <script>
                        var map = L.map('map').setView([${region.latitude}, ${region.longitude}], 14);
                        
                        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                            attribution: '© OpenStreetMap contributors'
                        }).addTo(map);
                        
                        var marker = null;
                        ${marker ? `
                        marker = L.marker([${marker.latitude}, ${marker.longitude}]).addTo(map)
                            .bindPopup('Fight Location')
                            .openPopup();
                        ` : ''}
                        
                        map.on('click', function(e) {
                            if (marker) {
                                map.removeLayer(marker);
                            }
                            marker = L.marker([e.latlng.lat, e.latlng.lng]).addTo(map)
                                .bindPopup('Fight Location')
                                .openPopup();
                            
                            // Send coordinates back to React Native
                            window.ReactNativeWebView.postMessage(JSON.stringify({
                                type: 'mapClick',
                                coordinates: {
                                    latitude: e.latlng.lat,
                                    longitude: e.latlng.lng
                                }
                            }));
                        });
                    </script>
                </body>
                </html>
                `
            }}
            onMessage={(event) => {
                try {
                    const data = JSON.parse(event.nativeEvent.data);
                    if (data.type === 'mapClick') {
                        onMapPress(data.coordinates);
                    }
                } catch (error) {
                    console.error('Error parsing WebView message:', error);
                }
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
    mapLoader: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: '#f5f5f5',
    },
});

export default InteractiveMapPicker;
