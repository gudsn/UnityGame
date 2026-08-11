using UnityEngine;
using UnityEngine.UIElements;

// 우측 상단 라운드 UI 표시 제어 클래스
public class RoundUIController : MonoBehaviour {
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset roundUITemplate;

    private VisualElement roundRoot;
    private Label roundNumberLabel;

    // UIDocument 바인딩 확인
    private void Awake() {
        if (uiDocument == null) {
            uiDocument = GetComponent<UIDocument>();
        }
    }

    // 초기 UI 구조 바인딩 및 라운드 수치 갱신
    private void Start() {
        InitUI();
    }

    // UI 엘리먼트 쿼리 및 기본값 세팅
    private void InitUI() {
        if (uiDocument == null) return;
        VisualElement canvas = uiDocument.rootVisualElement;

        if (canvas == null) return;

        if (roundUITemplate != null) {
            roundRoot = roundUITemplate.Instantiate();
            canvas.Add(roundRoot);
        }
        else {
            roundRoot = canvas.Q<VisualElement>("round-container");
        }

        if (roundRoot != null) {
            roundNumberLabel = roundRoot.Q<Label>("round-number-label");
        }

        if (FSMManager.Instance != null) {
            SetRoundText(FSMManager.Instance.CurrentRound);
        }
    }

    // 라운드 변경 이벤트 구독
    private void OnEnable() {
        EventBus<UpdateRoundEvent>.Subscribe(OnUpdateRound);
    }

    // 라운드 변경 이벤트 해제
    private void OnDisable() {
        EventBus<UpdateRoundEvent>.Unsubscribe(OnUpdateRound);
    }

    // 라운드 이벤트 수신 시 UI 텍스트 업데이트
    private void OnUpdateRound(UpdateRoundEvent evt) {
        SetRoundText(evt.currentRound);
    }

    // 라운드 텍스트 갱신 로직
    private void SetRoundText(int round) {
        if (roundNumberLabel != null) {
            roundNumberLabel.text = round > 0 ? round.ToString() : "1";
        }
    }
}