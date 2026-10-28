//https://supabase.com/docs/guides/getting-started/tutorials/with-expo-react-native?utm_source=youtube&utm_medium=social&utm_term=expo-react-native&utm_content=AE7dKIKMJy4&queryGroups=auth-store&auth-store=async-storage&queryGroups=database-method&database-method=dashboard#get-the-api-keys



import { Button, Input, Text } from '@rneui/themed'
import { useRouter } from 'expo-router'
import React, { useState } from 'react'
import { Alert, AppState, StyleSheet, View, TouchableOpacity } from 'react-native'
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
  const [isSignUp, setIsSignUp] = useState(false) // Toggle between sign in and sign up
  const [email, setEmail] = useState('jane.doe@example.com')
  const [password, setPassword] = useState('Password123!')
  const [confirmPassword, setConfirmPassword] = useState('Password123!')
  const [fullName, setFullName] = useState('Jane Doe')
  const [birthDate, setBirthDate] = useState('1990-05-14')
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
    } catch (error: any) {
      console.error("Login error:", error);
      const errorMessage = error.response?.data?.message || "Invalid email or password";
      const errorDetails = error.response?.data?.errors ? '\n' + error.response.data.errors.join('\n') : '';
      //       {
      //     "message": "Registration failed",
      //     "errors": [
      //         "Username 'AlterEgo2@gmail.com' is already taken.",
      //         "Email 'AlterEgo2@gmail.com' is already taken."
      //     ]
      // }
      Alert.alert("Login Failed", errorMessage + errorDetails);
    }
    setLoading(false)
  }

  async function signUpWithEmail() {
    setLoading(true)
    try {
      // Validate password match
      if (password !== confirmPassword) {
        Alert.alert("Registration Failed", "Passwords do not match");
        setLoading(false);
        return;
      }

      // Validate required fields
      if (!email || !password || !fullName || !birthDate) {
        Alert.alert("Registration Failed", "All fields are required");
        setLoading(false);
        return;
      }

      // Convert birthDate to ISO string format
      const birthDateISO = new Date(birthDate).toISOString();

      const response = await registerAPI({
        email: email,
        password: password,
        confirmPassword: confirmPassword,
        fullName: fullName,
        birthDate: birthDateISO
      });

      if (response.status === 200) {
        Alert.alert('Success', 'User registered successfully! You can now sign in.');
      }
    } catch (error: any) {
      console.error("Registration error:", error);
      const errorMessage = error.response?.data?.message || "Could not create account";
      const errorDetails = error.response?.data?.errors ? '\n' + error.response.data.errors.join('\n') : '';
      Alert.alert("Registration Failed", errorMessage + errorDetails);
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
      {/* Header with Title */}
      <View style={styles.headerContainer}>
        <Text h3 style={styles.headerText}>
          {isSignUp ? 'Create Account' : 'Welcome Back'}
        </Text>
        <Text style={styles.subtitleText}>
          {isSignUp ? 'Sign up to get started' : 'Sign in to continue'}
        </Text>
      </View>

      {/* Email Input */}
      <View style={styles.verticallySpaced}>
        <Input
          label="Email"
          leftIcon={{ type: 'font-awesome', name: 'envelope' }}
          onChangeText={(text) => setEmail(text)}
          value={email}
          placeholder="email@address.com"
          autoCapitalize={'none'}
        />
      </View>

      {/* Password Input */}
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

      {/* Sign Up Only Fields */}
      {isSignUp && (
        <>
          <View style={styles.verticallySpaced}>
            <Input
              label="Confirm Password"
              leftIcon={{ type: 'font-awesome', name: 'lock' }}
              onChangeText={(text) => setConfirmPassword(text)}
              value={confirmPassword}
              secureTextEntry={true}
              placeholder="Confirm Password"
              autoCapitalize={'none'}
            />
          </View>
          <View style={styles.verticallySpaced}>
            <Input
              label="Full Name"
              leftIcon={{ type: 'font-awesome', name: 'user' }}
              onChangeText={(text) => setFullName(text)}
              value={fullName}
              placeholder="John Doe"
              autoCapitalize={'words'}
            />
          </View>
          <View style={styles.verticallySpaced}>
            <Input
              label="Birth Date (YYYY-MM-DD)"
              leftIcon={{ type: 'font-awesome', name: 'calendar' }}
              onChangeText={(text) => setBirthDate(text)}
              value={birthDate}
              placeholder="1990-05-14"
              autoCapitalize={'none'}
            />
          </View>
        </>
      )}

      {/* Main Action Button */}
      <View style={[styles.verticallySpaced, styles.mt20]}>
        <Button
          title={isSignUp ? "Sign up" : "Sign in"}
          disabled={loading}
          onPress={isSignUp ? signUpWithEmail : signInWithEmail}
          buttonStyle={{ backgroundColor: '#2089dc', paddingVertical: 12 }}
        />
      </View>

      {/* Toggle Between Sign In/Sign Up */}
      <View style={styles.toggleContainer}>
        <Text style={styles.toggleText}>
          {isSignUp ? 'Already have an account?' : "Don't have an account?"}
        </Text>
        <TouchableOpacity onPress={() => setIsSignUp(!isSignUp)}>
          <Text style={styles.toggleLink}>
            {isSignUp ? ' Sign in' : ' Sign up'}
          </Text>
        </TouchableOpacity>
      </View>
    </View>
  )
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    padding: 20,
    backgroundColor: '#f5f5f5',
  },
  headerContainer: {
    alignItems: 'center',
    marginBottom: 30,
  },
  headerText: {
    fontWeight: 'bold',
    color: '#333',
  },
  subtitleText: {
    fontSize: 16,
    color: '#666',
    marginTop: 8,
  },
  verticallySpaced: {
    paddingTop: 4,
    paddingBottom: 4,
    alignSelf: 'stretch',
  },
  mt20: {
    marginTop: 20,
  },
  toggleContainer: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    marginTop: 20,
  },
  toggleText: {
    fontSize: 14,
    color: '#666',
  },
  toggleLink: {
    fontSize: 14,
    color: '#2089dc',
    fontWeight: 'bold',
  },
  divider: {
    height: 1,
    backgroundColor: '#ddd',
    marginVertical: 30,
  },
})