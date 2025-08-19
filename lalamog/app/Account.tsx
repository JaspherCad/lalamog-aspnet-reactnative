import CustomScreenHeader from '@/Components/CustomScreenHeader'
import { useAuth } from '@/lib/AuthProvider'
import { useAvatarUpload } from '@/lib/AvatarUplaod'
import { updateProfileAPI } from '@/api/axiosInstance'
import DateTimePicker from '@react-native-community/datetimepicker'
import { Picker } from '@react-native-picker/picker'
import { Button, Input } from '@rneui/themed'
import * as Location from 'expo-location'
import { useRouter } from 'expo-router'
import { useEffect, useState } from 'react'
import { Alert, Image, ScrollView, StyleSheet, Text, View } from 'react-native'
import MapView, { Marker } from 'react-native-maps'
const API_BASE_URL = process.env.EXPO_PUBLIC_API_BASE_URL;

export default function Account() {
  const [loading, setLoading] = useState(true)
  const [username, setUsername] = useState('')
  // const [website, setWebsite] = useState('')
  const [fightingStyle, setFightingStyle] = useState('')
  const [experienceLevel, setExperienceLevel] = useState('')
  const [availability, setAvailability] = useState('')
  const [avatarUrl, setAvatarUrl] = useState('')
  const [bio, setBio] = useState('')
  const [fullName, setfullName] = useState('')

  ///useProfiles and useAvatarUpload



  //AVAILABILITY TIME:
  const [availableDays, setAvailableDays] = useState<string[]>([]);
  const [startTime, setStartTime] = useState(new Date());
  const [endTime, setEndTime] = useState(new Date());
  //toggler of time popup
  const [showStartPicker, setShowStartPicker] = useState(false)
  const [showEndPicker, setShowEndPicker] = useState(false)


  const router = useRouter()
  const [marker, setMarker] = useState<{ latitude: number; longitude: number } | null>(null)

  const { session, isLoading } = useAuth()

  const [region, setRegion] = useState<{
    latitude: number;
    longitude: number;
    latitudeDelta: number;
    longitudeDelta: number;
  } | null>(null);


  const { uploadAvatar, uploading } = useAvatarUpload()
  //uploadImageProfile axios

  useEffect(() => {
    if (session) {
      console.log("Session found");
      getProfile()
      initLocation()

    } else {
      console.log("No session found")
    }
  }, [session])



  async function initLocation() {
    let { status } = await Location.requestForegroundPermissionsAsync()
    if (status !== 'granted') return
    const loc = await Location.getCurrentPositionAsync({})



    const newRegion = {
      latitude: loc.coords.latitude,
      longitude: loc.coords.longitude,
      latitudeDelta: 0.01,
      longitudeDelta: 0.01,
    };



    setMarker({ latitude: loc.coords.latitude, longitude: loc.coords.longitude });
    setRegion(newRegion);

  }


  async function getProfile() {
    try {
      setLoading(true)
      if (!session?.user) throw new Error('No user on the session!')
      console.log("Session user:")
      const data = session?.user;
      const dataJwt = session?.jwt;


      console.log(`data: ${JSON.stringify(data)}`);
      console.log(`dataJwt: ${JSON.stringify(dataJwt)}`);
      const baseUrl = (API_BASE_URL ? API_BASE_URL.replace(/\/api$/, "") : "") || "http://localhost:5248";
      if (data) {
        setUsername(data.nickname ?? "");
        const imageUrl = `${baseUrl}${data.profilePictureUrl ?? ""}`;
        setAvatarUrl(data.profilePictureUrl ?? "");
        console.log("avatar_url: " + imageUrl);
        if (data.location) {
          const { x, y } = data.location
          setMarker({ latitude: y, longitude: x })
        }

        setBio(data.bio ?? "");
        setFightingStyle(data.fightingStyle ?? "");
        setExperienceLevel(data.experienceLevel?.toString() ?? "");
        // setAvailability(JSON.stringify(data.availability));

        if (data.availability) {
          setAvailableDays(data.availability.days || [])
          if (data.availability.time) {
            const [start, end] = data.availability.time.split('-')

            const now = new Date()
            const startDt = new Date(now)
            const endDt = new Date(now)
            const [startHours, startMinutes] = start.split(':').map(Number)
            const [endHours, endMinutes] = end.split(':').map(Number)
            startDt.setHours(startHours, startMinutes)
            endDt.setHours(endHours, endMinutes)
            setStartTime(startDt)
            setEndTime(endDt)
          }
        }


      }
    } catch (error) {
      if (error instanceof Error) {
        Alert.alert(error.message)
      }
      console.error('Error fetching profile:', error)
    } finally {
      setLoading(false)
    }
  }




  async function updateProfile() {
    try {
      setLoading(true)
      if (!session?.user) throw new Error('No user on the session!')


      if (!marker) {
        Alert.alert('Please pick a location on the map.')
        return
      }

      // Convert location coordinates to match backend format
      const locationDto = marker ? {
        x: marker.longitude,  // longitude
        y: marker.latitude    // latitude  
      } : undefined;  // Use undefined instead of null for TypeScript compatibility

      const availabilityDto = {
        days: availableDays,
        time: `${startTime.getHours()}:${startTime.getMinutes().toString().padStart(2, '0')}-${endTime.getHours()}:${endTime.getMinutes().toString().padStart(2, '0')}`
      };


      // personal note: when sending IMAGE URL just keep "/uploads/profile-images/f2603802-deb3-4377-8651-1134db7837a7/profile_1754030873.jpeg", 
      // dont include the base URL, it will be handled by the backend

      // Updated format to match ASP.NET backend ProfileDto structure
      const updates = {
        id: session?.user?.id,                    // Profile ID (GUID)
        userId: session?.user?.userId,            // User ID (GUID)
        nickname: username,                       // Updated field name
        bio: bio,
        location: locationDto,                    // LocationDto format (undefined if no marker)
        fightingStyle: fightingStyle,            // Updated field name
        experienceLevel: parseInt(experienceLevel) || 0,
        profilePictureUrl: avatarUrl,            // Updated field name
        createdAt: session?.user?.createdAt,     // Keep existing
        updatedAt: new Date().toISOString(),     // Current timestamp
        availability: availabilityDto            // Availability format
      };

      //verify first here if ok uncomment process
      console.log("Updates to send:", updates)


      const response = await updateProfileAPI(updates);
      console.log("Update response:", response.data);

      Alert.alert('Success', 'Profile updated successfully!');
      console.log("AWAIT DONE")


      router.replace('/Account')

    } catch (error) {
      if (error instanceof Error) {
        Alert.alert(error.message)
      }
    } finally {
      setLoading(false)
    }
  }


  useEffect(() => {
    if (marker) {
      console.log(marker)

    }
  }, [marker])


  const formatTime = (date: Date) =>
    `${date.getHours()}:${date.getMinutes().toString().padStart(2, '0')}`


  return (
    <ScrollView contentContainerStyle={styles.scrollContainer}>

      <View style={styles.container}>
        <CustomScreenHeader title="ABOUT ME" showBackButton={true} />

        <View style={[styles.verticallySpaced, styles.mt20]}>
          <Input label="Emails" value={session?.user?.userId} disabled />
        </View>

        <View style={styles.verticallySpaced}>
          <Input label="Username" value={username || ''} onChangeText={(text) => setUsername(text)} />
        </View>

        {/* <View style={styles.verticallySpaced}>
          <Input label="Website" value={website || ''} onChangeText={(text) => setWebsite(text)} />
        </View> */}

        <View style={styles.verticallySpaced}>
          <Input label="Bio" value={bio || ''} onChangeText={(text) => setBio(text)} />
        </View>

        <View style={styles.verticallySpaced}>
          <Input label="Fighting Style" value={fightingStyle || ''} onChangeText={(text) => setFightingStyle(text)} />
        </View>

        <View style={styles.verticallySpaced}>
          <Input label="Experience Level" disabled />
          <Picker
            selectedValue={experienceLevel}
            onValueChange={(itemValue) => setExperienceLevel(itemValue)}
          >
            <Picker.Item label="Select experience level" value="" />
            <Picker.Item label="Beginner" value="1" />
            <Picker.Item label="Intermediate" value="2" />
            <Picker.Item label="Expert" value="3" />
          </Picker>
        </View>


        <View style={styles.verticallySpaced}>
          <Text>Availability Days</Text>
          {['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'].map((day) => (
            <Button
              key={day}
              type={availableDays.includes(day) ? 'solid' : 'outline'}
              title={day}
              onPress={() => {
                setAvailableDays((prev) =>
                  prev.includes(day) ? prev.filter((d) => d !== day) : [...prev, day]
                )
              }}
              containerStyle={{ margin: 2 }}
            />
          ))}
        </View>

        <View style={styles.verticallySpaced}>
          <Text style={styles.label}>Start Time</Text>
          <Button onPress={() => setShowStartPicker(true)}>
            <Input
              value={formatTime(startTime)}
              editable={false}
              rightIcon={{ name: 'access-time', type: 'material' }}
            />
          </Button>
          {showStartPicker && (
            <DateTimePicker
              value={startTime}
              mode="time"
              display="default"
              onChange={(event, selectedDate) => {
                setShowStartPicker(false)
                if (selectedDate) {
                  setStartTime(selectedDate)
                }
              }}
            />
          )}
        </View>

        <View style={styles.verticallySpaced}>
          <Text style={styles.label}>End Time</Text>
          <Button onPress={() => setShowEndPicker(true)}>
            <Input
              value={formatTime(endTime)}
              editable={false}
              rightIcon={{ name: 'access-time', type: 'material' }}
            />
          </Button>
          {showEndPicker && (
            <DateTimePicker
              value={endTime}
              mode="time"
              display="default"
              onChange={(event, selectedDate) => {
                setShowEndPicker(false)
                if (selectedDate) {
                  setEndTime(selectedDate)
                }
              }}
            />
          )}
        </View>


        {/* Avatar Section */}
        <View style={[styles.verticallySpaced, styles.mt20]}>
          <Text style={styles.label}>Avatar</Text>
          {avatarUrl ? (
            <View style={styles.avatarContainer}>
              {/* {        const baseUrl = (API_BASE_URL ? API_BASE_URL.replace(/\/api$/, "") : "") || "http://localhost:5248";
            } */}
              <Image source={{ uri: `${(API_BASE_URL ? API_BASE_URL.replace(/\/api$/, "") : "") || "http://localhost:5248"}${avatarUrl}` }} style={styles.avatar} />
              <Button
                title={uploading ? 'Uploading...' : 'Change Avatar'}
                onPress={() => {
                  if (session?.user?.userId) {
                    uploadAvatar(session.user.userId, setAvatarUrl)
                    // idea is to use the setAvatarUrl inside the uploadAvatar function to update the avatarUrl state here. 
                  } else {
                    Alert.alert('Error', 'User not authenticated')
                  }
                }}
                disabled={uploading}
              />
            </View>
          ) : (
            <Button
              title={uploading ? 'Uploading...' : 'Change Avatar'}
              onPress={() => {
                if (session?.user?.userId) {
                  uploadAvatar(session.user.userId, setAvatarUrl)
                } else {
                  Alert.alert('Error', 'User not authenticated')
                }
              }}
              disabled={uploading}
            />
          )}
        </View>

        <View style={styles.mapContainer}>
          <MapView
            style={styles.map}
            region={region ?? {
              latitude: 0,
              longitude: 0,
              latitudeDelta: 0.01,
              longitudeDelta: 0.01,
            }}

            onPress={(e: {
              nativeEvent:
              {
                coordinate:
                {
                  latitude: number;
                  longitude: number
                }
              }
            }) => {
              setMarker(e.nativeEvent.coordinate)
              console.log(e.nativeEvent.coordinate)
            }
            }
          >
            {marker && <Marker coordinate={marker} />}
          </MapView>
        </View>

        <View style={[styles.verticallySpaced, styles.mt20]}>
          <Button
            title={loading ? 'Loading ...' : 'Update'}
            onPress={() => updateProfile()}
            disabled={loading}
          />
        </View>

        <View style={styles.verticallySpaced}>
          <Button title="Sign Out" onPress={async () => {
            console.log("Sign OutED")
            // await supabase.auth.signOut()
            router.replace('/Auth')
          }} />
        </View>
      </View>
    </ScrollView>

  )
}

const styles = StyleSheet.create({
  scrollContainer: {
    flexGrow: 1,
  },

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
  mapContainer: { height: 300, marginVertical: 16 },
  map: { flex: 1 },
  label: {
    fontWeight: 'bold',
    marginBottom: 4,
  },
  picker: {
    backgroundColor: '#f0f0f0',
  },
  avatarContainer: {
    alignItems: 'center',
    marginBottom: 16,
  },
  avatar: {
    width: 120,
    height: 120,
    borderRadius: 60,
    marginBottom: 8,
  },
  changeAvatarButton: {
    marginTop: 8,
    backgroundColor: '#f0f0f0',
    borderColor: '#ccc',
  },
})