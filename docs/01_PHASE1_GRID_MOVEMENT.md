# `docs/01_PHASE1_GRID_MOVEMENT.md`

```md
# Phase 1: 小ブロックグリッド＋8方向移動＋掘削幅＋潜行タイマー＋帰還/救助

## 前提

- 依存Phase: なし
- 参照:
  - `docs/00_PROJECT_SPEC.md`

## このPhaseのゴール

16×16pxの小ブロックグリッド上をプレイヤーが8方向に移動し、掘削幅に応じたトンネルを掘りながら地下を進む。
60秒の潜行制限の中で資源を回収し、地上に自力帰還するか、ランダム発生する回収ポイントを利用して報酬を確保する。
時間切れや行動不能時は救助扱いとなり、報酬ロストと回収費が発生する。

また、探索補助として簡易ソナーを導入し、近くに「何か」があることを距離に応じて察知できるようにする。
「危険な坑道で短時間だけ稼ぎ、持ち帰る」感覚を実現する。

## 完了条件チェックリスト

### グリッド
- [ ] 67列×87行の小ブロックグリッドが画面に表示される（y:200〜1600、セルサイズ16×16px）
- [ ] 土、石、硬岩、破壊不能、空洞が色で区別できる
- [ ] チャンク管理（16行/チャンク）でメモリ効率的に運用される
- [ ] セルデータがbyte配列で管理される
- [ ] 画面外チャンクが自動破棄される
- [ ] 深度に応じてブロック出現率が変化する

### 移動
- [ ] プレイヤー（5×5セル=80×80px）がグリッド上に表示される
- [ ] 矢印キーで8方向移動できる（同時押しで斜め）
- [ ] スワイプ / バーチャルパッドで8方向方向指定できる
- [ ] 一度方向指定すると次の入力まで自動で進み続ける
- [ ] 移動速度が0.02秒/セル基準で滑らかに動作する
- [ ] 先行入力バッファがある
- [ ] Guardで移動停止できる

### バーチャルパッド
- [ ] タップ位置に動的表示される
- [ ] 指を離すと消える
- [ ] 8方向入力できる
- [ ] 透過率を調整可能
- [ ] 長時間操作しても入力起点がズレにくい

### 掘削
- [ ] 進行方向に対して幅5の面掘りができる
- [ ] 土ブロックは即時掘削
- [ ] 石ブロックでは移動/掘削が遅くなる
- [ ] 硬岩ブロックではさらに大きく遅くなる
- [ ] 破壊不能ブロックは通れない
- [ ] 掘削時に簡易アニメーションやエフェクトがある
- [ ] プレイヤー占有範囲（5×5）が壁にめり込まない
- [ ] 進めない場合、その理由が視覚的またはデバッグ的に分かる

### 潜行制限
- [ ] 60秒の潜行タイマーが動作する
- [ ] 状態が Stable / Worn / Critical に変化する
- [ ] 潜行開始直後が最も好調
- [ ] 後半ほど移動/掘削性能が低下する
- [ ] UIで酸素や危険状態が分かる

### 帰還 / 救助
- [ ] 地上に戻ると報酬100%確保
- [ ] 回収ポイントに到達すると帰還選択できる
- [ ] 回収ポイント帰還でも報酬100%確保
- [ ] 時間切れまたは行動不能で救助扱いになる
- [ ] 救助時に資源70%ロスト、アイテム70%ロスト、回収費請求が発生する
- [ ] 支払えない分が借金へ加算される
- [ ] 潜行結果画面に成功/救助/損失が表示される

### 回収ポイント
- [ ] 必ず到達可能な位置に発生する
- [ ] 深い階層ほど出現率が低い
- [ ] 序盤は30秒圏相当距離に最低1個保証される
- [ ] 見つけたら帰還できる
- [ ] 理不尽な位置に生成されない

### ソナー
- [ ] 近くに何かあると反応する
- [ ] 遠距離では「何かある」だけわかる
- [ ] 中距離では方向がなんとなくわかる
- [ ] 近距離では方向が明確にわかる
- [ ] 反応対象が敵・アイテム・回収ポイントなどで曖昧な状態を表現できる

### カメラ
- [ ] プレイヤー追従
- [ ] Lerp補間で滑らか
- [ ] 掘削フィールド中心に見える

### HUD
- [ ] HUDエリア（y:0〜200）に酸素バー、状態表示、深度、借金残高、未換金資源が表示される
- [ ] 下部UI（y:1600〜1920）にHP、ソナー、操作補助情報が表示される
- [ ] 60秒潜行タイマーが分かりやすい
- [ ] 状態遷移が分かりやすい

### 結果 / リスタート
- [ ] 潜行終了時に結果画面が表示される
- [ ] 到達深度、回収成功/救助、回収額、ロスト額、回収費、借金増減が表示される
- [ ] タップ / クリック / キー押下で次の潜行へ進める
- [ ] 潜行回数がインクリメントされる
- [ ] グリッドが完全リセットされる

### パフォーマンス
- [ ] PC上で60fps維持
- [ ] 67×87セル描画が滑らか
- [ ] チャンク管理でメモリが安定
- [ ] 高速移動でもフレーム落ちしにくい
- [ ] GCスパイクが体感できない

## 作成するファイル

### ディレクトリ構成

```text
Scripts/CursedBlood/
  Core/
    GameManager.cs
    ChunkManager.cs
    ChunkData.cs
    TerrainGenerator.cs
    CellType.cs
    DigHelper.cs
    RecoveryPointManager.cs
    SonarSystem.cs
  Player/
    PlayerController.cs
    PlayerStats.cs
  Camera/
    GameCamera.cs
  UI/
    HUDManager.cs
    ResultScreen.cs
    VirtualPad.cs
