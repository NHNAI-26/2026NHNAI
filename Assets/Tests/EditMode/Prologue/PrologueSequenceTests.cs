using NUnit.Framework;
using UnityEditor;

namespace Border.Prologue.Tests
{
    /// <summary>
    /// 저작된 프롤로그 에셋이 GDD 02 §2 의 스펙(5컷, 20~30초)을 벗어나지 않는지 잠근다.
    /// 인스펙터에서 컷 길이를 만지다 예산을 깨는 것이 현실적인 실패 모드라서 데이터를 검사한다.
    /// </summary>
    public class PrologueSequenceTests
    {
        private const string AssetPath = "Assets/02. ScriptableObjects/Prologue/PrologueSequence.asset";

        // GDD 02 §2: "프롤로그는 20~30초 안에 끝난다."
        private const float MinSeconds = 20f;
        private const float MaxSeconds = 30f;

        // GDD 02 §2 권장 순서의 컷 수. 1 통신음 / 2 2017.12 / 3 지시 / 4 프로젝트명 / 5 전환.
        private const int ExpectedBeatCount = 5;

        private static PrologueSequenceSO LoadSequence()
        {
            var sequence = AssetDatabase.LoadAssetAtPath<PrologueSequenceSO>(AssetPath);
            Assert.That(sequence, Is.Not.Null, $"프롤로그 시퀀스 에셋을 찾지 못했다: {AssetPath}");
            return sequence;
        }

        [Test]
        public void SequenceHasEveryBeatFromTheDesignDocument()
        {
            PrologueSequenceSO sequence = LoadSequence();

            Assert.That(sequence.Beats.Count, Is.EqualTo(ExpectedBeatCount),
                "GDD 02 §2 의 권장 순서는 5컷이다. 컷을 늘리거나 줄였다면 문서와 이 상수를 함께 고쳐야 한다.");
        }

        [Test]
        public void SequenceFitsInTheTwentyToThirtySecondBudget()
        {
            PrologueSequenceSO sequence = LoadSequence();

            Assert.That(sequence.TotalSeconds, Is.InRange(MinSeconds, MaxSeconds),
                $"프롤로그 총 길이가 {sequence.TotalSeconds:0.0}초로 20~30초 예산을 벗어났다.");
        }
    }
}
