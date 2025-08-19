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
            title: 'chat',
            tabBarIcon: ({ color }) => <FontAwesome size={28} name="snapchat" color={color} />,
          }}
        />







      </Tabs>



    </>
  );
}
