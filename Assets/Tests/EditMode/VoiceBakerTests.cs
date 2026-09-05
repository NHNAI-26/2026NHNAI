using System;
using System.Collections.Generic;
using System.Reflection;
using Border.Voice.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Border.Voice.Tests
{
    public sealed class VoiceBakerTests
    {
        private const int Frequency = VoiceBaker.OutputFrequency;
        private const float SecondsPerChar = 0.1f;
        private const int Spacing = (int)(SecondsPerChar * Frequency);

        private readonly List<UnityEngine.Object> spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object item in spawned)
            {
                if (item != null)
                {
                    UnityEngine.Object.DestroyImmediate(item);
                }
            }

            spawned.Clear();
        }

        /// <summary>
        /// 배치는 글자 인덱스 기준이어야 한다. 음절 카운터로 세면 공백이 든 대사에서 뒤로 갈수록 밀린다 —
        /// 짧은 문장에서는 멀쩡하게 들려서 놓치기 쉬운 버그다.
        /// </summary>
        [Test]
        public void Bake_PlacesOneSyllablePerCharacterIndexAndLeavesWhitespaceSilent()
        {
            VoicePresetSO preset = CreatePreset(new[] { CreateImpulseClip("Impulse", Frequency) },
                pitchRange: new Vector2(1f, 1f), volumeJitter: 0f);

            Assert.That(VoiceBaker.TryBake(preset, "ab c", 1, out float[] samples, out string error),
                Is.True, error);

            Assert.That(samples[0], Is.Not.EqualTo(0f), "첫 글자 위치에 소리가 없다.");
            Assert.That(samples[Spacing], Is.Not.EqualTo(0f), "두 번째 글자 위치에 소리가 없다.");
            Assert.That(samples[Spacing * 2], Is.EqualTo(0f), "공백 자리가 무음이 아니다.");
            Assert.That(samples[Spacing * 3], Is.Not.EqualTo(0f),
                "네 번째 글자 'c' 가 공백을 건너뛰고 앞으로 당겨졌다.");
        }

        [Test]
        public void Bake_IsDeterministicForSameSeedAndDiffersForAnother()
        {
            VoicePresetSO preset = CreatePreset(
                new[] { CreateImpulseClip("A", Frequency), CreateNoiseClip("B", Frequency) },
                pitchRange: new Vector2(0.8f, 1.2f), volumeJitter: 0.2f);

            Assert.That(VoiceBaker.TryBake(preset, "가나다라마바사", 1234, out float[] first, out string error),
                Is.True, error);
            Assert.That(VoiceBaker.TryBake(preset, "가나다라마바사", 1234, out float[] again, out error),
                Is.True, error);
            Assert.That(VoiceBaker.TryBake(preset, "가나다라마바사", 5678, out float[] other, out error),
                Is.True, error);

            Assert.That(again, Is.EqualTo(first), "같은 시드인데 결과가 다르다.");
            Assert.That(other, Is.Not.EqualTo(first), "시드를 바꿨는데 결과가 같다.");
        }

        [Test]
        public void Bake_ResamplesClipsRecordedAtAnotherSampleRate()
        {
            VoicePresetSO preset = CreatePreset(new[] { CreateImpulseClip("Half", Frequency / 2) },
                pitchRange: new Vector2(1f, 1f), volumeJitter: 0f);

            Assert.That(VoiceBaker.TryBake(preset, "a", 1, out float[] samples, out string error),
                Is.True, error);

            // 22050Hz 짜리 4 프레임은 44100Hz 출력에서 두 배 길이가 되어야 한다.
            Assert.That(samples.Length, Is.EqualTo(8));
        }

        [Test]
        public void Bake_FailsWithMessageWhenPresetHasNoSyllables()
        {
            VoicePresetSO preset = CreatePreset(Array.Empty<AudioClip>(),
                pitchRange: new Vector2(1f, 1f), volumeJitter: 0f);

            Assert.That(VoiceBaker.TryBake(preset, "a", 1, out float[] samples, out string error), Is.False);
            Assert.That(samples, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }

        /// <summary>
        /// 헤더가 한 바이트라도 밀리면 Unity 가 임포트를 거부하고, 32768 로 스케일하면 +1.0 이 부호를 뒤집어 딱 소리를 낸다.
        /// </summary>
        [Test]
        public void EncodeWav16_WritesCanonicalHeaderAndClampsFullScaleWithoutWrapping()
        {
            byte[] wav = VoiceBaker.EncodeWav16(new[] { 1f, -1f, 2f, -2f }, 1, Frequency);

            Assert.That(wav.Length, Is.EqualTo(44 + 8));
            Assert.That(System.Text.Encoding.ASCII.GetString(wav, 0, 4), Is.EqualTo("RIFF"));
            Assert.That(BitConverter.ToInt32(wav, 4), Is.EqualTo(36 + 8));
            Assert.That(System.Text.Encoding.ASCII.GetString(wav, 8, 4), Is.EqualTo("WAVE"));
            Assert.That(System.Text.Encoding.ASCII.GetString(wav, 12, 4), Is.EqualTo("fmt "));
            Assert.That(BitConverter.ToInt32(wav, 16), Is.EqualTo(16));
            Assert.That(BitConverter.ToInt16(wav, 20), Is.EqualTo(1), "PCM 포맷 코드가 아니다.");
            Assert.That(BitConverter.ToInt16(wav, 22), Is.EqualTo(1));
            Assert.That(BitConverter.ToInt32(wav, 24), Is.EqualTo(Frequency));
            Assert.That(BitConverter.ToInt32(wav, 28), Is.EqualTo(Frequency * 2));
            Assert.That(BitConverter.ToInt16(wav, 32), Is.EqualTo(2));
            Assert.That(BitConverter.ToInt16(wav, 34), Is.EqualTo(16));
            Assert.That(System.Text.Encoding.ASCII.GetString(wav, 36, 4), Is.EqualTo("data"));
            Assert.That(BitConverter.ToInt32(wav, 40), Is.EqualTo(8));

            Assert.That(BitConverter.ToInt16(wav, 44), Is.EqualTo(32767));
            Assert.That(BitConverter.ToInt16(wav, 46), Is.EqualTo(-32767));
            Assert.That(BitConverter.ToInt16(wav, 48), Is.EqualTo(32767), "+1 을 넘는 값이 랩어라운드했다.");
            Assert.That(BitConverter.ToInt16(wav, 50), Is.EqualTo(-32767));
        }

        private VoicePresetSO CreatePreset(AudioClip[] syllables, Vector2 pitchRange, float volumeJitter)
        {
            var preset = ScriptableObject.CreateInstance<VoicePresetSO>();
            spawned.Add(preset);
            SetField(preset, "syllables", syllables);
            SetField(preset, "pitchRange", pitchRange);
            SetField(preset, "secondsPerChar", SecondsPerChar);
            SetField(preset, "volume", 1f);
            SetField(preset, "volumeJitter", volumeJitter);
            return preset;
        }

        private AudioClip CreateImpulseClip(string name, int frequency) =>
            CreateClip(name, frequency, new[] { 1f, 0f, 0f, 0f });

        private AudioClip CreateNoiseClip(string name, int frequency) =>
            CreateClip(name, frequency, new[] { 0.5f, -0.5f, 0.25f, -0.25f });

        private AudioClip CreateClip(string name, int frequency, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, frequency, false);
            clip.SetData(data, 0);
            spawned.Add(clip);
            return clip;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing serialized field '{name}'.");
            field.SetValue(target, value);
        }
    }
}
