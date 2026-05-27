using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Match
{
    public class WireLogic : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RadarObjective _radar;
        private RectTransform _startPoint;
        private RectTransform _endPoint;
        private Sprite _symbol;
        
        public Image startBg;
        public Image startIcon;
        public Image endBg;
        public Image endIcon;
        public Image wireLine;

        private bool _isMatched = false;

        public void Setup(RadarObjective radar, RectTransform start, RectTransform end, Sprite symbol, Color wireColor)
        {
            _radar = radar;
            _startPoint = start;
            _endPoint = end;
            _symbol = symbol;

            // Place Icons and Backgrounds
            if (startBg) startBg.transform.position = start.position;
            if (endBg) endBg.transform.position = end.position;
            
            startIcon.transform.position = start.position;
            endIcon.transform.position = end.position;
            
            startIcon.sprite = symbol;
            endIcon.sprite = symbol;

            if (startBg) startBg.color = wireColor;
            if (endBg) endBg.color = wireColor;
            
            // Keep the symbol image pure white so its original black pixels show correctly
            startIcon.color = Color.white;
            endIcon.color = Color.white;
            
            wireLine.color = wireColor;
            wireLine.gameObject.SetActive(false);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isMatched) return;
            wireLine.gameObject.SetActive(true);
            wireLine.transform.position = _startPoint.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isMatched) return;
            
            Vector3 mousePos = eventData.position;
            Vector3 startPos = _startPoint.position;

            // Simple line renderer using UI Image stretching and rotating
            Vector3 dir = mousePos - startPos;
            float dist = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            wireLine.rectTransform.sizeDelta = new Vector2(dist, 10f); // 10f thickness
            wireLine.rectTransform.rotation = Quaternion.Euler(0, 0, angle);
            wireLine.rectTransform.position = startPos + dir / 2f;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_isMatched) return;

            // Check if dropped near the end point
            float dist = Vector3.Distance(eventData.position, _endPoint.position);
            if (dist < 50f) // 50 pixels tolerance
            {
                _isMatched = true;
                // Snap line
                Vector3 dir = _endPoint.position - _startPoint.position;
                wireLine.rectTransform.sizeDelta = new Vector2(dir.magnitude, 10f);
                wireLine.rectTransform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                wireLine.rectTransform.position = _startPoint.position + dir / 2f;
                
                _radar.OnWireConnected();
            }
            else
            {
                wireLine.gameObject.SetActive(false);
            }
        }
    }
}
