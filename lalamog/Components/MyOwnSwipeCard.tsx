//TRAIN TestSwipeCardHypothess
//❓❓❓https://reactnative.dev/docs/panresponder ====> MAIN IDEA





//🎴NOTE FOR MYSELF: this is for learning not completion! LEARN REUSABLE COMPONENT (system design)

//🎯GOAL 1: instead of using library, lets create my own reusable component to learn how to create reusable component. 
//🎯GOAL 2: i'm not here to master animation, rather logic in react native // react component ONLY
//GOAL 2.1 : Generating boilerplate code for animatio thru AI and youtube idea is OK. Im not mastering frontend, but full stack path


//🎯GOAL 3: Do this by my own
//GOAL 3.1: custom flexible
//onSwipeLeft || right
//renderNoMoreCards ==> returns REACT NODE ELEMENETS
//custom "render" function ==> returns REACT NODE ELEMENETS
//fix my own issue what will happen if data is all used? ''add more in ANIMATION var holder'' a guess answer

//https://www.youtube.com/watch?v=cWdGmUFXua8
// AND OF COURSE chat-gpt === LOOK AT DATE 18/05/2025
//https://www.youtube.com/watch?v=cWdGmUFXua8
// AND OF COURSE chat-gpt === LOOK AT DATE 18/05/2025
//https://www.youtube.com/watch?v=cWdGmUFXua8
// AND OF COURSE chat-gpt === LOOK AT DATE 18/05/2025
//https://www.youtube.com/watch?v=cWdGmUFXua8
// AND OF COURSE chat-gpt === LOOK AT DATE 18/05/2025

















import React, { forwardRef, useEffect, useImperativeHandle, useRef, useState } from 'react';
import {
    Animated,
    Dimensions,
    ImageSourcePropType,
    PanResponder,
    StyleProp,
    StyleSheet,
    Text,
    View,
    ViewStyle
} from 'react-native';










//📌INFINITE SCROLL WITH MOIVATION API ABOUT FIGHTING:
//1
//since our position[currentIndex] and currentIndex is the basis of whole animation by AI
//in setPositions(data.map(() => new Animated.ValueXY({ x: 0, y: 0 }))) let's add
//dummy data at the end of the list

//2
//  in handle swipe, stay at dummy index
// if (currentIndex >= data.length) {
//   setCurrentIndex(data.length);
// } else if (currentIndex < data.length - 1) {
//   setCurrentIndex(currentIndex + 1);
// } else {
//   setCurrentIndex(data.length);
// }

//3
//conditional if currentIndex => data.length
//copy <Animated.View {...panResponder.panHandlers}







//OVERALL CONCEPT HOW THIS MOVE
//I MEAN JUST READ THIS
//https://reactnative.dev/docs/panresponder

//Initialize State  
//const [currentIndex, setCurrentIndex] = useState(0);
//const [positions, setPositions] = useState<Animated.ValueXY[]>([]);

//const panResponder = PanResponder.create({ handles movement 
//https://reactnative.dev/docs/panresponder
//onPanResponderMove and onPanResponderRelease

//<Animated.View> ELEMENT INSIDE </Animated.View> is the one the cards


//off-screen movement ---> Animated.timing() using direction 'left'?'right'
//here we do what happened when onSwipe

//add rotation base on X Movement === animatedValue.setValue(gesture.dx); // Inside PanResponder
//apply in transform below the return jsx/

//I MEAN JUST READ THIS
//https://reactnative.dev/docs/panresponder











const SCREEN_WIDTH = Dimensions.get('window').width;
const DEFAULT_CARD_SIZE = { width: SCREEN_WIDTH - 40, height: 500 };
const DEFAULT_SWIPE_THRESHOLD = SCREEN_WIDTH * 0.25;

//DEPRECATED nvm... this is what we use if we dont apply <T> genericsss
type Card<T> = {
    id: string;
    imageUrl: ImageSourcePropType;
    name: string;
    age: number;
};


//allows users to pass any data structure as long as it has an id 
//we can do this too like in java type SwipeCardProps<T> =  BUT for safety and for me, lets RESTRICT

type SwipeCardProps<T extends { id: string }> = {
    data: T[];
    //data: { id: string }[]; without <T></T>
    cardSize?: { width: number; height: number };
    swipeThreshold?: number;
    cardStyle?: StyleProp<ViewStyle>;
    renderNoMoreCards?: () => React.ReactNode;
    customRender?: (item: T) => React.ReactNode;
    onSwipeLeft?: (cardIndex: number) => void;
    onSwipeRight?: (cardIndex: number) => void;
    customPreviewRender?: (item: T) => React.ReactNode;
}

