//https://supabase.com/docs/guides/getting-started/tutorials/with-expo-react-native?utm_source=youtube&utm_medium=social&utm_term=expo-react-native&utm_content=AE7dKIKMJy4&queryGroups=auth-store&auth-store=async-storage&queryGroups=database-method&database-method=dashboard#get-the-api-keys



import { Button, Input } from '@rneui/themed'
import { useRouter } from 'expo-router'
import React, { useState } from 'react'
import { Alert, AppState, StyleSheet, View } from 'react-native'
import { supabase } from '../lib/supabase'
import { loginAPI, registerAPI } from '@/api/axiosInstance';
import { useAuth } from '@/lib/AuthProvider';


AppState.addEventListener('change', (state) => {
  if (state === 'active') {
    supabase.auth.startAutoRefresh()
  } else {
    supabase.auth.stopAutoRefresh()
  }
})

export default function Auth() {
  const router = useRouter()
  const { signInEmail, signOut } = useAuth() // Get signInEmail and signOut from Auth context
  const [email, setEmail] = useState('jane.doe@example.com')
  const [password, setPassword] = useState('Password123!')
  const [loading, setLoading] = useState(false)


  const geojson = `SRID=4326;POINT(120.9842 14.5995)`;


  async function signInWithEmail() {
    setLoading(true)
    try {
      const { data } = await signInEmail({
        email: email,
        password: password,
      })

      console.log("Login response:", data);
      
      if (data) {
        console.log("Session user:", data.user); // Access user profile
        console.log("Session JWT:", data.jwt); // Access JWT token
        router.replace('/(tabs)'); // Or wherever you want to navigate after login
      }
    } catch (error) {
      console.error("Login error:", error);
      Alert.alert("Login Failed", "Invalid email or password");
    }
    setLoading(false)
  }

  async function signUpWithEmail() {
    setLoading(true)
    try {
      const response = await registerAPI({
        email: email,
        password: password,
        confirmPassword: password,
        fullName: email.split('@')[0], // Use email prefix as name
        birthDate: new Date().toISOString()
      });

      if (response.status === 200) {
        Alert.alert('Success', 'User registered successfully! You can now sign in.');
      }
    } catch (error) {
      console.error("Registration error:", error);
      Alert.alert("Registration Failed", "Could not create account");
    }
    setLoading(false)
  }

  async function handleLogout() {
    setLoading(true)
    try {
      await signOut()
      Alert.alert('Success', 'Logged out successfully!')
    } catch (error) {
      console.error("Logout error:", error);
      Alert.alert("Logout Failed", "Could not log out");
    }
    setLoading(false)
  }





  async function seedUsers() {
    setLoading(true)
    const users = [
      { email: "kingBarou@gmail.com", password: "password123" },
 
      // //jairene@gmail "password123"
      // //joshua "password123"
      // //jeneth  OUTSIDER "password123"
      // //jane "password123"
      // //nikki "password123"
      // //kingbarou "password123"

     
    ]

    for (let u of users) {
      const { data: { session }, error } = await supabase.auth.signUp({
        email: u.email,
        password: u.password
      });

      if (error) {
        Alert.alert('Error seeding', error.message)
        break
      }
      console.log(`adding ${u.email} works!`)
    }

    Alert.alert('✅ Seed complete!')
    setLoading(false)
  }


  return (
    <View style={styles.container}>
      <View style={[styles.verticallySpaced, styles.mt20]}>
        <Input
          label="Email"
          leftIcon={{ type: 'font-awesome', name: 'envelope' }}
          onChangeText={(text) => setEmail(text)}
          value={email}
          placeholder="email@address.com"
          autoCapitalize={'none'}
        />
      </View>
      <View style={styles.verticallySpaced}>
        <Input
          label="Password"
          leftIcon={{ type: 'font-awesome', name: 'lock' }}
          onChangeText={(text) => setPassword(text)}
          value={password}
          secureTextEntry={true}
          placeholder="Password"
          autoCapitalize={'none'}
        />
      </View>
      <View style={[styles.verticallySpaced, styles.mt20]}>
        <Button title="Sign in" disabled={loading} onPress={() => signInWithEmail()} />
      </View>
      <View style={styles.verticallySpaced}>
        <Button title="Sign up" disabled={loading} onPress={() => signUpWithEmail()} />
      </View>
      <View style={styles.verticallySpaced}>
        <Button title="Logout" disabled={loading} onPress={() => handleLogout()} buttonStyle={{ backgroundColor: '#ff4444' }} />
      </View>


      <View style={styles.verticallySpaced}>
        <Button
          title="Generate Dummy Users"
          loading={loading}
          containerStyle={styles.mt20}
          onPress={() =>
            Alert.alert(
              'Generate Dummy Users?',
              'This will create test accounts in your dev DB.',
              [
                { text: 'Cancel', style: 'cancel' },
                { text: 'Go for it', onPress: seedUsers },
              ]
            )
          }
        />
      </View>
    </View>
  )
}

const styles = StyleSheet.create({
  container: {
    marginTop: 40,
    padding: 12,
  },
  verticallySpaced: {
    paddingTop: 4,
    paddingBottom: 4,
    alignSelf: 'stretch',
  },
  mt20: {
    marginTop: 20,
  },
})