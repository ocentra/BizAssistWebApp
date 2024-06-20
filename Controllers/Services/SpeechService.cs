using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;


namespace BizAssistWebApp.Controllers.Services
{
    public class SpeechToTextService(string subscriptionKey, string region)
    {
        private readonly SpeechConfig _speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);

        public async Task<string> ConvertSpeechToTextAsync(Stream audioStream)
        {
            using AudioConfig? audioInput = AudioConfig.FromStreamInput(new CustomAudioInputStream(audioStream));
            using SpeechRecognizer recognizer = new SpeechRecognizer(_speechConfig, audioInput);

            SpeechRecognitionResult? result = await recognizer.RecognizeOnceAsync();

            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                return result.Text;
            }
            else if (result.Reason == ResultReason.NoMatch)
            {
                return "No speech could be recognized.";
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                CancellationDetails? cancellation = CancellationDetails.FromResult(result);
                return $"Recognition canceled: {cancellation.Reason}. Error details: {cancellation.ErrorDetails}";
            }

            return "Unknown error.";
        }
    }

    public class CustomAudioInputStream(Stream audioStream) : PullAudioInputStreamCallback
    {
        public override int Read(byte[] dataBuffer, uint size)
        {
            return audioStream.Read(dataBuffer, 0, (int)size);
        }

        public override void Close()
        {
            audioStream.Close();
            base.Close();
        }
    }

    public class TextToSpeechService(string subscriptionKey, string region)
    {
        private readonly SpeechConfig _speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);

        public async Task SpeakTextAsync(string text)
        {
            using SpeechSynthesizer synthesizer = new SpeechSynthesizer(_speechConfig, null);
            await synthesizer.SpeakTextAsync(text);
        }
    }
}

