namespace Speak
{
    using System.Collections;
    using System.Reflection;
    using System.Speech.AudioFormat;
    using System.Speech.Synthesis;

    public static class SpeechApiReflectionHelper
    {
        private const string PROP_VOICE_SYNTHESIZER = "VoiceSynthesizer";
        private const string FIELD_INSTALLED_VOICES = "_installedVoices";

        private const string ONE_CORE_VOICES_REGISTRY = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Voices";

        private static readonly Type ObjectTokenCategoryType = typeof(SpeechSynthesizer).Assembly
            .GetType("System.Speech.Internal.ObjectTokens.ObjectTokenCategory")!;

        private static readonly Type VoiceInfoType = typeof(SpeechSynthesizer).Assembly
            .GetType("System.Speech.Synthesis.VoiceInfo")!;

        private static readonly Type InstalledVoiceType = typeof(SpeechSynthesizer).Assembly
            .GetType("System.Speech.Synthesis.InstalledVoice")!;

        public static void PrintInstalledVoicesInfo(this SpeechSynthesizer synthesizer)
        {
            if (!OperatingSystem.IsWindows())
                return;

            // Output information about all of the installed voices.
            Console.WriteLine("Installed voices -");
            foreach (InstalledVoice voice in synthesizer.GetInstalledVoices())
            {
                VoiceInfo info = voice.VoiceInfo;
                string AudioFormats = "";
                foreach (SpeechAudioFormatInfo fmt in info.SupportedAudioFormats)
                {
                    AudioFormats += string.Format("{0}\n",
                    fmt.EncodingFormat.ToString());
                }

                Console.WriteLine(" Name:          " + info.Name);
                Console.WriteLine(" Culture:       " + info.Culture);
                Console.WriteLine(" Age:           " + info.Age);
                Console.WriteLine(" Gender:        " + info.Gender);
                Console.WriteLine(" Description:   " + info.Description);
                Console.WriteLine(" ID:            " + info.Id);
                Console.WriteLine(" Enabled:       " + voice.Enabled);
                if (info.SupportedAudioFormats.Count != 0)
                {
                    Console.WriteLine(" Audio formats: " + AudioFormats);
                }
                else
                {
                    Console.WriteLine(" No supported audio formats found");
                }

                string AdditionalInfo = "";
                foreach (string key in info.AdditionalInfo.Keys)
                {
                    AdditionalInfo += string.Format("  {0}: {1}\n", key, info.AdditionalInfo[key]);
                }

                if (!string.IsNullOrEmpty(AdditionalInfo))
                    Console.WriteLine(" Additional Info - " + AdditionalInfo);

                Console.WriteLine();
            }
        }


        public static void InjectOneCoreVoices(this SpeechSynthesizer synthesizer)
        {
            var voiceSynthesizer = GetProperty(synthesizer, PROP_VOICE_SYNTHESIZER);
            if (voiceSynthesizer == null) throw new NotSupportedException($"Property not found: {PROP_VOICE_SYNTHESIZER}");

            var installedVoices = GetField(voiceSynthesizer, FIELD_INSTALLED_VOICES) as IList;
            if (installedVoices == null)
                throw new NotSupportedException($"Field not found or null: {FIELD_INSTALLED_VOICES}");

            if (ObjectTokenCategoryType
                    .GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)?
                    .Invoke(null, new object?[] { ONE_CORE_VOICES_REGISTRY }) is not IDisposable otc)
                throw new NotSupportedException($"Failed to call Create on {ObjectTokenCategoryType} instance");

            using (otc)
            {
                if (ObjectTokenCategoryType
                        .GetMethod("FindMatchingTokens", BindingFlags.Instance | BindingFlags.NonPublic)?
                        .Invoke(otc, new object?[] { null, null }) is not IList tokens)
                    throw new NotSupportedException($"Failed to list matching tokens");

                foreach (var token in tokens)
                {
                    if (token == null || GetProperty(token, "Attributes") == null) continue;

                    var voiceInfo =
                        typeof(SpeechSynthesizer).Assembly
                            .CreateInstance(VoiceInfoType.FullName!, true,
                                BindingFlags.Instance | BindingFlags.NonPublic, null,
                                new object[] { token }, null, null);

                    if (voiceInfo == null)
                        throw new NotSupportedException($"Failed to instantiate {VoiceInfoType}");

                    var installedVoice =
                        typeof(SpeechSynthesizer).Assembly
                            .CreateInstance(InstalledVoiceType.FullName!, true,
                                BindingFlags.Instance | BindingFlags.NonPublic, null,
                                new object[] { voiceSynthesizer, voiceInfo }, null, null);

                    if (installedVoice == null)
                        throw new NotSupportedException($"Failed to instantiate {InstalledVoiceType}");

                    installedVoices.Add(installedVoice);
                }
            }
        }

        private static object? GetProperty(object target, string propName)
        {
            return target.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);
        }

        private static object? GetField(object target, string propName)
        {
            return target.GetType().GetField(propName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);
        }
    }
}
