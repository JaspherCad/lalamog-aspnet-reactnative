import CustomScreenHeader from "@/Components/CustomScreenHeader";
import { Text, View, Button, Alert } from "react-native";
import { connectionTest } from '@/api/axiosInstance'; 

export default function About() {
  const testConnection = async () => {
    try {
      const response = await connectionTest();
      console.log("Connection test response:", response);
      
      // Show success alert
      Alert.alert(
        "Success", 
        "Connection successful!", 
        [{ text: "OK" }]
      );
    } catch (error) {
      console.error("Error testing connection:", error);
      
      // Show error alert with more details
      Alert.alert(
        "Connection Failed", 
        "Could not connect to the server. Please ensure the backend is running and your network connection is working.", 
        [{ text: "OK" }]
      );
    }
  };

  return (
    <>
      <CustomScreenHeader title="ABOUT ME" showBackButton={true} />
      <View
        style={{
          flex: 1,
          justifyContent: "center",
          alignItems: "center",
          padding: 20,
        }}
      >
        <Text style={{ marginBottom: 20 }}>abouts.</Text>
        <Button 
          title="Test Connection" 
          onPress={testConnection} 
        />
      </View>
    </>
  );
}