using UnityEngine.UI;
using UnityEngine;

namespace RiskOfTraitors
{
    public class CustomGUI : MonoBehaviour
    {
        private GameObject canvas;
        public GameObject windowPanel;
        public Text textElement;
        public bool windowVisible = false;

        public void Init()
        {
            // Create canvas
            canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            // Create window panel
            windowPanel = new GameObject("WindowPanel");
            windowPanel.AddComponent<CanvasRenderer>();
            windowPanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
            windowPanel.transform.SetParent(canvas.transform);

            // Set panel size and position
            windowPanel.GetComponent<RectTransform>().anchorMin = new Vector2(0.25f, 0.1f);
            windowPanel.GetComponent<RectTransform>().anchorMax = new Vector2(0.75f, 0.9f);
            windowPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            // Create and add a Text UI element for the content
            textElement = new GameObject("TextElement").AddComponent<Text>();
            textElement.transform.SetParent(windowPanel.transform);
            textElement.rectTransform.anchorMin = new Vector2(0.01f, 1f);
            textElement.rectTransform.anchorMax = new Vector2(0.01f, 1f);

            // Set to top left corner of window panel
            textElement.rectTransform.pivot = new Vector2(0f, 1f);
            textElement.rectTransform.anchoredPosition = Vector2.zero;

            // Color and stuff
            textElement.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textElement.color = Color.white;
            textElement.fontSize = 28;


            // Set text content
            textElement.text = "Your text content here";

            // Enable horizontal and vertical overflow to allow word wrapping
            textElement.horizontalOverflow = HorizontalWrapMode.Wrap;
            textElement.verticalOverflow = VerticalWrapMode.Overflow;

            // Set the size of the text element to fit within the window panel
            textElement.rectTransform.sizeDelta = new Vector2(windowPanel.GetComponent<RectTransform>().rect.width, windowPanel.GetComponent<RectTransform>().rect.height);



            // Adjust font size to fit text within the available space
            AdjustFontSizeToFit(textElement);

            // Set initial visibility of the window panel
            windowPanel.SetActive(false);
        }

        private void AdjustFontSizeToFit(Text textElement)
        {
            //int maxFontSize = 28; // Adjust as needed

            while (textElement.preferredHeight > textElement.rectTransform.rect.height && textElement.fontSize > 12)
            {
                textElement.fontSize--;
            }
        }


        public void UpdateTextContent(string newText)
        {
            textElement.text = newText;
            AdjustFontSizeToFit(textElement);
        }


        public void ToggleWindow()
        {
            windowVisible = !windowVisible;
            windowPanel.SetActive(windowVisible);
        }
    }


}
