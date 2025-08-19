//https://reactnative.dev/docs/panresponder

//swipe card tester






import React, { useRef } from 'react';
import { Animated, PanResponder, StyleSheet, Text, View } from 'react-native';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';

const TestCard = () => {
  const pan = useRef(new Animated.ValueXY()).current;
  //in our code, suppose this is an array of Animated.ValueXY() MAPPED by the length of given data

  const panResponder = useRef(
    PanResponder.create({
      onMoveShouldSetPanResponder: () => true,
      onPanResponderMove: Animated.event([null, {dx: pan.x, dy: pan.y}]),
      onPanResponderRelease: () => {
        //if swiped right / left (THRESHOLD LENGTH ASK AI IDK HOW TO DO THAT NO TIME){
          // call handleSwipeLeft || onSwipeRight
          //
        //}
        Animated.timing(pan, {
                    toValue: { x: 0, y: 0 },
                    duration: 150,
                    useNativeDriver: true,
                }).start();;
                      },
    }),
  ).current;


  return (
    <SafeAreaProvider>
      <SafeAreaView style={styles.container}>
        <Text style={styles.titleText}>Drag this box!</Text>
        <Animated.View
          style={{
            transform: [{translateX: pan.x}, {translateY: pan.y}],
          }}
          {...panResponder.panHandlers}>
          <View style={styles.box} />
        </Animated.View>
      </SafeAreaView>
    </SafeAreaProvider>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  titleText: {
    fontSize: 14,
    lineHeight: 24,
    fontWeight: 'bold',
  },
  box: {
    height: 150,
    width: 150,
    backgroundColor: 'blue',
    borderRadius: 5,
  },
});

export default TestCard;