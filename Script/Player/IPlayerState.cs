
///=====================================================================
/// プレイヤーの各ステート（状態）が実装すべきインターフェース 
/// 待機・移動・攻撃・ガード・回避などの状態を統一した方法で管理する
///=====================================================================
public interface IPlayerState
{

    void Enter(PlayerController player);       // ステートに入った瞬間
    void UpdateState(PlayerController player); // ステート中の毎フレーム処理
    void Exit(PlayerController player);        // ステートを出る瞬間
}