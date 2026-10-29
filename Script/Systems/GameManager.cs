using UnityEngine;

///===============================================================
/// ゲーム全体のグローバルデータと状態を管理するクラス
/// シーンをまたいで永続化する（DontDestroyOnLoad）
///===============================================================
public class GameManager : MonoBehaviour
{
    // シングルトンインスタンス。他クラスから GameManager.Instance 経由でアクセスします。
    public static GameManager Instance { get; private set; }

     // --------------------------------------------------
    // ゲーム設定・選択データ
    // --------------------------------------------------
    [Header("Game Settings & Data")]
    [Tooltip("現在選択されているステージの識別ID")]
    public int selectedStageID = 1;

    [Tooltip("現在選択されているプレイヤーキャラクターの識別ID")]
    public int selectedCharacterID = 0;

    [Tooltip("現在装備しているスキルの識別ID配列（最大3つ）")]
    public int[] selectedSkillIDs = new int[3];

    // プレイ結果データ
    [Header("Play Result Data")]
    [Tooltip("現在のステージでのプレイ時間（秒）")]
    public float playTime = 0f;

    [Tooltip("今回のプレイにおける最大コンボ数")]
    public int maxComboCount = 0;

    [Tooltip("ステージクリア判定フラグ")]
    public bool isClear = false;

    [Tooltip("ゲームオーバー判定フラグ")]
    public bool isGameOver = false;

    private void Awake()
    {
        // シングルトンの初期化と重複インスタンスの破棄
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    ///--------------------------------
    /// リザルトデータの初期化
    ///--------------------------------
    public void ResetPlayData()
    {
        playTime = 0f;
        maxComboCount = 0;
        isClear = false;
        isGameOver = false;
    }
}


// ＜注意事項＞
//
//・プレイ時間の更新処理はまだありません。( playTime を増やす処理は、ゲームプレイ中の管理クラスなどに実装する必要があります)
//
//・最大コンボ数の更新処理もまだありません。( プレイヤーのコンボ数が更新された際に、maxComboCount と比較して記録する仕組みが必要です。)
//
//・クリア・ゲームオーバーの判定処理も別途必要です。( 現状はフラグを保持するだけで、リザルトシーンへの遷移などは行いません。)
//
//・Instance の破棄処理は現状では追加していません。 (通常のシーン遷移を想定した構成を維持し、必要性が出てから検討します。)
//