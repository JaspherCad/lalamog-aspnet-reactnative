import { AuthProvider } from '@/lib/AuthProvider';
import { Stack } from "expo-router";
import { SafeAreaView } from "react-native-safe-area-context";
import { MessageProvider } from '@/lib/MessageContext';

export default function RootLayout() {
  const user = false;
  return (

    <AuthProvider>
      <MessageProvider>

        <SafeAreaView style={{ flex: 1 }}>

          <Stack
            screenOptions={{
              headerStyle: { backgroundColor: 'papayawhip' },
              headerTintColor: '#333',
              headerShown: false

            }}
          >
            {user ?
              (<>
                <Stack.Screen
                  name="(tabs)"
                  options={{ headerShown: false }}
                />

                {/* <Stack.Screen
                name="(messages/[matchId]"
                options={{ 
                  headerShown: false, 
                  title:"Chat"}}
              /> */}





                <Stack.Screen
                  name="about"
                  options={{
                    title: 'ABOUT ME',
                    headerShown: true

                  }}
                />


                <Stack.Screen
                  name="settings"
                  options={{ title: 'SETTINGS' }}
                />
              </>)

              ://If USER false, auth endpoints

              (<>
                <Stack.Screen
                  name="login"
                  options={{ title: 'LOGIN PAGE' }}
                />
              </>)}



          </Stack>
        </SafeAreaView>
      </MessageProvider>

    </AuthProvider>
  )
}



