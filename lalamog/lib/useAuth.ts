
// import { Session } from '@supabase/supabase-js';
// import { useEffect, useRef, useState } from 'react';
// import { supabase } from './supabase';




// //NOTE: this is using Websocket for realtime update if I AM AUTHENTICATED or NOT
// //useEffect here has no dependencies
// export function useAuth() {
//   const [session, setSession] = useState<Session | null>(null);
//   const [isLoading, setIsLoading] = useState(true);
//   const mounted = useRef(false);

//   useEffect(() => {
//     const getSession = async () => {
//       const { data: { session } } = await supabase.auth.getSession();
//       console.log('Initial session:', session?.user?.email);

//       if (mounted.current) {
//         setSession(session);
//         setIsLoading(false);
//       }
//     };



//     // const { data: listener } = supabase.auth.onAuthStateChange((event, session) => {

//     //   console.log('🍏🍏Supabase WS Auth Event:', event, session);

//     //   setSession(session);
//     //   setIsLoading(false);
//     // });

//     const { data: listener } = supabase.auth.onAuthStateChange(
//       (_, session) => {
//         if (mounted.current) {
//           setSession(session);
//           setIsLoading(false);
//         }
//       }
//     );

//     getSession();


//     //     If not unsubscribed, this listener continues running even after the component unmounts, potentially causing:
//     // Memory leaks (retaining unused components in memory).
//     // Race conditions (updating state in an unmounted component).
//     return () => {
//       mounted.current = false;
//       listener?.subscription.unsubscribe();
//     };
//   }, []);

//   return { session, isLoading };
// }