// Custom hooks usually return state variables and functions that you want to use in your components.
// For example, your useProfiles hook returns: tignan mo sa baba

import { useEffect, useRef, useState } from 'react';
import { Alert } from 'react-native';
import { useAuth } from './AuthProvider';
import { supabase } from './supabase';
import { getAvailableProfilesAPI, ProfileData, AllProfileData, getMatchesAPI } from '@/api/axiosInstance';


//fetch profiles of other users. NOT MY PROFILE (para di ako malito)
// export type Profile = {
//   id: string;
//   username: string;
//   full_name: string;
//   avatar_url: string | null;
//   bio: string | null;
//   fighting_style: string | null;
//   experience_level: number | null;
//   availability: { days: string[]; time: string } | null;
//   latitude: string;
//   longitude: string;
//   address?: string;

// };




//helper function for getting actual address rather than numerics FReE?? idk try

async function reverseGeocode(lat: number, lng: number) {
  const url = `https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&zoom=18&addressdetails=1`;
  const response = await fetch(url, {
    headers: { 'User-Agent': 'MyApp/1.0' } // Required by OSM
  });
  const data = await response.json();
  
//    Latitude corresponds to the Y coordinate.

//    Longitude corresponds to the X coordinate.



  const address = data.address;

  const parts = [
    address.road,
    address.suburb,
    address.city || address.town || address.village,
    address.state,
    address.country,
  ].filter(Boolean);

  return parts.join(', ');
}



//HOW TO USE? sample
//const { updatePictureProfileToFix } = useProfiles('accountrr');

