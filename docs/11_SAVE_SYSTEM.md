# セーブシステム統合管理（SaveManager）

## 前提

- 参照:
  - `docs/00_PROJECT_SPEC.md`
  - `docs/12_SCREEN_FLOW.md`

## 目的

CursedBlood の全体進行データを一元管理する。
本プロジェクトでは「同一主人公の継続挑戦」「借金返済」「潜行記録」「研究強化」が中核であるため、それらを安全に保存・復元できる設計にする。

## 保存対象

### 必須永続データ
- 主人公選択（男 / 女）
- 主人公名
- 総潜行回数
- 借金残高
- 総返済額
- 総利息支払額
- 総救助費
- 完済済みフラグ
- 所持金
- 恒久研究状況
- ソナー研究状況
- 装備インベントリ
- 装備中アイテム
- 潜行記録一覧
- 実績
- ランキング
- 設定値（バーチャルパッド透過率など）

### 任意保存
- チュートリアル既読
- 最後に選択した見た目
- 最後に表示した画面
- デバッグフラグ（必要なら別管理）

## 保存しないもの
- 進行中のチャンク状態
- 進行中の敵配置
- 進行中の潜行そのもの
- 一時的なソナー反応
- 一時的な回収ポイント位置

※ Phase 1〜5 では「潜行中断再開」は不要。潜行は1回単位で完結させる。

## SaveData構成

```text
SaveData
├─ Meta
│   ├─ Version
│   ├─ CreatedAt
│   ├─ UpdatedAt
├─ PlayerProfile
│   ├─ Gender
│   ├─ Name
│   ├─ TotalDiveCount
│   ├─ CurrentMoney
├─ Debt
│   ├─ CurrentDebt
│   ├─ TotalRepaid
│   ├─ TotalInterestPaid
│   ├─ TotalRescueCost
│   ├─ DebtCleared
├─ Research
│   ├─ SonarRangeLevel
│   ├─ SonarPrecisionLevel
│   ├─ SonarIdentifyLevel
│   ├─ FilterLevel
│   ├─ OxygenLevel
│   ├─ RescueCostReductionLevel
├─ Equipment
│   ├─ EquippedItems
│   ├─ Inventory
├─ Records
│   ├─ DiveRecords[]
├─ Achievement
│   ├─ UnlockedAchievements[]
│   ├─ Counters
├─ Ranking
│   ├─ BestDepth
│   ├─ BestSingleProfit
│   ├─ FastestDebtClear
│   ├─ TotalMoneyEarned
├─ Settings
│   ├─ VirtualPadOpacity
│   ├─ Audio
│   ├─ Accessibility
Copy
DiveRecord構造
各潜行記録に保存する内容:

潜行回数
日時
到達深度
回収成功 / 救助失敗
持ち帰り額
ロスト額
回収費
借金変動
使用装備
救助理由
スコア
SaveManager責務
役割
SaveDataのロード
SaveDataの保存
新規データ作成
バージョン移行
欠損データのデフォルト補完
必須メソッド
SaveData Load()
void Save(SaveData data)
SaveData CreateNew()
SaveData Migrate(SaveData oldData)
bool Exists()
void DeleteAll()
保存タイミング
タイトル画面から開始時
主人公選択確定時
潜行結果確定時
返済後
研究購入後
装備変更後
実績解除後
設定変更後
タイトルへ戻る時
アプリ終了時
保存形式
JSON想定
バージョン番号を含める
将来のフィールド追加に備えて後方互換性を意識する
SaveMigrator
旧バージョンからのマイグレーションを担当。

想定
呪い / 世代ベースの旧保存データが存在する場合、読み込み時に破棄または新構造へ変換
旧フィールド:
Generation
CurseResearch
FamilyTree
HumanAge などは新仕様に合わせて移行する
移行ポリシー
変換不能な古いデータは安全な初期値に落とす
クラッシュさせないことを最優先
注意事項
セーブ破損時にゲーム不能にならないようにする
必須項目欠落時は新規データ生成で復旧できること
進行中潜行は保存対象外とし、複雑化を避ける
借金や研究など、ゲームコアの数値は必ず保存整合性を確認する