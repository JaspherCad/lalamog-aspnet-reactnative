    import React from 'react';
    import { View, Text, TouchableOpacity, StyleSheet } from 'react-native';
    import { useRouter } from 'expo-router';
    import FontAwesome from '@expo/vector-icons/FontAwesome';

    export default function CustomHeader() {
    const router = useRouter();
//USE FOR LAYOUT_(tabs)
    return (
        <View style={styles.header}>
        <TouchableOpacity onPress={() => router.push('/Account')}>
            <FontAwesome name="cog" size={24} color="black" />
        </TouchableOpacity>

        <TouchableOpacity onPress={() => router.push('/settings')}>
            <FontAwesome name="user" size={24} color="black" />
        </TouchableOpacity>

        <TouchableOpacity onPress={() => router.push('/settings')}>
            <Text style={styles.logo}>MyLogo</Text>
        </TouchableOpacity>
        </View>
    );
    }

    const styles = StyleSheet.create({
    header: {
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "space-around",
        paddingVertical: 10,
        backgroundColor: "white",
        borderBottomWidth: StyleSheet.hairlineWidth,
        borderBottomColor: "#ccc",
    },
    logo: {
        fontSize: 20,
        fontWeight: "bold",
    },
    });