export function useProfiles(context: string) {
  const { session, isLoading } = useAuth()

  const hasSubscribed = useRef(false);
  const [profiles, setProfiles] = useState<ProfileData[]>([]);


  //already matched, can be PASSED for message views and to filter out the NOTmathcedProfiles
  const [matchedProfiles, setMatchedProfiles] = useState<ProfileData[]>([]);



  const [fetching, setFetching] = useState(false);




  // update getMatchesAPI
  const fetchMatchedProfiles = async () => {
    try {
      const response = await getMatchesAPI();
      const matchedProfilesData: ProfileData[] = response.data || [];
      setMatchedProfiles(matchedProfilesData);
    } catch (error) {
      console.error("Error fetching matched profiles:", error);
    }
  };

  const updateProfileToFix = async (updates: Partial<ProfileData>) => {
    try {
      const { error } = await supabase
        .from('profiles')
        .upsert(updates);

      return error;

    } catch (error) {
      console.log(error)
    }


  }

  const updatePictureProfileToFix = async (publicUrl: string, userId: string) => {
    try {
      const { error: updateError } = await supabase
        .from('profiles')
        .update({ avatar_url: publicUrl })
        .eq('id', userId)

      return updateError;

    } catch (error) {
      console.log(error)
    }


  }





  // runs of UseEffect on session change
  // FETCHES ALL AVAILABLE PROFILES
  const load = async () => {
    try {
      if (!session) {
        setProfiles([]);
        return;
      }


      //code below are for asp net: PSEUDOCODE
      // 1: fetch all profiles except my own
      // asp net backend has already that it returns profilesDto. 
      //    Request.get(available-profiles)

      try {
        const response = await getAvailableProfilesAPI(); //returns AllProfileData
        const allProfilesData: AllProfileData = response.data; //assuming data is AllProfileData object

        console.log("Fetched all profiles data:", allProfilesData);

        // Extract available and matched profiles from the response
        const availableProfiles = allProfilesData.availableProfiles || [];
        const matchedProfilesData = allProfilesData.matchedProfiles || [];

        
        console.log(`Available profiles count: ${availableProfiles.length}`);
        console.log(`Matched profiles count: ${matchedProfilesData.length}`);

        if (availableProfiles.length === 0 && matchedProfilesData.length === 0) {
          setProfiles([]);
          setMatchedProfiles([]);
          return;
        }





        // helper function to add address to profiles
        const addAddress = async (profiles: ProfileData[]) => {

          //personal note: WHY USE Promise.all?
          //    const result = profiles.map(async (profile) => {
          //    returns a Promise<Profile> only not actual result
          //
          //    console.log(result); // [Promise, Promise, ...]

          // so always use promise.all() when using for loop or .map function that alwyas await async

          return Promise.all(
            profiles.map(async (profile) => {
              const lat = profile.location?.y;
              const lng = profile.location?.x;
              if (typeof lat === "number" && typeof lng === "number") {

                try {
                  const address = await reverseGeocode(lat, lng);
                  return { ...profile, address };
                } catch (err) {
                  console.warn(`Failed to geocode profile ${profile.id}`, err);
                  return profile;
                }
              } else {
                // location may be missing or invalid -> skip geocoding
                return profile;
              }


            })
          );
        };

        // //sv🎯tep 6: Update state
                              //❌geocode only not mached.. note for me WHY? Faster initial load:
        const profilesWithAddresses = await addAddress(availableProfiles);   //await addAddress(matched) --> matched profiles are basically for chat component;

        console.log("Fetched profiles with addresses:", profilesWithAddresses);
        setProfiles(profilesWithAddresses);
        setMatchedProfiles(matchedProfilesData);







        // // Set all fetched profiles
        // setProfiles(profilesData);
      } catch (error) {
        console.error("Error fetching profiles:", error);
      }

      // (DONE IN BACKEND) --skip to step 3
      // 2: from tables of Matches fetch all that match with user1 or user2 (lexicographically)
      // then filter out the matched and non matched profiles from the fetched profiles (OPTIONAL)



      // 3: geocode the non-matched profiles to get their addresses (mas mabilis, then IT limits the request load to geoJson.)

      // const profilesWithGeoAddress = await addAddress(notMatchedFromApiFetchResults);

      // 4: set result: setProfiles() && setMatchedProfiles() with the results








      //below code is for supabase



      // //🎯vstep 1: FETCH ALL profiles
      // setFetching(true);
      // const { data: profilesData, error: profilesError } = await supabase
      //   .from('profiles')
      //   .select(`
      //     id,
      //     username,
      //     full_name,
      //     avatar_url,
      //     latitude,
      //     longitude,
      //     bio,
      //     fighting_style,
      //     experience_level,
      //     availability
      //   `)
      //   .neq('id', session.user.id);


      // if (profilesError) throw profilesError;



      // //🎯STEP 2: fetch from matchedTable if swiper/swipee is matching my id
      // const { data: matchesData, error: matchesError } = await supabase
      //   .from('matches')
      //   .select('user1_id, user2_id')
      //   //.eq('status', 'active') ===> soon maybe we need 
      //   .or(`user1_id.eq.${session.user.id},user2_id.eq.${session.user.id}`);

      // if (matchesError) throw matchesError;


      // //🎯vSTEP 3: get the ids of matched users (to filter out in next steps) of course exept my id
      // const matchedUserIds = matchesData
      //   .map(match => {
      //     if (match.user1_id === session.user.id) return match.user2_id;
      //     return match.user1_id;
      //   })
      //   .filter(Boolean);



      // //🎯step 4 :plit profiles into matched and not-matched

      // //Filter out matched profiles from ALL PROFILES
      // const matched = profilesData.filter(profile =>
      //   matchedUserIds.includes(profile.id)
      // );

      // const notMatched = profilesData.filter(profile =>
      //   !matchedUserIds.includes(profile.id)
      // );






      //deprecated code: HIDE || still save for refernce
      //geocode only not mached.. note for me WHY? Faster initial load: 
      //nonsense naman i geo load pa natin matched i guess?? mabagal kasi.

      //DEPRECATED CODE still savev

      //   const profilesWithAddresses = await Promise.all(
      //   notMatched.map(async (profile: any) => {
      //     const lat = parseFloat(profile.latitude);
      //     const lng = parseFloat(profile.longitude);
      //     const address = await reverseGeocode(lat, lng);
      //     return { ...profile, address };
      //   })
      // );

      // //sv🎯tep 6: Update state
      // setProfiles(profilesWithAddresses);
      // setMatchedProfiles(matched);


      //  const addAddress = async (profiles: Profile[]) => {

      //   //personal note: WHY USE Promise.all?
      //   //    const result = profiles.map(async (profile) => {
      //   //    returns a Promise<Profile> only not actual result
      //   //
      //   //    console.log(result); // [Promise, Promise, ...]

      //   // so always use promise.all() when using for loop or .map function that alwyas await async

      //   return Promise.all(
      //     profiles.map(async (profile) => {
      //       const lat = parseFloat(profile.latitude);
      //       const lng = parseFloat(profile.longitude);
      //       try {
      //         const address = await reverseGeocode(lat, lng);
      //         return { ...profile, address };
      //       } catch (err) {
      //         console.warn(`Failed to geocode profile ${profile.id}`, err);
      //         return profile; 
      //       }
      //     })
      //   );
      // };

      // //sv🎯tep 6: Update state
      //                       //❌geocode only not mached.. note for me WHY? Faster initial load:
      // const profilesWithAddresses = await addAddress(notMatched);   //await addAddress(matched) --> matched profiles are basically for chat component;


      // setProfiles(profilesWithAddresses);
      // setMatchedProfiles(matched);


    } catch (err: any) {
      Alert.alert('Error loading profiles', err.message);
    } finally {
      setFetching(false);
    }
  };













  //two ways to trigger load
  //on session change
  //on websocket






  //on session change
  useEffect(() => {
    load();
  }, [session]);





  //on websocket (SUPABASE)

  // useEffect(() => {
  //   if (!session || hasSubscribed.current) return;

  //   const channel = supabase
  //     .ch('matches-changes')
  //     .on(
  //       'postgres_changes',
  //       {
  //         event: 'INSERT',
  //         schema: 'public',
  //         table: 'matches',
  //       },
  //       (payload) => {
  //         const match = payload.new;
  //         if (
  //           match.user1_id === session.user.id ||
  //           match.user2_id === session.user.id
  //         ) {
  //           load();//refetchd
  //         }
  //       }
  //     )
  //     .on(
  //     'postgres_changes',
  //     {
  //       event: 'DELETE',
  //       schema: 'public',
  //       table: 'matches',
  //     },(payload) => {
  //       const deletedMatch = payload.old;
  //         if (
  //         deletedMatch?.user1_id === session.user.id ||
  //         deletedMatch?.user2_id === session.user.id
  //       ) {
  //           load();//refetchd
  //         }
  //       }
  //     )
  //     .subscribe();

  //   hasSubscribed.current = true;

  //   return () => {
  //     supabase.removeChannel(channel);
  //   };
  // }, [session]);





  //   // trigger listener (SUPABASE)
  // useEffect(() => {
  //   if (!session || hasSubscribed.current) return;

  //   // const channelName = `matches-changes-${session.user.id}`;
  //   const channel = supabase
  //     .channel(`matches-changes-${context}-${session.user.id}`) 
  //     .on(
  //       'postgres_changes',
  //       {
  //         event: '*',
  //         schema: 'public',
  //         table: 'matches',
  //         filter: `or=(user1_id.eq.${session.user.id},user2_id.eq.${session.user.id})`
  //       },
  //       (payload) => {
  //         if (payload.eventType === 'INSERT') {
  //           Alert.alert('New match!', 'You matched with someone!', [{ text: 'OK' }]);
  //         } else if (payload.eventType === 'DELETE') {
  //           Alert.alert('Match removed', 'Someone unmatched with you', [{ text: 'OK' }]);
  //         }
  //         load();
  //       }
  //     )
  //     .subscribe();

  //   hasSubscribed.current = true;

  //   return () => {
  //     supabase.removeChannel(channel);
  //     hasSubscribed.current = false;
  //   };
  // }, [session, context]);








  // useEffect(() => {
  //   if (!session || hasSubscribed.current) return;

  //   const channel = supabase
  //     .ch('matches-changes')
  //     .on(
  //     'postgres_changes',
  //     {
  //       event: 'DELETE',
  //       schema: 'public',
  //       table: 'matches',
  //     },(payload) => {
  //       const deletedMatch = payload.old;
  //         if (
  //         deletedMatch?.user1_id === session.user.id ||
  //         deletedMatch?.user2_id === session.user.id
  //       ) {
  //           load();//refetchd
  //         }
  //       }
  //     )
  //     .subscribe();

  //   hasSubscribed.current = true;

  //   return () => {
  //     supabase.removeChannel(channel);
  //   };
  // }, [session]);

  return { profiles, matchedProfiles, fetching, updateProfileToFix, updatePictureProfileToFix, fetchMatchedProfiles };
}