export type SwipeCardHandle = {
    swipeLeft: () => void;
    swipeRight: () => void;
};



export default forwardRef<SwipeCardHandle, SwipeCardProps<any>>(function MyOwnSwipeCard(
    {
        data,
        cardSize = DEFAULT_CARD_SIZE,
        swipeThreshold = DEFAULT_SWIPE_THRESHOLD,
        cardStyle,
        renderNoMoreCards,
        customRender,
        onSwipeLeft,
        onSwipeRight,
        customPreviewRender,
    },
    ref
) {

    useImperativeHandle(ref, () => ({
        swipeLeft: () => handleSwipe('left'),
        swipeRight: () => handleSwipe('right'),
    }));


    //allows users to pass any data structure as long as it has an id 
    //we can do this too like in java type SwipeCardProps<T> =  BUT for safety and for me, lets RESTRICT



    const [currentIndex, setCurrentIndex] = useState(0);
    const [positions, setPositions] = useState<Animated.ValueXY[]>([]);
    const animatedValue = new Animated.Value(0);

    //handle swipe
    const mountedRef = useRef(true);

    useEffect(() => {
        return () => {
            mountedRef.current = false;
        };
    }, []);



    const styles = StyleSheet.create({
        card: {
            position: 'absolute',
            backgroundColor: 'white',
            borderRadius: 10,
            shadowColor: '#000',
            shadowOffset: { width: 0, height: 2 },
            shadowOpacity: 0.25,
            shadowRadius: 3.84,
            elevation: 5,
            overflow: 'hidden',
        },
        previewCard: {
            position: 'absolute',
            backgroundColor: 'white',
            borderRadius: 10,
            left: 20,
            top: 0,
        },
        previewImage: {
            width: '100%',
            height: '100%',
            resizeMode: 'cover',
        },
        image: {
            width: '100%',
            height: '100%',
            resizeMode: 'cover',
        },
        infoContainer: {
            position: 'absolute',
            bottom: 0,
            left: 0,
            right: 0,
            padding: 16,
            backgroundColor: 'rgba(0,0,0,0.5)',
        },
        name: {
            color: 'white',
            fontSize: 24,
            fontWeight: 'bold',
        },
        emptyContainer: {
            flex: 1,
            justifyContent: 'center',
            alignItems: 'center',
            padding: 20,
        },








        dummyContainer: {
            flex: 1,
            justifyContent: 'center',
            alignItems: 'center',
            padding: 20,
        },

        dummyEmoji: {
            fontSize: 48,
            marginBottom: 10,
        },

        dummyText: {
            fontSize: 24,
            fontWeight: 'bold',
            color: '#333',
            textAlign: 'center',
        },

        dummySubtext: {
            fontSize: 16,
            color: '#888',
            marginTop: 8,
        },
    });





    //count of POSITIONS is based on the submitted DATA (profiles example)
    //currentIndex is just counter to POSITIONS,,, and not the actual id,, set ID on index.tsx
    useEffect(() => {
        if (data.length === 0) return;

        const newPositions = data.map(() => new Animated.ValueXY({ x: 0, y: 0 }));
        //for infinite swipe always add one in last length
        newPositions.push(new Animated.ValueXY({ x: 0, y: 0 }));
        setPositions(newPositions);
        setCurrentIndex(0);
    }, [data]);

    if (positions.length === 0) {
        return null;
    }


    //personal note:
    //currentIndex -- ofc current index of the card
    //positions -- array of Animated.ValueXY this objects track the current translation (x and y offsets)
    //so positions[currentIndex] is the currentCard.

    //how does it move?
    //positions[currentIndex].setValue({ x: gesture.dx, y: gesture.dy }); updates position of card

    //when swipe is detected (left or right) => we can remove the current index and reset animation
    //by currentCard.setValue({ x: 0, y: 0 }); and updated currentIndex



    const handleSwipe = (direction: 'left' | 'right') => {
        const isValidIndex = mountedRef.current && currentIndex < data.length;



        const currentCard = positions[currentIndex]; //to pan or drag a current-card, here is the logic
        if (isValidIndex) {
            //customizable fallback logic custom mine code
            if (direction === 'left' && onSwipeLeft) {
                onSwipeLeft(currentIndex);
            } else if (direction === 'right' && onSwipeRight) {
                onSwipeRight(currentIndex);
            }
        }


        // Animate off screen
        //again, currentCard === array of Animated.ValueXY({ x: 0, y: 0 }) [based on currentIndex]
        Animated.timing(currentCard, {
            toValue: {
                x: direction === 'left' ? -SCREEN_WIDTH : SCREEN_WIDTH,
                y: 0,
            },
            duration: 200,
            useNativeDriver: true,
        }).start(() => {
            if (!mountedRef.current) return;
            // Reset value
            currentCard.setValue({ x: 0, y: 0 });

            // // Move to next card
            // if (currentIndex < data.length - 1) {
            //     setCurrentIndex(currentIndex + 1);
            // }


            //update about INFINITE SCROLL
            if (currentIndex < data.length) {
                if (isValidIndex && currentIndex < data.length - 1) {
                    setCurrentIndex(currentIndex + 1);
                } else {
                    //DUMMY CARD
                    setCurrentIndex(data.length);
                }
            }

        });
    };









    // Create pan responder
    //https://reactnative.dev/docs/panresponder     
    const panResponder = PanResponder.create({
        onStartShouldSetPanResponder: () => true,
        onPanResponderMove: (_, gesture) => {
            positions[currentIndex].setValue({ x: gesture.dx, y: gesture.dy });

            // Rotate based on X movement
            animatedValue.setValue(gesture.dx);
        },
        onPanResponderRelease: (_, gesture) => {
            if (gesture.dx > swipeThreshold) {
                handleSwipe('right');
            } else if (gesture.dx < -swipeThreshold) {
                handleSwipe('left');
            } else {
                // Reset
                Animated.timing(positions[currentIndex], {
                    toValue: { x: 0, y: 0 },
                    duration: 150,
                    useNativeDriver: true,
                }).start();;
                animatedValue.setValue(0);
            }
        },
    });

    // Rotation interpolation
    const rotate = animatedValue.interpolate({
        inputRange: [-swipeThreshold, 0, swipeThreshold],
        outputRange: ['-30deg', '0deg', '30deg'],
    });











    //render cards unli swipe if last CARD
    if (currentIndex >= data.length) {
        const isDummy = currentIndex === data.length;

        return (
            <Animated.View
                {...panResponder.panHandlers}
                style={[
                    styles.card,
                    {
                        width: cardSize.width,
                        height: cardSize.height,
                        backgroundColor: '#f5f5f5',
                        transform: [
                            { translateX: positions[data.length].x },
                            { translateY: positions[data.length].y },
                            { rotate },
                        ],
                    },
                    cardStyle,
                ]}
            >


                {/* MUST ME FLEXIBLE UPDATE SOON */}
                {renderNoMoreCards ?
                    (<>
                        {renderNoMoreCards()}
                    </>)
                    :
                    (<View style={styles.dummyContainer}>
                        <Text style={styles.dummyEmoji}>😄</Text>
                        <Text style={styles.dummyText}>Keep Swiping!</Text>
                        <Text style={styles.dummySubtext}>Template Card</Text>
                    </View>)
                }

            </Animated.View>
        );
    }




















    return (
        <>
            {/* Next card preview */}
            {currentIndex + 1 < data.length && (
                <View style={[
                    styles.previewCard,
                    {
                        width: cardSize.width,
                        height: cardSize.height,
                        opacity: 0.7,
                        transform: [{ scale: 0.95 }],
                    },
                    cardStyle,
                ]}>

                    {currentIndex + 1 < data.length && (
                        <View style={[styles.previewCard, { width: cardSize.width, height: cardSize.height }]}>
                            {customPreviewRender ? customPreviewRender(data[currentIndex + 1]) : <Text >
                                Please provide  customPreviewRender ("use the returnd PROFILE (not index).. but whole profile")
                            </Text>}
                        </View>
                    )}


                </View>
            )}

            {/* Current card */}
            <Animated.View
                {...panResponder.panHandlers}
                style={[
                    styles.card,
                    {
                        width: cardSize.width,
                        height: cardSize.height,
                        transform: [
                            { translateX: positions[currentIndex].x },
                            { translateY: positions[currentIndex].y },
                            { rotate },
                        ],
                    },
                    cardStyle,
                ]}
            >
                {/* should be flexible... update soon */}
                {customRender ?
                    (<>
                        {/* if custom logic. JUST TRIGGER the logic here and DEFINE logic in APPS */}
                        {customRender(data[currentIndex])}
                    </>)
                    :
                    (<>
                        {/* <Image
                            source={data[currentIndex].imageUrl}
                            style={styles.image}
                        /> */}
                        <View style={styles.infoContainer}>
                            <Text style={styles.name}>
                                Please provide customRender()
                            </Text>
                        </View>
                    </>)

                }



            </Animated.View>
        </>
    );
}
















)
