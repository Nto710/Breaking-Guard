using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

///=================================================
/// シーン遷移を一括管理するマネージャークラス
///================================================
public class SceneTransitionManager : MonoBehaviour
{
    // シングルトンインスタンス。他クラスから SceneTransitionManager.Instance 経由でアクセス。
    public static SceneTransitionManager Instance { get; private set; }

    //-----------------------
    // シーン名の定数定義
    //-----------------------
    [Header("Scene Name Constants")]
    public const string TITLE_SCENE        = "TitleScene";
    public const string STAGE_SELECT_SCENE = "StageSelectScene";
    public const string CHAR_SELECT_SCENE  = "CharSelectScene";
    public const string LOADING_SCENE      = "LoadingScene";
    public const string GAME_MAIN_SCENE    = "GameMainScene";
    public const string RESULT_SCENE       = "ResultScene";


    //------------------------
    // シーン遷移管理
    //------------------------
    [Tooltip("ローディング画面を挟む際に、次に遷移する目的地のシーン名一時保持用")]
    private string nextSceneName;


    // ローディングを挟むシーン遷移が進行中かどうか。
    // 多重にコルーチンが起動することを防ぐ。
    private bool isTransitioning = false;


    //-----------------------------------------------------------------------------------------------------------------
    // シングルトンの初期化処理。(最初のインスタンスをシーン間で維持し、重複して生成されたインスタンスは破棄する。)
    //-----------------------------------------------------------------------------------------------------------------
    private void Awake()
    {
        // シングルトンの初期化と重複インスタンスの破棄
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    //--------------------------------------------------------------------------------
    // 指定したシーンへ直接遷移(ローディング画面を挟まない短い遷移などに使用する。)
    //--------------------------------------------------------------------------------
    public void LoadScene(string sceneName)
    {
        // ローディングを挟む遷移中は、別の遷移要求を受け付けない。
        if (isTransitioning)
        {
            Debug.LogWarning("シーン遷移中のため、新しい遷移要求を無視しました。");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("遷移先のシーン名が指定されていません。");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    ///-------------------------------------------------------------------
    /// LoadingSceneを挟んで目的のシーンへ非同期ロード遷移するコルーチン
    ///-------------------------------------------------------------------
    public void LoadSceneWithLoading(string targetSceneName)
    {
        // 遷移中の多重呼び出しを防ぐ。
        if (isTransitioning)
        {
            Debug.LogWarning("シーン遷移中のため、新しい遷移要求を無視しました。");
            return;
        }

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("遷移先のシーン名が指定されていません。");
            return;
        }

        // コルーチンで使用する遷移先を保存する。
        nextSceneName = targetSceneName;
        isTransitioning = true;

        StartCoroutine(LoadWithLoadingRoutine());
    }

    ///----------------------------------------------------------------------------
    // ローディング画面の表示、ウェイト、非同期ロードの進行を管理するコルーチン
    ///-----------------------------------------------------------------------------
    private IEnumerator LoadWithLoadingRoutine()
    {
        // 1. ローディング画面を読み込む。
        AsyncOperation loadingOperation = SceneManager.LoadSceneAsync(LOADING_SCENE);

        if (loadingOperation == null)
        {
            Debug.LogError($"ローディングシーンを読み込めませんでした: {LOADING_SCENE}");
            isTransitioning = false;
            yield break;
        }

        yield return loadingOperation;


        // 2. ローディング画面を一定時間表示する。（タイムスケールが停止していても待機時間が進むようにする。）
        yield return new WaitForSecondsRealtime(1.0f);


        // 3. 遷移先のシーンを非同期で読み込む。
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);

        if (asyncLoad == null)
        {
            Debug.LogError($"遷移先のシーンを読み込めませんでした: {nextSceneName}");
            isTransitioning = false;
            yield break;
        }

        // 読み込みが完了しても、すぐにはシーンを切り替えない。
        asyncLoad.allowSceneActivation = false;

        // 4. 読み込みが有効化可能な状態になるまで待機する。
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // 5. シーンを有効化し、読み込み完了まで待つ。
        asyncLoad.allowSceneActivation = true;

        yield return asyncLoad;

        // 次のシーン遷移を受け付けられる状態に戻す。
        isTransitioning = false;
    }
}