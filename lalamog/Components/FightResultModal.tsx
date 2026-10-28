import React, { useState } from 'react';
import {
    View,
    Text,
    StyleSheet,
    TouchableOpacity,
    ScrollView,
    Alert,
    Modal,
    TextInput,
} from 'react-native';
import FontAwesome from '@expo/vector-icons/FontAwesome';
import { FightScheduleData, completeFightScheduleAPI, recordFightResultAPI } from '@/api/axiosInstance';

interface FightResultModalProps {
    visible: boolean;
    onClose: () => void;
    fight: FightScheduleData;
    currentUserId: string;
    matchUserId1: string; // User1Id from the Match
    matchUserId2: string; // User2Id from the Match
    onSuccess: () => void;
}

const FightResultModal: React.FC<FightResultModalProps> = ({
    visible,
    onClose,
    fight,
    currentUserId,
    matchUserId1,
    matchUserId2,
    onSuccess
}) => {
    const [step, setStep] = useState<'complete' | 'record'>(fight.status === 'confirmed' ? 'complete' : 'record');
    const [loading, setLoading] = useState(false);

    // Step 1: Complete fight
    const [completionNotes, setCompletionNotes] = useState('');

    // Step 2: Record result
    const [fightResult, setFightResult] = useState<'user1_win' | 'user2_win' | 'draw' | 'no_contest'>('user1_win');
    const [winMethod, setWinMethod] = useState('');
    const [fightDurationMinutes, setFightDurationMinutes] = useState('');
    const [resultNotes, setResultNotes] = useState('');
    const [user1Rating, setUser1Rating] = useState('5');
    const [user2Rating, setUser2Rating] = useState('5');
    const [user1Feedback, setUser1Feedback] = useState('');
    const [user2Feedback, setUser2Feedback] = useState('');

    const handleCompleteFight = async () => {
        try {
            setLoading(true);
            await completeFightScheduleAPI(fight.id, {
                completionNotes: completionNotes || undefined
            });
            
            Alert.alert('Success', 'Fight marked as completed. Now record the results.', [
                {
                    text: 'Continue',
                    onPress: () => setStep('record')
                }
            ]);
        } catch (error: any) {
            console.error('Error completing fight:', error);
            let errorMessage = 'Failed to complete fight';
            
            if (error.response?.data?.message) {
                errorMessage = error.response.data.message;
            }
            
            Alert.alert('Error', errorMessage);
        } finally {
            setLoading(false);
        }
    };

    const handleRecordResult = async () => {
        try {
            setLoading(true);

            // Determine winner ID based on fight result
            // The backend validates that winnerId matches User1Id or User2Id from the Match
            let winnerId: string | undefined;
            
            if (fightResult === 'user1_win') {
                winnerId = matchUserId1; // User1 from Match is the winner
            } else if (fightResult === 'user2_win') {
                winnerId = matchUserId2; // User2 from Match is the winner
            } else {
                // draw or no_contest - no winner
                winnerId = undefined;
            }

            const resultData = {
                fightScheduleId: fight.id,
                fightResult,
                winnerId,
                winMethod: winMethod || undefined,
                fightDurationMinutes: fightDurationMinutes ? parseInt(fightDurationMinutes) : undefined,
                resultNotes: resultNotes || undefined,
                user1Rating: user1Rating ? parseInt(user1Rating) : undefined,
                user2Rating: user2Rating ? parseInt(user2Rating) : undefined,
                user1Feedback: user1Feedback || undefined,
                user2Feedback: user2Feedback || undefined,
            };

            console.log('Recording result:', resultData);
            await recordFightResultAPI(resultData);
            
            Alert.alert('Success', 'Fight result recorded successfully!');
            onSuccess();
            handleClose();
        } catch (error: any) {
            console.error('Error recording result:', error);
            let errorMessage = 'Failed to record fight result';
            
            if (error.response?.data?.message) {
                errorMessage = error.response.data.message;
            }
            
            Alert.alert('Error', errorMessage);
        } finally {
            setLoading(false);
        }
    };

    const handleClose = () => {
        setStep('complete');
        setCompletionNotes('');
        setFightResult('user1_win');
        setWinMethod('');
        setFightDurationMinutes('');
        setResultNotes('');
        setUser1Rating('5');
        setUser2Rating('5');
        setUser1Feedback('');
        setUser2Feedback('');
        onClose();
    };

    const winMethods = ['KO', 'TKO', 'Decision', 'Submission', 'Technical Decision', 'Disqualification'];

    return (
        <Modal
            visible={visible}
            animationType="slide"
            transparent={true}
            onRequestClose={handleClose}
        >
            <View style={styles.modalOverlay}>
                <View style={styles.modalContent}>
                    <View style={styles.modalHeader}>
                        <Text style={styles.modalTitle}>
                            {step === 'complete' ? 'Complete Fight' : 'Record Fight Result'}
                        </Text>
                        <TouchableOpacity onPress={handleClose}>
                            <FontAwesome name="times" size={24} color="#333" />
                        </TouchableOpacity>
                    </View>

                    <ScrollView style={styles.modalBody} showsVerticalScrollIndicator={true}>
                        {/* Step indicator */}
                        <View style={styles.stepIndicator}>
                            <View style={[styles.step, step === 'complete' && styles.activeStep]}>
                                <Text style={[styles.stepText, step === 'complete' && styles.activeStepText]}>
                                    1. Complete
                                </Text>
                            </View>
                            <View style={styles.stepConnector} />
                            <View style={[styles.step, step === 'record' && styles.activeStep]}>
                                <Text style={[styles.stepText, step === 'record' && styles.activeStepText]}>
                                    2. Record Result
                                </Text>
                            </View>
                        </View>

                        {step === 'complete' ? (
                            // STEP 1: Complete Fight
                            <>
                                <Text style={styles.infoText}>
                                    Mark this fight as completed. After completion, you'll record the fight results.
                                </Text>

                                <Text style={styles.inputLabel}>Completion Notes (Optional)</Text>
                                <TextInput
                                    style={[styles.textInput, styles.textArea]}
                                    placeholder="Add any notes about how the fight went..."
                                    value={completionNotes}
                                    onChangeText={setCompletionNotes}
                                    multiline={true}
                                    numberOfLines={4}
                                />
                            </>
                        ) : (
                            // STEP 2: Record Result
                            <>
                                <Text style={styles.infoText}>
                                    Record the official fight result and provide feedback.
                                </Text>

                                <Text style={styles.inputLabel}>Fight Result *</Text>
                                <View style={styles.resultButtonsContainer}>
                                    <TouchableOpacity
                                        style={[styles.resultButton, fightResult === 'user1_win' && styles.resultButtonActive]}
                                        onPress={() => setFightResult('user1_win')}
                                    >
                                        <Text style={[styles.resultButtonText, fightResult === 'user1_win' && styles.resultButtonTextActive]}>
                                            User 1 Win
                                        </Text>
                                    </TouchableOpacity>
                                    <TouchableOpacity
                                        style={[styles.resultButton, fightResult === 'user2_win' && styles.resultButtonActive]}
                                        onPress={() => setFightResult('user2_win')}
                                    >
                                        <Text style={[styles.resultButtonText, fightResult === 'user2_win' && styles.resultButtonTextActive]}>
                                            User 2 Win
                                        </Text>
                                    </TouchableOpacity>
                                </View>
                                <View style={styles.resultButtonsContainer}>
                                    <TouchableOpacity
                                        style={[styles.resultButton, fightResult === 'draw' && styles.resultButtonActive]}
                                        onPress={() => setFightResult('draw')}
                                    >
                                        <Text style={[styles.resultButtonText, fightResult === 'draw' && styles.resultButtonTextActive]}>
                                            Draw
                                        </Text>
                                    </TouchableOpacity>
                                    <TouchableOpacity
                                        style={[styles.resultButton, fightResult === 'no_contest' && styles.resultButtonActive]}
                                        onPress={() => setFightResult('no_contest')}
                                    >
                                        <Text style={[styles.resultButtonText, fightResult === 'no_contest' && styles.resultButtonTextActive]}>
                                            No Contest
                                        </Text>
                                    </TouchableOpacity>
                                </View>

                                <Text style={styles.inputLabel}>Win Method</Text>
                                <View style={styles.methodButtonsContainer}>
                                    {winMethods.map((method) => (
                                        <TouchableOpacity
                                            key={method}
                                            style={[styles.methodButton, winMethod === method && styles.methodButtonActive]}
                                            onPress={() => setWinMethod(method)}
                                        >
                                            <Text style={[styles.methodButtonText, winMethod === method && styles.methodButtonTextActive]}>
                                                {method}
                                            </Text>
                                        </TouchableOpacity>
                                    ))}
                                </View>

                                <Text style={styles.inputLabel}>Fight Duration (Minutes)</Text>
                                <TextInput
                                    style={styles.textInput}
                                    placeholder="e.g., 15"
                                    value={fightDurationMinutes}
                                    onChangeText={setFightDurationMinutes}
                                    keyboardType="numeric"
                                />

                                <Text style={styles.inputLabel}>Result Notes</Text>
                                <TextInput
                                    style={[styles.textInput, styles.textArea]}
                                    placeholder="Describe what happened in the fight..."
                                    value={resultNotes}
                                    onChangeText={setResultNotes}
                                    multiline={true}
                                    numberOfLines={3}
                                />

                                <View style={styles.divider} />

                                <Text style={styles.sectionTitle}>Opponent Ratings & Feedback</Text>

                                <Text style={styles.inputLabel}>User 1 Rating (1-5)</Text>
                                <View style={styles.ratingContainer}>
                                    {[1, 2, 3, 4, 5].map((rating) => (
                                        <TouchableOpacity
                                            key={rating}
                                            style={styles.starButton}
                                            onPress={() => setUser1Rating(rating.toString())}
                                        >
                                            <FontAwesome
                                                name={parseInt(user1Rating) >= rating ? 'star' : 'star-o'}
                                                size={32}
                                                color="#FFD700"
                                            />
                                        </TouchableOpacity>
                                    ))}
                                </View>

                                <Text style={styles.inputLabel}>User 1 Feedback</Text>
                                <TextInput
                                    style={[styles.textInput, styles.textArea]}
                                    placeholder="Your feedback for User 1..."
                                    value={user1Feedback}
                                    onChangeText={setUser1Feedback}
                                    multiline={true}
                                    numberOfLines={3}
                                />

                                <Text style={styles.inputLabel}>User 2 Rating (1-5)</Text>
                                <View style={styles.ratingContainer}>
                                    {[1, 2, 3, 4, 5].map((rating) => (
                                        <TouchableOpacity
                                            key={rating}
                                            style={styles.starButton}
                                            onPress={() => setUser2Rating(rating.toString())}
                                        >
                                            <FontAwesome
                                                name={parseInt(user2Rating) >= rating ? 'star' : 'star-o'}
                                                size={32}
                                                color="#FFD700"
                                            />
                                        </TouchableOpacity>
                                    ))}
                                </View>

                                <Text style={styles.inputLabel}>User 2 Feedback</Text>
                                <TextInput
                                    style={[styles.textInput, styles.textArea]}
                                    placeholder="Your feedback for User 2..."
                                    value={user2Feedback}
                                    onChangeText={setUser2Feedback}
                                    multiline={true}
                                    numberOfLines={3}
                                />
                            </>
                        )}
                    </ScrollView>

                    <View style={styles.modalButtons}>
                        <TouchableOpacity
                            style={[styles.modalButton, styles.cancelButton]}
                            onPress={handleClose}
                        >
                            <Text style={styles.cancelButtonText}>Cancel</Text>
                        </TouchableOpacity>
                        <TouchableOpacity
                            style={[styles.modalButton, styles.submitButton, loading && styles.disabledButton]}
                            onPress={step === 'complete' ? handleCompleteFight : handleRecordResult}
                            disabled={loading}
                        >
                            <Text style={styles.submitButtonText}>
                                {loading ? 'Processing...' : step === 'complete' ? 'Complete Fight' : 'Record Result'}
                            </Text>
                        </TouchableOpacity>
                    </View>
                </View>
            </View>
        </Modal>
    );
};

