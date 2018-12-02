using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sone.UI
{
    public static class UI
    {
        private static Texture2D m_whiteTexture;

        /// <summary>
        /// A plain white texture.
        /// </summary>
        public static Texture2D WhiteTexture
        {
            get
            {
                if (m_whiteTexture == null)
                {
                    m_whiteTexture = new Texture2D(1, 1);
                    m_whiteTexture.SetPixel(0, 0, Color.white);
                    m_whiteTexture.Apply();
                }

                return m_whiteTexture;
            }
        }

        #region Text

        /// <summary>
        /// Returns the actual width of the given string, in the given font.
        /// </summary>
        public static float GetTextWidth(Font font, string str)
        {
            float sum = 0f;

            CharacterInfo info;

            foreach (char c in str.ToCharArray())
            {
                font.GetCharacterInfo(c, out info);
                sum += info.advance;
            }

            return sum;
        }

        #endregion
        
        #region Canvases

        /// <summary>
        /// Toggle a canvas group's alpha, interactable, and raycast blocking.
        /// </summary>
        public static void Toggle(this CanvasGroup group, bool enabled, bool toggleBlocksRaycasts = true)
        {
            if (group == null)
                return;

            group.alpha = enabled ? 1f : 0f;

            if (toggleBlocksRaycasts)
            {
                group.interactable = enabled;
                group.blocksRaycasts = enabled;
            }
        }
        
        #endregion

        #region Images

        /// <summary>
        /// Set the alpha of this image.
        /// </summary>
        public static void SetAlpha(this Image image, float a)
        {
            Color colour = image.color;
            colour.a = a;
            image.color = colour;
        }
        
        #endregion
        
        #region UI Components

        /// <summary>
        /// Set the scroll rect to the top.
        /// </summary>
        public static void ScrollToTop(this ScrollRect scrollRect)
        {
            scrollRect.normalizedPosition = new Vector2(0, 1);
        }

        /// <summary>
        /// Set the scroll rect to the bottom.
        /// </summary>
        public static void ScrollToBottom(this ScrollRect scrollRect)
        {
            scrollRect.normalizedPosition = Vector2.zero;
        }
        
        #endregion
    }

}