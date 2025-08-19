import { useRouter } from "expo-router";
import { Text, TouchableOpacity, View } from "react-native";

export default function MyPage() { //MyPage.tsx is under (tabs) 
    const router = useRouter();

  return (
    <View
      style={{
        flex: 1,
        justifyContent: "center",
        alignItems: "center",
      }}
    >
      <Text>myPagae in tab.</Text>


      <TouchableOpacity onPress={() => router.navigate('/about')}> {/* about.tsx is under APP (not tabs) */}
      <Text>Go to Test Screen</Text>
    </TouchableOpacity>


    </View>
  );
}
