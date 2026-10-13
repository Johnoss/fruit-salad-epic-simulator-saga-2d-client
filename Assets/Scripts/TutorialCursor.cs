using UnityEngine;

/// <summary>
/// TEMPORARY - for recording a tutorial video. Delete this file and
/// Assets/Resources/tutorial_cursor.png when the recording is done.
///
/// Swaps the system cursor for the pointing hand. Self-installing via
/// RuntimeInitializeOnLoadMethod, so it needs no scene wiring - which also means removing it is
/// just deleting the two files, with nothing left dangling in the scene.
/// </summary>
public static class TutorialCursor
{
    private const string TextureResourcePath = "tutorial_cursor";

    /// <summary>
    /// Where the click actually lands, as a fraction of the texture. Fractional rather than pixels
    /// so it stays correct if the import size changes: the index fingertip sits about 41% across
    /// and near the very top of the image.
    /// </summary>
    private static readonly Vector2 HotspotFraction = new(0.41f, 0.03f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Apply()
    {
        var cursorTexture = Resources.Load<Texture2D>(TextureResourcePath);

        if (cursorTexture == null)
        {
            Debug.LogWarning($"[TutorialCursor] No texture at Resources/{TextureResourcePath}.");
            return;
        }

        var hotspot = new Vector2(
            cursorTexture.width * HotspotFraction.x,
            cursorTexture.height * HotspotFraction.y);

        // Auto so the platform uses a hardware cursor where it can - software cursors lag a frame
        // behind the pointer, which is exactly the artefact you do not want on camera.
        Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
    }

    /// Restores the default pointer without needing a domain reload.
    public static void Restore()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
