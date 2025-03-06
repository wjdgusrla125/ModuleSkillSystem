using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CookieController : MonoBehaviour
{
    private Entity entity;
    
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    
    public float MoveSpeed 
    { 
        get { return moveSpeed; }
        set { moveSpeed = Mathf.Max(0, value); }
    }
    
    // 이동 방식 선택을 위한 열거형
    public enum MovementType
    {
        NavMeshAgent,
        DirectMovement
    }
    
    [SerializeField] private MovementType currentMovementType = MovementType.NavMeshAgent;
    
    // 움직임 관련 변수
    private Vector3 moveDirection;
    private bool isMoving = false;

    private void Start()
    {
        entity = GetComponent<Entity>();
        entity.SkillSystem.onSkillTargetSelectionCompleted += ReserveSkill;

        // 탭 이벤트에 SelectTarget 메서드 연결
        JoyStickController.Instance.onTapAction += SelectTarget;
        
        // 조이스틱 이벤트에 HandleJoystickMove 메서드 연결
        JoyStickController.Instance.onJoystickMove += HandleJoystickMove;
    }

    private void OnEnable()
    {
        if (JoyStickController.Instance != null)
        {
            JoyStickController.Instance.onJoystickMove += HandleJoystickMove;
        }
    }

    private void OnDisable()
    {
        if (JoyStickController.Instance != null)
        {
            JoyStickController.Instance.onJoystickMove -= HandleJoystickMove;
        }
    }

    private void OnDestroy()
    {
        if (JoyStickController.Instance != null)
        {
            JoyStickController.Instance.onTapAction -= SelectTarget;
            JoyStickController.Instance.onJoystickMove -= HandleJoystickMove;
        }
    }

    private void Update()
    {
        // 스킬트리 토글 기능 유지
        if (Input.GetKeyDown(KeyCode.S))
        {
            var skillTreeView = SkillTreeView.Instance;
            if (!skillTreeView.gameObject.activeSelf)
                skillTreeView.Show(entity, entity.SkillSystem.DefaultSkillTree);
            else
                skillTreeView.Hide();
        }
        
        // 조이스틱 기반 이동 처리
        if (isMoving && moveDirection.magnitude > 0)
        {
            // 이동 방향에 따라 캐릭터 회전
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            // 이동 방식에 따른 처리
            switch (currentMovementType)
            {
                case MovementType.NavMeshAgent:
                    HandleNavMeshAgentMovement();
                    break;
                
                case MovementType.DirectMovement:
                    HandleDirectMovement();
                    break;
            }
            
            // 스킬 취소
            entity.SkillSystem.CancelReservedSkill();
        }
    }

    // NavMeshAgent를 사용한 이동 처리
    private void HandleNavMeshAgentMovement()
    {
        if (entity.Movement != null)
        {
            // NavMeshAgent를 사용하여 목적지 설정
            entity.Movement.Destination = transform.position + moveDirection * 1.5f;
        }
    }

    // 직접 이동 처리 (NavMeshAgent 미사용)
    private void HandleDirectMovement()
    {
        // 카메라 기준 이동 및 충돌 검사
        Vector3 movementVector = moveDirection * moveSpeed * Time.deltaTime;
        
        // 기본 이동 충돌 검사
        RaycastHit hit;
        if (Physics.Raycast(transform.position, movementVector.normalized, out hit, movementVector.magnitude))
        {
            // 충돌 시 이동 제한
            if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Player"))
            {
                movementVector = Vector3.zero;
            }
        }
        
        // 실제 이동 적용
        transform.position += movementVector;
    }

    // 조이스틱 이동 처리
    private void HandleJoystickMove(Vector2 direction, float magnitude)
    {
        if (magnitude > 0.1f) // 최소 임계값 설정
        {
            isMoving = true;
            
            // 2D 방향을 3D 공간으로 변환 (카메라 방향 고려)
            Vector3 cameraForward = Camera.main.transform.forward;
            Vector3 cameraRight = Camera.main.transform.right;
            
            // Y 성분(높이)을 0으로 설정하여 수평 평면에서만 이동하도록 함
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();
            
            // 조이스틱 입력을 카메라 기준으로 변환
            moveDirection = (cameraForward * direction.y + cameraRight * direction.x).normalized;
            
            // 이동 속도 조절 (magnitude에 따라)
            moveDirection *= magnitude;
        }
        else
        {
            isMoving = false;
            moveDirection = Vector3.zero;
        }
    }

    // 화면 탭으로 대상 선택
    private void SelectTarget(Vector2 screenPosition)
    {
        var ray = Camera.main.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out var hitInfo, Mathf.Infinity))
        {
            var entity = hitInfo.transform.GetComponent<Entity>();
            if (entity)
                EntityHUD.Instance.Show(entity);
        }
    }

    // 스킬 예약 기능 유지
    private void ReserveSkill(SkillSystem skillSystem, Skill skill, TargetSearcher targetSearcher, TargetSelectionResult result)
    {
        if (result.resultMessage != SearchResultMessage.OutOfRange ||
            !skill.IsInState<SearchingTargetState>())
            return;
        
        entity.SkillSystem.ReserveSkill(skill);

        var selectionResult = skill.TargetSelectionResult;
        if (selectionResult.selectedTarget)
            entity.Movement.TraceTarget = selectionResult.selectedTarget.transform;
        else
            entity.Movement.Destination = selectionResult.selectedPosition;
    }

    // 이동 방식 변경 메서드 (외부에서 호출 가능)
    public void SetMovementType(MovementType type)
    {
        currentMovementType = type;
    }
}