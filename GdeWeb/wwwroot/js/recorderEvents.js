// Hangfelvétel kezelő modul
let mediaRecorder = null;
let audioChunks = [];
let dotNetHelper = null;
let recordingInterval = null;

export function startRecording(dotNetReference) {
    dotNetHelper = dotNetReference;
    audioChunks = [];

    navigator.mediaDevices.getUserMedia({
        audio: {
            channelCount: 1,
            sampleRate: 16000,
            echoCancellation: true,
            noiseSuppression: true
        }
    })
        .then(stream => {
            try {
                mediaRecorder = new MediaRecorder(stream, {
                    mimeType: 'audio/webm;codecs=opus'
                });
            } catch (e) {
                // Fallback ha a webm nem támogatott
                mediaRecorder = new MediaRecorder(stream);
            }

            mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    audioChunks.push(event.data);
                }
            };

            mediaRecorder.onstop = () => {
                // Hangfelvétel leállt
                stream.getTracks().forEach(track => track.stop());
            };

            mediaRecorder.onerror = (event) => {
                console.error('MediaRecorder error:', event.error);
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnRecordingError',
                        'Hiba a felvétel során: ' + event.error.name);
                }
            };

            // 3 másodperces chunk-ok rögzítése
            mediaRecorder.start(3000);

            console.log('🎤 Recording started');

            // Auto-stop timeout (max 5 perc)
            recordingInterval = setTimeout(() => {
                if (mediaRecorder && mediaRecorder.state === 'recording') {
                    stopRecording(null, null);
                }
            }, 300000); // 5 perc

        })
        .catch(error => {
            console.error('Microphone access error:', error);
            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('OnRecordingError',
                    'Mikrofon hozzáférés megtagadva! Kérlek engedélyezd a beállításokban.');
            }
        });
}

export async function stopRecording(apiUrl, accessToken) {
    return new Promise(async (resolve, reject) => {
        if (!mediaRecorder || mediaRecorder.state === 'inactive') {
            resolve('');
            return;
        }

        if (recordingInterval) {
            clearTimeout(recordingInterval);
            recordingInterval = null;
        }

        mediaRecorder.onstop = async () => {
            try {
                console.log('🎤 Recording stopped, processing...');

                // Összes chunk egyesítése
                const audioBlob = new Blob(audioChunks, { type: 'audio/webm' });

                if (audioBlob.size === 0) {
                    console.warn('Empty audio blob');
                    resolve('');
                    return;
                }

                console.log(`📦 Audio size: ${audioBlob.size} bytes`);

                // FormData készítése
                const formData = new FormData();
                formData.append('audioChunk', audioBlob, 'recording.webm');
                formData.append('language', 'hu');
                formData.append('cleanText', 'true');

                // API hívás
                const response = await fetch(`${apiUrl}/api/SpeechToText/transcribe-chunk`, {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${accessToken}`
                    },
                    body: formData
                });

                if (!response.ok) {
                    const errorText = await response.text();
                    console.error('API Error:', errorText);
                    reject(`API hiba: ${response.status} - ${errorText}`);
                    return;
                }

                const result = await response.json();
                console.log('✅ Transcription result:', result);

                if (result && result.text) {
                    resolve(result.text);
                } else {
                    resolve('');
                }

            } catch (error) {
                console.error('Error processing audio:', error);
                reject(error.message);
            } finally {
                // Cleanup
                audioChunks = [];
                if (mediaRecorder && mediaRecorder.stream) {
                    mediaRecorder.stream.getTracks().forEach(track => track.stop());
                }
                mediaRecorder = null;
            }
        };

        mediaRecorder.stop();
    });
}

// Cleanup function
export function cleanup() {
    if (recordingInterval) {
        clearTimeout(recordingInterval);
    }
    if (mediaRecorder && mediaRecorder.state === 'recording') {
        mediaRecorder.stop();
    }
    audioChunks = [];
    dotNetHelper = null;
}