namespace Border.Audio
{
    public readonly struct SoundHandle
    {
        private readonly PooledSfxVoice voice;
        private readonly int generation;

        internal SoundHandle(PooledSfxVoice voice, int generation)
        {
            this.voice = voice;
            this.generation = generation;
        }

        public static SoundHandle Invalid => default;
        public bool IsValid => voice != null && voice.IsGenerationValid(generation);
        public bool IsPlaying => voice != null && voice.IsPlaying(generation);

        public void Stop(float fadeSeconds = 0f)
        {
            if (voice != null)
            {
                voice.Stop(generation, fadeSeconds);
            }
        }

        public void SetVolume(float volume)
        {
            if (voice != null)
            {
                voice.SetVolume(generation, volume);
            }
        }

        public void SetPitch(float pitch)
        {
            if (voice != null)
            {
                voice.SetPitch(generation, pitch);
            }
        }

        public void SetLoop(bool loop)
        {
            if (voice != null)
            {
                voice.SetLoop(generation, loop);
            }
        }
    }
}
