# `docs/05_PHASE5_DEBT.md`

```md
# Phase 5: 借金＋利息＋救助費＋返済UI＋取り立て圧力

## 前提

- 依存Phase:
  - Phase 4
- 参照:
  - `docs/00_PROJECT_SPEC.md`
  - `docs/11_SAVE_SYSTEM.md`
  - `docs/12_SCREEN_FLOW.md`

## このPhaseのゴール

借金をゲーム全体の中心システムとして完成させる。
プレイヤーは潜行で利益を持ち帰り、借金返済を進める。
一方で利息、救助費、維持費、取り立て圧力が継続的にプレイヤーを圧迫し、「常に少し苦しい」経済状態を維持する。

また、借金完済後に目標を失わないよう、完済を中間目標とし、その先に深層ライセンスや高難度契約など新しい経済圧力と目標を導入する。

## 完了条件チェックリスト

### 借金
- [ ] 初期借金額が設定されている
- [ ] 潜行結果に応じて借金返済ができる
- [ ] 未返済分に利息が発生する
- [ ] 救助時に回収費が請求される
- [ ] 支払えない回収費が借金へ加算される
- [ ] 借金残高がUIで常に見える

### 返済
- [ ] リザルト画面で返済額を選べる
- [ ] 全額返済 / 一部返済 / 最低返済 / 返済見送り ができる
- [ ] 返済後に残高が正しく更新される
- [ ] 所持金・未換金資源・借金の関係が破綻しない

### 利息
- [ ] 潜行ごとに利息が反映される
- [ ] 利息が重すぎず軽すぎない
- [ ] 返済を怠ると苦しくなる
- [ ] 返済を進めると少し楽になる

### 救助費
- [ ] 救助時に深度や状況に応じた回収費が計算される
- [ ] UIで救助費が明示される
- [ ] 借金加算が発生したとき分かりやすい

### 取り立て圧力
- [ ] 借金が大きい / 返済が滞ると不利要素が発生する
- [ ] 取り立て圧力が敵出現率や危険イベントに反映される
- [ ] ただの理不尽ではなく、経済的なプレッシャーとして機能する

### 完済後
- [ ] 借金完済後に新しい中長期目標が提示される
- [ ] 深層ライセンス・維持費・新契約などで経済的圧迫が完全には消えない
- [ ] 完済後もプレイ目的を失わない

## 作成するファイル

```text
Scripts/CursedBlood/
  Debt/
    DebtManager.cs
    DebtUI.cs
    DebtTerms.cs
    DebtResultCalculator.cs
必要に応じて以下も更新:

Scripts/CursedBlood/Core/GameManager.cs
Scripts/CursedBlood/UI/ResultScreen.cs
Scripts/CursedBlood/Save/SaveData.cs
Scripts/CursedBlood/Save/SaveManager.cs
DebtTerms.cs
借金条件・利率・基本料金定義。

例
InitialDebt = 100000
InterestRatePerDive = 0.10f
MinimumRepaymentRate
BaseRescueCost
DepthRescueCostMultiplier
MaintenanceCost
PostClearLicenseCost
DebtManager.cs
役割
借金残高管理
利息加算
返済処理
救助費処理
完済判定
完済後の経済圧力管理
取り立て圧力計算
必須プロパティ
long CurrentDebt
long TotalRepaid
long TotalInterestPaid
long TotalRescueCost
bool DebtCleared
int ConsecutiveUnderpaymentCount
float CollectorPressure
メソッド
void Initialize()
void ApplyInterest()
void Repay(long amount)
long CalculateInterest()
long CalculateRescueCost(int maxDepthMeters, bool timeoutRescue, bool defeatedRescue)
void AddDebt(long amount)
void ApplyPostClearCosts()
float GetCollectorPressure()
DebtResultCalculator.cs
役割
リザルト時の精算
回収成功 / 救助失敗による差分
ロスト計算
借金増減計算
入出力
入力:

潜行深度
持ち帰り額
ロスト額
救助費
現在借金
返済額選択
出力:

新しい借金
利息
残高
今回の純利益
実支払額
DebtUI.cs
表示内容
借金残高
利息予測
今回返済額
次回潜行に残る所持金
返済選択ボタン
完済後の新コスト説明
借金ルール
初期借金
両親が遺した初期借金を持つ
初期値は調整可能だが、序盤に強く圧迫を感じる量にする
利息
潜行ごとに利息を加算
返済不足が続くと利息圧力が体感できる
ただし絶望的になりすぎないよう、研究や収益増で巻き返し可能にする
救助費
行動不能や時間切れ時に請求
深いほど高い
足りない場合は借金加算
維持費
装備整備費
フィルター交換費
入坑関連費
完済後もカツカツ感を維持するために使用
返済選択
リザルト画面で選択:

全額返済
半額返済
最低返済
返済しない
それぞれに

借金変動
所持金残量
次回圧力 が反映される
取り立て圧力
借金が大きい / 返済停滞で以下を強化可能:

取り立て屋出現率上昇
不利イベント増加
深層契約条件悪化
物価上昇
回収費増加
完済後要素
借金完済はエンドではなく中間目標。

完済後に解放:

深層ライセンス
高難度契約
高額装備維持費
深層調査依頼
最深部到達報酬
完済後も経済的に余裕が出すぎないようにする。

実装順序
DebtTerms.cs
DebtManager.cs
DebtResultCalculator.cs
DebtUI.cs
ResultScreenとの統合
SaveData/SaveManagerとの統合
GameManager連携
バランス確認
注意事項
借金を単なる数字にせず、毎回の判断に影響するシステムにすること
「苦しいが進歩は感じる」バランスを目指すこと
完済後の虚無を避けること
