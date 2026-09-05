using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Border.Voice.Editor
{
    /// <summary>
    /// 대사 한 줄을 음절 샘플들로 합성한다. 동물의 숲 / 데이브 더 다이버식 지껄임.
    /// UI 에 의존하지 않는 순수 로직이라 EditMode 테스트로 그대로 검증한다.
    /// </summary>
    public static class VoiceBaker
    {
        /// <summary>출력은 항상 모노 44100Hz 다. 입력 클립이 뭐든 리샘플로 흡수하므로 형식 불일치가 존재하지 않는다.</summary>
        public const int OutputFrequency = 44100;

        /// <summary>
        /// <paramref name="text"/> 의 공백 아닌 글자마다 음절 하나를 무작위로 골라 배속을 흔들어 이어 붙인다.
        /// 배치 위치는 글자 인덱스 기준이라 공백도 제 몫의 시간을 차지한다 — 그래야 타자기 연출과 어긋나지 않는다.
        /// </summary>
        /// <param name="seed">0 이면 매번 다른 소리. 그 외에는 같은 입력이 항상 같은 결과를 낸다.</param>
        public static bool TryBake(VoicePresetSO preset, string text, int seed,
            out float[] samples, out string error)
        {
            samples = null;
            error = null;

            if (preset == null)
            {
                error = "프리셋이 없다.";
                return false;
            }

            AudioClip[] clips = preset.Syllables;
            if (clips == null || clips.Length == 0)
            {
                error = "프리셋에 음절 샘플이 하나도 없다.";
                return false;
            }

            if (string.IsNullOrEmpty(text))
            {
                error = "대사가 비어 있다.";
                return false;
            }

            float minPitch = Mathf.Min(preset.PitchRange.x, preset.PitchRange.y);
            float maxPitch = Mathf.Max(preset.PitchRange.x, preset.PitchRange.y);
            if (minPitch <= 0.01f)
            {
                error = $"배속 범위가 잘못됐다({minPitch:0.###} ~ {maxPitch:0.###}). 0.01 보다 커야 한다.";
                return false;
            }

            // 모노로 미리 펼쳐 둔다. 클립마다 주파수가 달라도 리샘플 단계에서 흡수한다.
            var monos = new float[clips.Length][];
            var sourceRates = new int[clips.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                if (!TryReadMono(clips[i], out monos[i], out string clipError))
                {
                    error = clipError;
                    return false;
                }

                sourceRates[i] = clips[i].frequency;
            }

            var random = new System.Random(seed != 0 ? seed : Environment.TickCount);
            float spacing = preset.SecondsPerChar * OutputFrequency;

            // 1 패스: 배치를 먼저 정해 전체 길이를 구한다. 가산 믹스라 버퍼를 미리 잡아야 한다.
            var starts = new int[text.Length];
            var indices = new int[text.Length];
            var steps = new float[text.Length];
            var gains = new float[text.Length];
            var lengths = new int[text.Length];
            int total = 0;

            for (int c = 0; c < text.Length; c++)
            {
                if (char.IsWhiteSpace(text[c]))
                {
                    lengths[c] = 0;
                    continue;
                }

                int clipIndex = random.Next(clips.Length);
                float pitch = Lerp(random, minPitch, maxPitch);
                float step = pitch * sourceRates[clipIndex] / (float)OutputFrequency;
                int frames = monos[clipIndex].Length;
                int length = Mathf.FloorToInt(frames / step);
                if (length <= 0)
                {
                    continue;
                }

                float jitter = preset.VolumeJitter <= 0f
                    ? 1f
                    : Lerp(random, 1f - preset.VolumeJitter, 1f + preset.VolumeJitter);

                starts[c] = Mathf.RoundToInt(c * spacing);
                indices[c] = clipIndex;
                steps[c] = step;
                gains[c] = preset.Volume * jitter;
                lengths[c] = length;
                total = Mathf.Max(total, starts[c] + length);
            }

            if (total <= 0)
            {
                error = "소리가 나는 글자가 하나도 없다. 공백뿐인 대사이거나 샘플이 비었다.";
                return false;
            }

            // 2 패스: 선형 보간 리샘플 후 가산 믹스. 배속을 바꾸면 길이도 같이 변하는 게 이 소리의 핵심이다.
            var buffer = new float[total];
            for (int c = 0; c < text.Length; c++)
            {
                int length = lengths[c];
                if (length <= 0)
                {
                    continue;
                }

                float[] mono = monos[indices[c]];
                int last = mono.Length - 1;
                float step = steps[c];
                float gain = gains[c];
                int start = starts[c];

                for (int o = 0; o < length; o++)
                {
                    float position = o * step;
                    int i0 = (int)position;
                    int i1 = Mathf.Min(i0 + 1, last);
                    buffer[start + o] += Mathf.Lerp(mono[i0], mono[i1], position - i0) * gain;
                }
            }

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = Mathf.Clamp(buffer[i], -1f, 1f);
            }

            samples = buffer;
            return true;
        }

        /// <summary>
        /// 클립을 모노 float 로 읽는다. 압축 클립은 Decompress On Load 여야만 GetData 가 실제 파형을 준다 —
        /// 아니면 0 으로 채워져 무음이 구워지므로 GetData 의 반환값을 반드시 본다.
        /// </summary>
        public static bool TryReadMono(AudioClip clip, out float[] mono, out string error)
        {
            mono = null;
            error = null;

            if (clip == null)
            {
                error = "음절 샘플 목록에 빈 칸이 있다.";
                return false;
            }

            if (clip.loadState != AudioDataLoadState.Loaded && !clip.LoadAudioData())
            {
                error = $"'{clip.name}' 의 오디오 데이터를 불러오지 못했다.";
                return false;
            }

            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                error = $"'{clip.name}' 이 아직 로딩 중이다. 인스펙터에서 Load In Background 를 꺼라.";
                return false;
            }

            int channels = Mathf.Max(1, clip.channels);
            var interleaved = new float[clip.samples * channels];
            if (!clip.GetData(interleaved, 0))
            {
                error = $"'{clip.name}' 에서 파형을 읽지 못했다(Load Type 이 {clip.loadType}). " +
                        "인스펙터에서 Decompress On Load 로 바꿔라 — Streaming 과 Compressed In Memory 는 읽을 수 없다.";
                return false;
            }

            if (channels == 1)
            {
                mono = interleaved;
                return true;
            }

            mono = new float[clip.samples];
            for (int f = 0; f < mono.Length; f++)
            {
                float sum = 0f;
                for (int k = 0; k < channels; k++)
                {
                    sum += interleaved[f * channels + k];
                }

                mono[f] = sum / channels;
            }

            return true;
        }

        /// <summary>16-bit PCM RIFF WAV 로 인코딩한다. 44 바이트 표준 헤더 + 인터리브 샘플.</summary>
        public static byte[] EncodeWav16(float[] samples, int channels, int frequency)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            int dataBytes = samples.Length * 2;
            using var stream = new MemoryStream(44 + dataBytes);
            using var writer = new BinaryWriter(stream, Encoding.ASCII);

            // BinaryWriter.Write(string) 은 길이 접두사를 붙인다. 청크 ID 는 반드시 바이트로 써야 한다.
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataBytes);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(frequency);
            writer.Write(frequency * channels * 2);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataBytes);

            // 32768 이 아니라 32767 로 곱한다. +1.0 이 short 범위를 넘으면 부호가 뒤집혀 딱 소리가 난다.
            for (int i = 0; i < samples.Length; i++)
            {
                writer.Write((short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767f));
            }

            writer.Flush();
            return stream.ToArray();
        }

        private static float Lerp(System.Random random, float min, float max) =>
            min + (float)random.NextDouble() * (max - min);
    }
}
