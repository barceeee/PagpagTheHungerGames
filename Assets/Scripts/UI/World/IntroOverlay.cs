using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>Paged story screen shown at the start of a new run (character lore, then Day 1).</summary>
    public class IntroOverlay : MonoBehaviour
    {
        [SerializeField] private Image bodyImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text pageText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text nextLabel;

        private List<(string title, string body)> _pages;
        private int _index;
        private Action _onDone;

        public void Show(Sprite body, List<(string title, string body)> pages, Action onDone)
        {
            _pages = pages;
            _onDone = onDone;
            _index = 0;
            bodyImage.sprite = body;
            bodyImage.preserveAspect = true;
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(Next);
            gameObject.SetActive(true);
            Render();
        }

        private void Next()
        {
            _index++;
            if (_index >= _pages.Count)
            {
                gameObject.SetActive(false);
                _onDone?.Invoke();
                return;
            }
            Render();
        }

        private void Render()
        {
            titleText.text = _pages[_index].title;
            bodyText.text = _pages[_index].body;
            pageText.text = $"{_index + 1} / {_pages.Count}";
            nextLabel.text = _index == _pages.Count - 1 ? "Begin" : "Next";
        }
    }
}
