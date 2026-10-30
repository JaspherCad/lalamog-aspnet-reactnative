import CustomHeader from '@/Components/CustomHeader';
import FontAwesome from '@expo/vector-icons/FontAwesome';
import { Tabs } from 'expo-router';

export default function TabLayout() {
  


  return (
    <>
      <CustomHeader />


      <Tabs screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: 'blue'


      }}>

        <Tabs.Screen
          name="index"
          options={{
            title: 'Matching',
            tabBarIcon: ({ color }) => <FontAwesome size={28} name="home" color={color} />,
          }}
        />
        <Tabs.Screen
          name="settingsTab"
          options={{
            title: 'Settings',
            tabBarIcon: ({ color }) => <FontAwesome size={28} name="cog" color={color} />,
          }}
        />

        <Tabs.Screen
          name="messages/index"
          options={{
            title: 'Chat',
            tabBarIcon: ({ color }) => <FontAwesome size={28} name="snapchat" color={color} />,
          }}
        />

        <Tabs.Screen
          name="myPage"
          options={{
            title: 'Public Fights',
            tabBarIcon: ({ color }) => <FontAwesome size={28} name="map-marker" color={color} />,
          }}
        />

        {/* Hide these from tab bar - they're sub-routes */}
        <Tabs.Screen
          name="messages/[matchId]"
          options={{
            href: null, // This hides it from the tab bar
          }}
        />
        <Tabs.Screen
          name="messages/matchSettings"
          options={{
            href: null, // This hides it from the tab bar
          }}
        />
        <Tabs.Screen
          name="messages/scheduleFight"
          options={{
            href: null, // This hides it from the tab bar
          }}
        />
        <Tabs.Screen
          name="indexOriginalDontTouch"
          options={{
            href: null, // This hides it from the tab bar
          }}
        />


      </Tabs>



    </>
  );
}
