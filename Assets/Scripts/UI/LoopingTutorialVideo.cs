using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace UI
{
    /// <summary>
    /// Plays one looping clip into a RawImage, for a tutorial page.
    ///
    /// Driven from OnEnable/OnDisable so it follows the page being shown or hidden, with no need
    /// for the tutorial to know videos exist. Only the visible page decodes.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class LoopingTutorialVideo : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("File name inside StreamingAssets, e.g. tutorial_page1.mp4")]
        private string _fileName;

        private RawImage _target;
        private VideoPlayer _videoPlayer;
        private RenderTexture _renderTexture;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(_fileName))
            {
                Debug.LogError("[LoopingTutorialVideo] No file name set.", this);
                return;
            }

            _target = GetComponent<RawImage>();

            _videoPlayer = gameObject.AddComponent<VideoPlayer>();
            _videoPlayer.playOnAwake = false;
            _videoPlayer.isLooping = true;

            // URL, not VideoClip: WebGL only supports URL playback, backed by the browser's video
            // element. StreamingAssets keeps the file inside the package, so this is not an
            // external network call.
            _videoPlayer.source = VideoSource.Url;
            _videoPlayer.url = Path.Combine(Application.streamingAssetsPath, _fileName);

            // Muted. Browsers block autoplay with sound, and a silently-refusing video is a
            // miserable thing to debug. The clips are screen captures; the audio is not wanted.
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.prepareCompleted += OnPrepared;
            _videoPlayer.errorReceived += OnError;
            _videoPlayer.Prepare();
        }

        private void OnPrepared(VideoPlayer player)
        {
            // Sized from the clip once known, rather than guessed up front: the two pages are
            // different resolutions, and a mismatched target would letterbox or stretch.
            _renderTexture = new RenderTexture((int)player.width, (int)player.height, 0)
            {
                name = $"{_fileName}_rt"
            };

            player.targetTexture = _renderTexture;

            _target.texture = _renderTexture;
            _target.color = Color.white;

            player.Play();
        }

        private void OnError(VideoPlayer player, string message)
        {
            Debug.LogError($"[LoopingTutorialVideo] {_fileName}: {message}", this);
        }

        private void OnDisable()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.prepareCompleted -= OnPrepared;
                _videoPlayer.errorReceived -= OnError;
                _videoPlayer.Stop();
                Destroy(_videoPlayer);
                _videoPlayer = null;
            }

            if (_renderTexture != null)
            {
                if (_target != null)
                {
                    _target.texture = null;
                }

                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }
        }
    }
}
