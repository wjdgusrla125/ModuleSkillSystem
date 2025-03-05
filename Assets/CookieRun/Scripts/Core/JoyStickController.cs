using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// MouseController를 JoystickController로 변경하면서 기본 구조는 유지
public class JoyStickController : MonoSingleton<JoyStickController>
{
    // 터치 이벤트 처리를 위한 대리자
    public delegate void TouchHandler(Vector2 touchPosition);
    public delegate void JoystickHandler(Vector2 direction, float magnitude);
    
    [Serializable]
    private struct CursorData
    {
        public CursorType type;
        public Texture2D texture;
    }

    [SerializeField] private CursorData[] cursorDatas;
    
    // 가상 조이스틱 관련 필드 추가
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;
    [SerializeField] private float joystickRadius = 50f;
    [SerializeField] private bool joystickFixed = false;
    
    // 터치 및 조이스틱 이벤트 정의
    public event TouchHandler onTapAction;
    public event JoystickHandler onJoystickMove;
    
    private Vector2 joystickOrigin;
    private Vector2 joystickDirection;
    private float joystickMagnitude;
    private bool isDragging = false;
    private int touchId = -1;

    private void Start()
    {
        // 조이스틱 초기위치 저장
        if (joystickBackground != null)
            joystickOrigin = joystickBackground.position;
            
        // 모바일이 아닌 경우 조이스틱 UI 숨김 처리 (선택적)
        if (!Application.isMobilePlatform && joystickBackground != null)
        {
            joystickBackground.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // 모바일 터치 입력 처리
        if (Application.isMobilePlatform || Input.touchSupported)
        {
            HandleTouchInput();
        }
        // PC에서 테스트를 위한 마우스 입력 처리
        else
        {
            HandleMouseInput();
        }
        
        // 조이스틱 입력이 있을 때 이벤트 발생
        if (isDragging && joystickMagnitude > 0)
        {
            onJoystickMove?.Invoke(joystickDirection, joystickMagnitude);
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            // 모든 터치 검사
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                
                // 새로운 터치 시작
                if (touch.phase == TouchPhase.Began)
                {
                    // 이미 조이스틱 터치가 진행 중이 아닐 때만 새 터치 처리
                    if (!isDragging || touchId == -1)
                    {
                        touchId = touch.fingerId;
                        StartJoystick(touch.position);
                    }
                    // 다른 손가락으로 탭 액션 (조이스틱 사용 중 다른 터치)
                    else if (touch.fingerId != touchId)
                    {
                        onTapAction?.Invoke(touch.position);
                    }
                }
                // 터치 이동 중
                else if (touch.phase == TouchPhase.Moved && touch.fingerId == touchId)
                {
                    UpdateJoystick(touch.position);
                }
                // 터치 종료
                else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && touch.fingerId == touchId)
                {
                    EndJoystick();
                }
            }
        }
    }

    // PC 환경에서 테스트를 위한 마우스 입력 처리
    private void HandleMouseInput()
    {
        // 좌클릭 시작 - 조이스틱 위치 설정
        if (Input.GetMouseButtonDown(0) && !isDragging)
        {
            StartJoystick(Input.mousePosition);
        }
        // 좌클릭 유지 중 - 조이스틱 업데이트
        else if (Input.GetMouseButton(0) && isDragging)
        {
            UpdateJoystick(Input.mousePosition);
        }
        // 좌클릭 종료 - 조이스틱 리셋
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndJoystick();
        }
        
        // 우클릭은 탭 액션으로 처리
        if (Input.GetMouseButtonDown(1))
        {
            onTapAction?.Invoke(Input.mousePosition);
        }
    }

    // 조이스틱 시작
    private void StartJoystick(Vector2 position)
    {
        isDragging = true;
        
        // 고정 조이스틱이 아닌 경우, 터치한 위치에 조이스틱 표시
        if (!joystickFixed && joystickBackground != null)
        {
            joystickBackground.position = position;
            joystickOrigin = position;
        }
        
        if (joystickHandle != null)
            joystickHandle.position = joystickOrigin;
            
        joystickDirection = Vector2.zero;
        joystickMagnitude = 0f;
    }
    
    // 조이스틱 위치 업데이트
    private void UpdateJoystick(Vector2 position)
    {
        if (!isDragging) return;
        
        // 조이스틱 중심으로부터의 벡터 계산
        Vector2 direction = position - joystickOrigin;
        
        // 조이스틱 최대 이동 범위 제한
        float magnitude = Mathf.Min(direction.magnitude, joystickRadius) / joystickRadius;
        
        // 정규화된 방향 벡터 저장
        joystickDirection = direction.normalized;
        joystickMagnitude = magnitude;
        
        // 조이스틱 핸들 위치 업데이트
        if (joystickHandle != null)
        {
            joystickHandle.position = joystickOrigin + joystickDirection * magnitude * joystickRadius;
        }
    }
    
    // 조이스틱 종료
    private void EndJoystick()
    {
        isDragging = false;
        touchId = -1;
        
        // 핸들을 중앙으로 복귀
        if (joystickHandle != null)
            joystickHandle.position = joystickOrigin;
        
        // 고정 조이스틱이 아닌 경우, 원래 위치로 복귀
        if (!joystickFixed && joystickBackground != null)
            joystickBackground.position = joystickOrigin;
            
        joystickDirection = Vector2.zero;
        joystickMagnitude = 0f;
        
        // 이동 중지 이벤트 발행
        onJoystickMove?.Invoke(Vector2.zero, 0f);
    }

    // 커서 변경 기능 유지 (UI 요소 등에 사용 가능)
    // public void ChangeCursor(CursorType newType)
    // {
    //     // 모바일에서는 커서가 필요 없으므로 무시하거나,
    //     // UI 요소로 시각적 피드백을 제공할 수 있음
    //     if (!Application.isMobilePlatform)
    //     {
    //         if (newType == CursorType.Default)
    //         {
    //             Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    //         }
    //         else
    //         {
    //             var cursorTexture = cursorDatas.First(x => x.type == newType).texture;
    //             Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
    //         }
    //     }
    // }
}