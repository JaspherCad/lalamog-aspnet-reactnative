import CustomScreenHeader from "@/Components/CustomScreenHeader";
import { useAuth } from "@/lib/AuthProvider";
import { useRouter } from "expo-router";
import { useState } from "react";
import { Button, Text, View, Alert, ActivityIndicator, StyleSheet } from "react-native";

export default function Settings() {
  const router = useRouter()
  const { signOut } = useAuth()
  const [loading, setLoading] = useState(false)

  async function handleLogout() {
    setLoading(true)
    try {
      await signOut()
      Alert.alert('Success', 'Logged out successfully!')
      router.replace('/Auth')
    } catch (error) {
      console.error("Logout error:", error);
      Alert.alert("Logout Failed", "Could not log out");
    }
    setLoading(false)
  }
  
  return (
    <>
    <CustomScreenHeader title="SETTINGS" showBackButton={true} />
      <View style={styles.container}>
        <Text style={styles.title}>Settings</Text>
        <View style={styles.buttonContainer}>
          {loading ? (
            <ActivityIndicator size="large" color="#ff4444" />
          ) : (
            <Button title="Sign Out" onPress={handleLogout} color="#ff4444" />
          )}
        </View>
      </View>
    </>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    padding: 20,
  },
  title: {
    fontSize: 20,
    fontWeight: 'bold',
    marginBottom: 20,
  },
  buttonContainer: {
    width: '100%',
    maxWidth: 300,
  },
});
