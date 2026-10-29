using UnityEngine;

///=====================================================
/// 3Dアクション用カメラの追従・ロックオン管理クラス
///=====================================================
public class CameraManager : MonoBehaviour
{
    // 現在有効なCameraManagerのインスタンス
    public static CameraManager Instance { get; private set; }


    //--------------------------------------------------
    // カメラの追従対象
    //--------------------------------------------------
    [Header("Target Transforms")]

    [Tooltip("カメラが追従するプレイヤー")]
    public Transform playerTransform;

    [Tooltip("ロックオン対象となるボス")]
    public Transform bossTransform;


    //--------------------------------------------------
    // カメラ設定
    //-------------------------------------------------
    [Header("Camera Settings")]

    [Tooltip("プレイヤーを基準としたカメラの位置")]
    [SerializeField] private Vector3 followOffset = new Vector3(0, 2.5f, -4.5f);

    [Tooltip("カメラの追従・回転速度")]
    [Min(0f)]
    [SerializeField] private float smoothSpeed = 10f;

    [Tooltip("通常追従時に注視するプレイヤーの位置補正")]
    [SerializeField]
    private Vector3 playerLookAtOffset = new Vector3(0f, 1.5f, 0f);

    [Tooltip("ロックオン時に注視するボスの位置補正")]
    [SerializeField]
    private Vector3 bossLookAtOffset = new Vector3(0f, 1.2f, 0f);


    //--------------------------------------------------
    // カメラの状態
    //--------------------------------------------------
    [Header("Status")]

    [Tooltip("ボスにロックオンしているかどうか")]
    public bool isLockOn = false;

    //---------------------------------------------------------------------------------------------------------
    // CameraManagerの初期化処理。(同じシーンなどで重複して生成された場合は、後から生成された方を破棄する。)
    //--------------------------------------------------------------------------------------------------------
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //----------------------------------------------------------
    // プレイヤーやボスの移動が反映された後にカメラを更新する。
    //----------------------------------------------------------
    private void LateUpdate()
    {
        // プレイヤーが設定されていない場合はカメラを更新しない。
        if (playerTransform == null)
        {
            return;
        }


        // ロックオン対象が存在しなくなった場合はロックオンを解除する。
        if (bossTransform == null)
        {
            isLockOn = false;
        }

        // ロックオン入力があった場合、ロックオン状態を切り替える。
        if (InputManager.Instance != null && InputManager.Instance.LockOnPressed)
        {
            if (bossTransform != null)
            {
                isLockOn = !isLockOn;
            }
        }

        Vector3 targetPosition;
        Vector3 lookAtPosition;

        // プレイヤーの水平方向の向きを取得する。
        // キャラクターが傾いていても、カメラの追従位置が上下に傾かないようにする。
        Quaternion playerYawRotation = Quaternion.Euler(0f, playerTransform.eulerAngles.y, 0f);

        if (isLockOn && bossTransform != null)
        {
            // --------------------------------------------------
            // ロックオン時
            // --------------------------------------------------

            // プレイヤーとボスの中間地点を求める。
            Vector3 centerPoint = (playerTransform.position + bossTransform.position) * 0.5f;

            // 中間地点を基準にカメラを配置する。
            targetPosition = centerPoint + playerYawRotation * followOffset;

            // ボスの上半身付近を注視する。
            lookAtPosition = bossTransform.position + bossLookAtOffset;
        }
        else
        {
            // --------------------------------------------------
            // 通常追従時
            // --------------------------------------------------

            // プレイヤーの向きに合わせて、後方にカメラを配置する。
            targetPosition = playerTransform.position + playerYawRotation * followOffset;

            // プレイヤーの頭付近を注視する。
            lookAtPosition = playerTransform.position + playerLookAtOffset;
        }

        // --------------------------------------------------
        // カメラ位置の更新
        // --------------------------------------------------

        // フレームレートに応じた補間係数を使用して、位置を滑らかに移動させる。
        float interpolation = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, targetPosition, interpolation);

        // --------------------------------------------------
        // カメラ回転の更新
        // --------------------------------------------------

        // 注視点までの方向から、目標となるカメラの回転を求める。
        Vector3 lookDirection = lookAtPosition - transform.position;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

            // 回転も滑らかに補間する。
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, interpolation);
        }
    }

    //--------------------------------------------------------------------------------
    // ロックオン状態を切り替える。(外部のロックオン処理から呼び出すこともできる。)
    //----------------------------------------------------------------------------------
    public void ToggleLockOn()
    {
        if (bossTransform == null)
        {
            isLockOn = false;
            return;
        }

        isLockOn = !isLockOn;
    }

    //----------------------------------------------------------------------
    // オブジェクト破棄時、自分が現在のインスタンスなら参照を解除する。
    //---------------------------------------------------------------------
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}