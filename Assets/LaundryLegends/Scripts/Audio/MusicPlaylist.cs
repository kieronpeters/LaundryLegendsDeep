using UnityEngine;

namespace LaundryLegends.Audio
{
    /// <summary>Sequential ambient music playlist with a single AudioSource.</summary>
    [DisallowMultipleComponent]
    public sealed class MusicPlaylist : MonoBehaviour
    {
        [SerializeField] private AudioClip[] _clips;
        [SerializeField] private AudioSource _source;

        private int _index;

        public int ClipCount => _clips != null ? _clips.Length : 0;

        public void Configure(AudioSource source, params AudioClip[] clips)
        {
            _source = source;
            _clips = clips ?? new AudioClip[0];
        }

        private void Start()
        {
            if (_source == null)
            {
                _source = GetComponent<AudioSource>();
            }

            if (_source != null && _clips != null && _clips.Length > 0)
            {
                _source.loop = false;
                _source.clip = _clips[0];
                _source.Play();
            }
        }

        private void Update()
        {
            if (_source == null || _clips == null || _clips.Length == 0 || _source.isPlaying)
            {
                return;
            }

            _index = (_index + 1) % _clips.Length;
            _source.clip = _clips[_index];
            _source.Play();
        }
    }
}