Scenes/CursedBlood/
  CursedBloodMain.tscn
CellType.cs
セル種別の定義。

Copynamespace: CursedBlood.Core
CellType byte enum:

Empty = 0
Dirt = 1
Stone = 2
HardRock = 3
Ore = 4
Bedrock = 5
Enemy = 6
Boss = 7
RecoveryPoint = 8
Item = 9
CellTypeUtil static class:

float GetHardness(CellType type)
bool IsDiggable(CellType type)
Color GetColor(CellType type, int depthTier)
ChunkData.cs
Copynamespace: CursedBlood.Core
class ChunkData
Width = 67
Height = 16
byte[] Cells
int ChunkIndex
int StartRow => ChunkIndex * Height
byte GetCell(int localCol, int localRow)
void SetCell(int localCol, int localRow, byte value)
int ToIndex(int col, int row)
TerrainGenerator.cs
Copynamespace: CursedBlood.Core
class TerrainGenerator
役割
深度に応じた地形生成
開口保証
到達可能性の担保
回収ポイントの配置余地を壊さない生成
深度別ブロック分布（目安）
深度(m)	Dirt	Stone	HardRock	Bedrock	Empty
0〜200	65%	15%	2%	3%	15%
200〜500	50%	25%	8%	5%	12%
500〜1000	35%	30%	15%	8%	12%
1000〜3000	20%	35%	25%	10%	10%
3000〜	10%	30%	35%	15%	10%
ルール
左右端は一部Bedrock
横断Bedrock帯は作るが、必ず開口部を残す
各行に最低限の掘削可能帯を保証
開始付近は広めで理不尽な進行不能を避ける
プレイヤー5×5が通れる余地を考慮
開始エリア近辺は余裕を持たせる
DigHelper.cs
Copynamespace: CursedBlood.Core
static class DigHelper
DigShape enum
Copypublic enum DigShape : byte
{
    Square,
    Diamond,
    Fan
}
メソッド
List<Vector2I> GetDigArea(Vector2I playerPos, Vector2I direction, int width, DigShape shape, int playerSize)
void ExecuteDig(ChunkManager chunks, List<Vector2I> area)
Phase 1
width = 5
shape = Square
playerSize = 5
RecoveryPointManager.cs
Copynamespace: CursedBlood.Core
partial class RecoveryPointManager : Node
役割
回収ポイント出現管理
深度依存出現率
序盤保証ポイント
到達可能位置チェック
既存ポイント管理
仕様
序盤は30秒圏相当距離に最低1個保証
深いほど出現率低下
完全運ゲー防止のため、長時間未出現なら補正可
必ず到達可能かつ5×5で接近可能な位置にのみ配置
回収ポイントは地形内で明確に見える必要がある
SonarSystem.cs
Copynamespace: CursedBlood.Core
class SonarSystem
役割
周囲オブジェクト感知
距離段階ごとの精度変化
将来研究での強化前提
最低限実装
圏外: 無反応
遠距離: 何かあり
中距離: 方向ぼんやり
近距離: 方向明確
探知対象
回収ポイント
アイテム
敵
PlayerStats.cs
Copynamespace: CursedBlood.Player
class PlayerStats
プロパティ
float MaxDiveTime = 60f
float CurrentDiveTime
float OxygenRatio
float FilterRatio
int MaxHp = 100
int CurrentHp
Vector2I GridPosition = (33, 8)
int PlayerSize = 5
int MaxDepthPixels
int DigWidth = 5
DigShape DigShape = DigShape.Square
int DiveCount = 1
long CurrentDebt
long SalvageValue
List<ItemData> CollectedItems
bool ReturnedSafely
bool Rescued
算出
DivePhase Phase = Stable / Worn / Critical
float PhaseMultiplier
float EffectiveMoveInterval
bool IsOperational
フェーズ
Stable（0〜20秒）
Worn（21〜45秒）
Critical（46〜60秒）
メソッド
void AdvanceTime(float delta)
void TakeDamage(int damage)
long CalculateScore()
void ResetForNewDive()
PlayerController.cs
Copynamespace: CursedBlood.Player
partial class PlayerController : Node2D
責務
8方向入力
バーチャルパッド入力
自動継続移動
Guard
掘削実行
プレイヤー5×5占有チェック
進行不能理由の検出
要件
キーボード同時押し斜め対応
スワイプ8方向スナップ
動的バーチャルパッド対応
入力しやすさ重視
前方掘削と移動可能判定が一致していること
VirtualPad.cs
Copynamespace: CursedBlood.UI
partial class VirtualPad : Control
役割
タップ位置に動的表示
8方向入力
透過率調整
調整項目
BaseOpacity
KnobOpacity
DeadZoneRadius
MaxRadius
ChunkManager.cs
Copynamespace: CursedBlood.Core
partial class ChunkManager : Node2D
定数
CellSize = 16
ChunkHeight = 16
Columns = 67
VisibleRows = 87
FieldOffsetX = 4f
FieldOffsetY = 200f
役割
チャンク生成 / 破棄
セル読み書き
描画
カメラ連動可視範囲管理
描画最適化
Empty非描画
水平runバッチ描画
背景は1回描画
GameCamera.cs
Copynamespace: CursedBlood.Camera
partial class GameCamera : Camera2D
プレイヤー追従
Lerp補間
HUDと下部UIを考慮
HUDManager.cs
Copynamespace: CursedBlood.UI
partial class HUDManager : CanvasLayer
表示内容
酸素バー
状態（Stable / Worn / Critical）
深度
借金残高
未換金資源
HP
ソナー反応
回収ポイント発見通知
ResultScreen.cs
Copynamespace: CursedBlood.UI
partial class ResultScreen : CanvasLayer
表示内容
潜行回数
到達深度
回収成功 / 救助
持ち帰り額
ロスト額
回収費
借金増減
スコア
再出発入力
GameManager.cs
Copynamespace: CursedBlood.Core
partial class GameManager : Node2D
_Ready()
PlayerStats生成
ChunkManager生成
RecoveryPointManager生成
SonarSystem生成
PlayerController生成
GameCamera生成
HUDManager生成
ResultScreen生成
_Process(delta)
潜行時間進行
状態フェーズ更新
チャンク更新
回収ポイント管理
ソナー更新
地上帰還判定
回収ポイント帰還判定
救助判定
HUD更新
救助処理
資源70%ロスト
アイテム70%ロスト
回収費計算
不足分借金加算
結果画面表示
帰還成功処理
持ち帰り額100%
借金返済反映
結果画面表示
CursedBloodMain.tscn
Copy[gd_scene format=3]
[ext_resource type="Script" path="res://Scripts/CursedBlood/Core/GameManager.cs" id="1"]

