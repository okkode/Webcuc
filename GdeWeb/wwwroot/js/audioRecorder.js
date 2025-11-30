window.AudioRecorder = {
    mediaRecorder: null,
    dotNetHelper: null,

    initialize: function (dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
    },

    startRecording: async function () {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({
                audio: {
                    channelCount: 1,
                    sampleRate: 16000
                }
            });

            this.mediaRecorder = new MediaRecorder(stream, {
                mimeType: 'audio/webm;codecs=opus'
            });

            this.mediaRecorder.ondataavailable = async (event) => {
                if (event.data.size > 0) {
                    await this.sendChunkToBackend(event.data);
                }
            };

            // 3 másodperces chunk-ok
            this.mediaRecorder.start(3000);
            console.log('🎤 Recording started');
        } catch (error) {
            console.error('❌ Microphone access denied:', error);
            alert('Kérlek engedélyezd a mikrofon hozzáférést!');
        }
    },

    stopRecording: function () {
        if (this.mediaRecorder && this.mediaRecorder.state !== 'inactive') {
            this.mediaRecorder.stop();
            this.mediaRecorder.stream.getTracks().forEach(track => track.stop());
            console.log('⏹️ Recording stopped');
        }
    },

    sendChunkToBackend: async function (audioBlob) {
        try {
            const arrayBuffer = await audioBlob.arrayBuffer();
            const uint8Array = new Uint8Array(arrayBuffer);

            await this.dotNetHelper.invokeMethodAsync(
                'ProcessAudioChunk',
                Array.from(uint8Array)
            );
        } catch (error) {
            console.error('❌ Error sending audio:', error);
        }
    }
};