import { decode } from 'base64-arraybuffer'
import * as FileSystem from 'expo-file-system'
import * as ImagePicker from 'expo-image-picker'

import { useState } from 'react'
import { Alert } from 'react-native'
import { uploadImageProfile } from '@/api/axiosInstance'



//SIMPLE GOAL (SUPASE APPROACH)
//to upload
//get userID (used for filepath), setAvatarUrl(customFunction from account.tsx)
//get image result from ImagePicker
//process the 'image' 
//use base64 to decode.. idk why just follow the tutorial
//set proper file extension
//const filePath = `${userId}/${Date.now()}.${fileExtension}`


//Upload to supabase + decode(base64)

//to retrieve...
// const { data } = supabase
//     .storage
//     .from('public-bucket')
//     .getPublicUrl('folder/avatar1.png')
//use the setAvatar from component ---- setAvatar(data) easy
//base case (update daw)


//27/05/2025
//SIMPLE GOAL ❌ ==> base64 keeps error.. try blob
//base64 is the supported way to upload media type REACT NATIVE + supabase... keep base64 not blob

export function useAvatarUpload() {
    const [uploading, setUploading] = useState(false)
    // const { updatePictureProfileToFix } = useProfiles('accountrr'); //'account' try if bugged again

    async function uploadAvatar(userId: string, setAvatarUrl: (url: string) => void) {
        setUploading(true)

        if (!userId) {
            console.error('Missing user ID')
            Alert.alert('Error', 'User ID is required')
            setUploading(false)
            return
        }

        try {
            //permission
            const { status } = await ImagePicker.requestMediaLibraryPermissionsAsync()
            if (status !== 'granted') {
                Alert.alert('Permission required', 'Need access to media library')
                setUploading(false)
                return
            }






            const result = await ImagePicker.launchImageLibraryAsync({
                mediaTypes: ['images'],
                allowsEditing: true,
                aspect: [1, 1],
                quality: 0.75,
            })

            if (result.canceled) {
                setUploading(false)
                return
            }

            const asset = result.assets[0]
            const uri = asset.uri

            // Create file object for FormData
            const filename = uri.split('/').pop() || 'profile.jpg'
            const fileType = asset.type || 'image/jpeg'

            // Create a File-like object for React Native
            const file = {
                uri: uri,
                type: fileType,
                name: filename,
            } as any

            // Upload to ASP.NET backend instead of Supabase
            const response = await uploadImageProfile(file) // returns directory of uploaded image (from localstorage of my pc)

            if (response.data) {
                const { imageUrl, message } = response.data
                console.log(message)

                // Set the avatar URL (this will be the relative path from backend)
                const API_BASE_URL = process.env.EXPO_PUBLIC_API_BASE_URL
                const baseUrl = (API_BASE_URL ? API_BASE_URL.replace(/\/api$/, "") : "") || "http://localhost:5248"
                const fullImageUrl = `${baseUrl}${imageUrl}` // NOT THIS! but keep this note haha

                setAvatarUrl(imageUrl)
                // setAvatarUrl is from outside who used AvatarUpload helper.
                Alert.alert('Success', 'Avatar updated successfully')
            }
        } catch (error) {
            console.error('Avatar upload error:', error)
            Alert.alert('Error', 'Failed to upload avatar. Please try again.')
        } finally {
            setUploading(false)
        }
    }

    return { uploadAvatar, uploading }
}