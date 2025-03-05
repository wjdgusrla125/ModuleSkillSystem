using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 가상 조이스틱 UI 설정을 위한 클래스
public class JoystickUI : MonoBehaviour
{
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;
    
    [SerializeField] private bool fixedPosition = true;
    [SerializeField] private Color joystickBackgroundColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color joystickHandleColor = new Color(1f, 1f, 1f, 0.8f);
    
    private void Start()
    {
        // JoystickController와 UI 연결
        if (joystickBackground != null && joystickHandle != null && JoyStickController.Instance != null)
        {
            // Reflection을 통해 컨트롤러의 비공개 필드에 값 설정하는 방법 (직접 참조가 어려운 경우)
            var controller = JoyStickController.Instance;
            var backgroundField = controller.GetType().GetField("joystickBackground", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var handleField = controller.GetType().GetField("joystickHandle", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fixedField = controller.GetType().GetField("joystickFixed", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (backgroundField != null) backgroundField.SetValue(controller, joystickBackground);
            if (handleField != null) handleField.SetValue(controller, joystickHandle);
            if (fixedField != null) fixedField.SetValue(controller, fixedPosition);
            
            // UI 색상 설정
            if (joystickBackground.GetComponent<Image>() != null)
                joystickBackground.GetComponent<Image>().color = joystickBackgroundColor;
                
            if (joystickHandle.GetComponent<Image>() != null)
                joystickHandle.GetComponent<Image>().color = joystickHandleColor;
        }
        else
        {
            Debug.LogWarning("조이스틱 UI 요소가 할당되지 않았거나 JoystickController가 없습니다.");
        }
    }
}