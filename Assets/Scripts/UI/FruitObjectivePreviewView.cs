using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// One line of the level preview: a fruit and how many of it the level wants. No progress, no
    /// tick - this is the level before it starts, so there is nothing to be part way through.
    ///
    /// Not injected, like the objective bars: the panel owns however many of these a level needs
    /// and hands each one its fruit.
    /// </summary>
    public class FruitObjectivePreviewView : MonoBehaviour
    {
        [SerializeField]
        private Image _fruitIconImage;

        [SerializeField]
        [Tooltip("How many of the fruit the level asks for. Arg 0 is the count.")]
        private TextMeshProUGUI _countText;

        [SerializeField]
        private string _countFormat = "{0}";

        public void Show(Sprite fruitSprite, int requiredCount)
        {
            gameObject.SetActive(true);

            if (_fruitIconImage != null && fruitSprite != null)
            {
                _fruitIconImage.sprite = fruitSprite;
            }

            if (_countText != null)
            {
                _countText.text = string.Format(_countFormat, requiredCount);
            }
        }

        /// Disabled rather than blanked, so a layout group closes the gap.
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
