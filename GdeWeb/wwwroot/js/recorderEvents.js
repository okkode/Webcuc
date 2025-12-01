let recognition = null;
let isListening = false;
let dotNetReference = null;
let finalTranscript = '';
let interimTranscript = '';

export function startRecording(dotNetRef) {
    dotNetReference = dotNetRef;

    // Ellenőrizzük, hogy elérhető-e a Web Speech API
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;

    if (!SpeechRecognition) {
        if (dotNetReference) {
            dotNetReference.invokeMethodAsync('OnRecordingError', 'A böngésző nem támogatja a beszédfelismerést. Próbáld Chrome-ban!');
        }
        return;
    }

    try {
        recognition = new SpeechRecognition();

        // Beállítások
        recognition.continuous = true; // Folyamatos felismerés
        recognition.interimResults = true; // Köztes eredmények is
        recognition.lang = 'hu-HU'; // Magyar nyelv
        recognition.maxAlternatives = 1;

        // Visszaállítjuk a változókat
        finalTranscript = '';
        interimTranscript = '';

        // Eseménykezelők
        recognition.onstart = function () {
            isListening = true;
            console.log('Beszédfelismerés elindult');
        };

        recognition.onresult = function (event) {
            interimTranscript = '';

            console.log('Recognition result:', event.results.length);

            // Végigmegyünk az eredményeken
            for (let i = event.resultIndex; i < event.results.length; i++) {
                const transcript = event.results[i][0].transcript;

                if (event.results[i].isFinal) {
                    // Végleges eredmény
                    finalTranscript += transcript + ' ';
                    console.log('Final transcript:', finalTranscript);

                    // Elküldjük a .NET-nek a frissítést
                    if (dotNetReference) {
                        dotNetReference.invokeMethodAsync('OnTranscriptUpdate', finalTranscript.trim())
                            .catch(err => console.error('Error calling OnTranscriptUpdate:', err));
                    }
                } else {
                    // Köztes eredmény
                    interimTranscript += transcript;
                    console.log('Interim transcript:', interimTranscript);
                }
            }

            // Ha van köztes eredmény is, azt is elküldjük
            if (interimTranscript && dotNetReference) {
                dotNetReference.invokeMethodAsync('OnInterimTranscript', interimTranscript)
                    .catch(err => console.error('Error calling OnInterimTranscript:', err));
            }
        };

        recognition.onerror = function (event) {
            console.error('Beszédfelismerési hiba:', event.error);

            let errorMessage = 'Hiba történt a beszédfelismerés során.';

            switch (event.error) {
                case 'no-speech':
                    errorMessage = 'Nem észlelhető beszéd. Beszélj hangosabban!';
                    break;
                case 'audio-capture':
                    errorMessage = 'Nem érhető el a mikrofon. Ellenőrizd a beállításokat!';
                    break;
                case 'not-allowed':
                    errorMessage = 'Mikrofon hozzáférés megtagadva. Engedélyezd a böngészőben!';
                    break;
                case 'network':
                    errorMessage = 'Hálózati hiba történt.';
                    break;
                case 'aborted':
                    errorMessage = 'Beszédfelismerés megszakítva.';
                    break;
            }

            if (dotNetReference) {
                dotNetReference.invokeMethodAsync('OnRecordingError', errorMessage)
                    .catch(err => console.error('Error calling OnRecordingError:', err));
            }

            isListening = false;
        };

        recognition.onend = function () {
            console.log('Beszédfelismerés befejeződött. isListening:', isListening);

            // Ha még aktívan hallgatunk, újraindítjuk (folyamatos működéshez)
            if (isListening) {
                try {
                    console.log('Újraindítás...');
                    recognition.start();
                } catch (e) {
                    console.error('Nem sikerült újraindítani:', e);
                    isListening = false;
                }
            }
        };

        // Indítás
        recognition.start();
        console.log('Recognition started');

    } catch (error) {
        console.error('Hiba a beszédfelismerés inicializálásakor:', error);
        if (dotNetReference) {
            dotNetReference.invokeMethodAsync('OnRecordingError', 'Nem sikerült elindítani a beszédfelismerést: ' + error.message)
                .catch(err => console.error('Error calling OnRecordingError:', err));
        }
    }
}

export function stopRecording() {
    console.log('stopRecording called. finalTranscript:', finalTranscript);
    isListening = false;

    if (recognition) {
        try {
            recognition.stop();
            console.log('Recognition stopped');
        } catch (e) {
            console.error('Hiba a leállításkor:', e);
        }
    }

    // Visszaadjuk a teljes átírt szöveget
    const result = finalTranscript.trim();
    console.log('Returning transcript:', result);

    // NEM tisztítjuk még a változókat, mert lehet hogy még jön result event
    // finalTranscript = '';
    // interimTranscript = '';

    return result;
}

export function resetTranscript() {
    finalTranscript = '';
    interimTranscript = '';
    recognition = null;
    console.log('Transcript reset');
}

export function isRecordingActive() {
    return isListening;
}