using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;
using UnityEngine;

namespace RiskOfTraitors
{
    public class RoleDisplayUI : MonoBehaviour
    {
        private GameObject canvas;
        public GameObject windowPanel;
        public Text roleText;
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
            windowPanel.GetComponent<RectTransform>().anchorMin = new Vector2(0.05f, 0.33f);
            windowPanel.GetComponent<RectTransform>().anchorMax = new Vector2(0.2f, 0.4f);
            windowPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            // Create and add a Text UI element for the content
            roleText = new GameObject("roleText").AddComponent<Text>();
            roleText.transform.SetParent(windowPanel.transform);
            roleText.rectTransform.anchorMin = new Vector2(0.01f, 1f);
            roleText.rectTransform.anchorMax = new Vector2(0.01f, 1f);

            // Set to top left corner of window panel
            roleText.rectTransform.pivot = new Vector2(0f, 1f);
            roleText.rectTransform.anchoredPosition = Vector2.zero;

            // Color and stuff
            roleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            roleText.color = Color.white;
            roleText.fontSize = 22;


            // Set text content
            roleText.text = "Your text content here";

            // Enable horizontal and vertical overflow to allow word wrapping
            roleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            roleText.verticalOverflow = VerticalWrapMode.Overflow;

            // Set the size of the text element to fit within the window panel
            roleText.rectTransform.sizeDelta = new Vector2(windowPanel.GetComponent<RectTransform>().rect.width, windowPanel.GetComponent<RectTransform>().rect.height);



            // Adjust font size to fit text within the available space
            AdjustFontSizeToFit(roleText);

            // Set initial visibility of the window panel
            windowPanel.SetActive(false);
        }

        private void AdjustFontSizeToFit(Text roleText)
        {
            //int maxFontSize = 28; // Adjust as needed

            while (roleText.preferredHeight > roleText.rectTransform.rect.height && roleText.fontSize > 12)
            {
                roleText.fontSize--;
            }
        }


        public void UpdateTextTraitor(string traitorTeam)
        {
            roleText.text = "Role: Traitor\n" +
                " " + traitorTeam;
            roleText.color = Color.red;
            AdjustFontSizeToFit(roleText);
        }

        public void UpdateTextInnocent()
        {
            roleText.text = "Role: Innocent";
            roleText.color = Color.green;
            AdjustFontSizeToFit(roleText);
        }


        public void ToggleWindow()
        {
            windowVisible = !windowVisible;
            windowPanel.SetActive(windowVisible);
        }
    }
}
