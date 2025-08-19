import CustomScreenHeader from "@/Components/CustomScreenHeader";
import { supabase } from "@/lib/supabase";
import { useRouter } from "expo-router";
import { Button, Text, View } from "react-native";

export default function Settings() {
  const router = useRouter()
  
  return (
    <>
    <CustomScreenHeader title="ABOUT ME" showBackButton={true} />
      <View
        style={{
          flex: 1,
          justifyContent: "center",
          alignItems: "center",
        }}
      >
        <Text>SETTINGS TAB.</Text>
        <View >
        <Button title="Sign Out" onPress={async () => {
          console.log("Sign OutED")
          await supabase.auth.signOut()
          router.replace('/Auth')
        }} />
      </View>
      </View>
    </>
  );
}