const styles = StyleSheet.create({
    modalOverlay: {
        flex: 1,
        backgroundColor: 'rgba(0,0,0,0.5)',
        justifyContent: 'center',
        alignItems: 'center',
    },
    modalContent: {
        backgroundColor: '#fff',
        margin: 20,
        borderRadius: 12,
        width: '90%',
        maxHeight: '90%',
    },
    modalHeader: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
        padding: 20,
        borderBottomWidth: 1,
        borderBottomColor: '#eee',
    },
    modalTitle: {
        fontSize: 18,
        fontWeight: 'bold',
        color: '#333',
    },
    modalBody: {
        padding: 20,
    },
    stepIndicator: {
        flexDirection: 'row',
        alignItems: 'center',
        marginBottom: 20,
    },
    step: {
        flex: 1,
        padding: 10,
        backgroundColor: '#f0f0f0',
        borderRadius: 8,
        alignItems: 'center',
    },
    activeStep: {
        backgroundColor: '#007AFF',
    },
    stepText: {
        fontSize: 14,
        color: '#666',
        fontWeight: '600',
    },
    activeStepText: {
        color: '#fff',
    },
    stepConnector: {
        width: 20,
        height: 2,
        backgroundColor: '#ddd',
    },
    infoText: {
        fontSize: 14,
        color: '#666',
        marginBottom: 20,
        lineHeight: 20,
    },
    inputLabel: {
        fontSize: 16,
        fontWeight: '600',
        color: '#333',
        marginBottom: 8,
        marginTop: 15,
    },
    textInput: {
        borderWidth: 1,
        borderColor: '#ddd',
        borderRadius: 8,
        padding: 12,
        fontSize: 16,
        backgroundColor: '#fff',
    },
    textArea: {
        minHeight: 80,
        textAlignVertical: 'top',
    },
    resultButtonsContainer: {
        flexDirection: 'row',
        gap: 10,
        marginBottom: 10,
    },
    resultButton: {
        flex: 1,
        padding: 12,
        backgroundColor: '#f0f0f0',
        borderRadius: 8,
        alignItems: 'center',
        borderWidth: 2,
        borderColor: 'transparent',
    },
    resultButtonActive: {
        backgroundColor: '#E3F2FD',
        borderColor: '#007AFF',
    },
    resultButtonText: {
        fontSize: 14,
        color: '#666',
        fontWeight: '600',
    },
    resultButtonTextActive: {
        color: '#007AFF',
    },
    methodButtonsContainer: {
        flexDirection: 'row',
        flexWrap: 'wrap',
        gap: 8,
    },
    methodButton: {
        padding: 8,
        paddingHorizontal: 12,
        backgroundColor: '#f0f0f0',
        borderRadius: 6,
        borderWidth: 1,
        borderColor: 'transparent',
    },
    methodButtonActive: {
        backgroundColor: '#E3F2FD',
        borderColor: '#007AFF',
    },
    methodButtonText: {
        fontSize: 13,
        color: '#666',
    },
    methodButtonTextActive: {
        color: '#007AFF',
        fontWeight: '600',
    },
    ratingContainer: {
        flexDirection: 'row',
        gap: 8,
        marginVertical: 8,
    },
    starButton: {
        padding: 4,
    },
    divider: {
        height: 1,
        backgroundColor: '#eee',
        marginVertical: 20,
    },
    sectionTitle: {
        fontSize: 16,
        fontWeight: 'bold',
        color: '#333',
        marginBottom: 10,
    },
    modalButtons: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        padding: 20,
        borderTopWidth: 1,
        borderTopColor: '#eee',
    },
    modalButton: {
        flex: 1,
        paddingVertical: 12,
        borderRadius: 8,
        alignItems: 'center',
    },
    cancelButton: {
        backgroundColor: '#f0f0f0',
        marginRight: 10,
    },
    cancelButtonText: {
        color: '#333',
        fontSize: 16,
        fontWeight: '600',
    },
    submitButton: {
        backgroundColor: '#4CAF50',
        marginLeft: 10,
    },
    submitButtonText: {
        color: '#fff',
        fontSize: 16,
        fontWeight: '600',
    },
    disabledButton: {
        opacity: 0.5,
    },
});

export default FightResultModal;