[node name="CursedBloodMain" type="Node2D"]
script = ExtResource("1")
project.godot 変更
Copy[application]
run/main_scene="res://Scenes/CursedBlood/CursedBloodMain.tscn"

[display]
window/size/viewport_width=1080
window/size/viewport_height=1920
window/stretch/mode="canvas_items"
window/stretch/aspect="keep"
window/handheld/orientation="portrait"
実装順序
CellType.cs
ChunkData.cs
TerrainGenerator.cs
DigHelper.cs
RecoveryPointManager.cs
SonarSystem.cs
ChunkManager.cs
PlayerStats.cs
VirtualPad.cs
PlayerController.cs
GameCamera.cs
HUDManager.cs
ResultScreen.cs
GameManager.cs
CursedBloodMain.tscn
project.godot
dotnet build
Godot起動確認
必要ならAPKビルド
パフォーマンス確認ポイント
 67×87描画で60fps
 チャンク生成が軽い
 高速移動でも破綻しない
 回収ポイント生成が重くない
 ソナー処理で重くならない
 GCスパイクが目立たない
注意事項
旧コード（Scripts/Sample/, Scripts/Core/）は変更しない
新コードは CursedBlood.* namespace 配下
Godot 4.6.1 C# partial class パターン
_Draw() による仮ビジュアル可
まず仕様準拠と動作安定を優先
旧仕様の年齢・世代・呪い要素は一切持ち込まない
