# Phase 1: 小ブロックグリッド＋8方向移動＋掘削幅＋タイマー＋リスタート

## 前提

- 依存Phase: なし（最初に実装）
- 参照: `docs/00_PROJECT_SPEC.md`

## このPhaseのゴール

16×16pxの小ブロックグリッド上をプレイヤーが8方向に移動し、掘削幅に応じたトンネルを掘りながら深度を進む。60秒の寿命タイマーで死亡し、リザルト画面からリスタートできる最小プレイアブル。「砂の壁を掘り進む」感覚を実現する。

## 完了条件チェックリスト

### グリッド
- [ ] 67列×87行の小ブロックグリッドが画面に表示される（y:200〜1600、セルサイズ16×16px）
- [ ] 土ブロック（茶色）、石ブロック（灰色）、硬岩ブロック（暗灰色）、破壊不能（黒）、空洞（暗色）が色で区別できる
- [ ] チャンク管理（16行/チャンク）でメモリ効率的に運用される
- [ ] セルデータがbyte配列で管理される（クラスインスタンスではない）
- [ ] 画面外チャンクが自動破棄される
- [ ] 深度に応じてブロック種別の出現率が変化する

### 移動
- [ ] プレイヤー（5×5セル=80×80pxの仮ビジュアル）がグリッド上に表示される
- [ ] 矢印キーで8方向に移動できる（同時押しで斜め）
- [ ] スワイプ（マウスドラッグ/タッチ）で8方向の方向指定ができる（自由角度→最近接8方向にスナップ）
- [ ] 一度方向を指定すると、次の入力まで自動で同方向に進み続ける
- [ ] 移動速度が0.02秒/セル（50セル/秒）で滑らかに移動する
- [ ] 先行入力バッファが機能する

### 掘削
- [ ] 進行方向に対して掘削幅分（Phase 1では固定幅5）のセルが同時に破壊される
- [ ] 土ブロックは即座に破壊（速度低下なし）
- [ ] 石ブロックは通過速度が1/2に低下
- [ ] 硬岩ブロックは通過速度が1/4に低下
- [ ] 破壊不能ブロックは通れない（方向が変わらず停止）
- [ ] 掘削時にブロックが消えるアニメーション（色が薄くなって消える簡易演出）
- [ ] プレイヤーの占有範囲（5×5セル）が壁にめり込まない

### ガード
- [ ] Space長押し/長押しでガード（移動停止）

### カメラ
- [ ] プレイヤー移動に合わせてカメラが追従する（プレイヤーが画面中央）
- [ ] カメラ追従が滑らか（Lerp補間）

### HUD
- [ ] HUDエリア（y:0〜200）に寿命バー、年齢テキスト、深度(m)が表示される
- [ ] 下部エリア（y:1600〜1920）にHPが表示される
- [ ] 60秒の寿命タイマーが動作する
- [ ] 年齢表示が「X歳/35歳」で動的に更新される
- [ ] 少年期/青年期/晩年期のフェーズ表示がある
- [ ] 少年期は移動速度が遅く（×0.6）、プレイヤーサイズが3×3セル
- [ ] 青年期で5×5セルに成長、速度ピーク
- [ ] 晩年期は速度低下（×0.7）

### 死亡・リスタート
- [ ] 寿命が尽きると死亡し、リザルト画面が表示される
- [ ] リザルト画面に世代、享年、最大深度(m)、スコアが表示される
- [ ] リザルト画面タップ/クリック/キー押下でリスタート
- [ ] リスタート時に世代番号がインクリメントされる
- [ ] グリッドが完全にリセットされて再開される

### パフォーマンス
- [ ] PC上で60fps維持
- [ ] 画面内セル総数（67×87≒5,829セル）の描画が滑らか
- [ ] メモリ使用量がチャンク管理で安定している

## 作成するファイル

### ディレクトリ構成

```
Scripts/CursedBlood/
  Core/
    GameManager.cs           ← ゲームループ管理
    ChunkManager.cs          ← チャンク生成・管理・破棄
    ChunkData.cs             ← チャンクデータ（byte配列）
    TerrainGenerator.cs      ← 地形生成ロジック
    CellType.cs              ← セル種別enum定義
    DigHelper.cs             ← 掘削幅・形状の計算
  Player/
    PlayerController.cs      ← 入力処理・8方向移動・掘削実行
    PlayerStats.cs           ← ステータス管理
  Camera/
    GameCamera.cs            ← カメラ追従（Lerp）
  UI/
    HUDManager.cs            ← HUD表示
    DeathScreen.cs           ← 死亡画面
Scenes/CursedBlood/
  CursedBloodMain.tscn       ← メインシーン
```

### CellType.cs

セル種別の定義。

```
namespace: CursedBlood.Core
```

**CellType byte enum**:
- `Empty = 0` — 空洞（掘削済み）
- `Dirt = 1` — 土ブロック（硬さ1.0、即破壊）
- `Stone = 2` — 石ブロック（硬さ2.0、速度1/2）
- `HardRock = 3` — 硬岩ブロック（硬さ4.0、速度1/4）
- `Ore = 4` — 鉱石（Phase 2以降。定義のみ）
- `Bedrock = 5` — 破壊不能
- `Enemy = 6` — 敵占有セル（Phase 2以降。定義のみ）
- `Boss = 7` — ボス占有セル（Phase 6以降。定義のみ）

byte型にすることで1セル=1バイト。

**CellTypeUtil static class**:
- `float GetHardness(CellType type)` — 硬さ倍率を返す
- `bool IsDiggable(CellType type)` — Bedrock以外はtrue
- `Color GetColor(CellType type, int depthTier)` — 描画色を返す（深度帯0〜3で色変化）

### ChunkData.cs

チャンクのデータ管理。

```
namespace: CursedBlood.Core
class ChunkData
```

- **定数**: `Width = 67`, `Height = 16`（16行で1チャンク）
- `byte[] Cells` — Width × Height = 1,072 バイトの配列
- `int ChunkIndex` — チャンク番号（0起点。チャンク0=行0〜15、チャンク1=行16〜31...）
- `int StartRow` → ChunkIndex × Height
- `byte GetCell(int localCol, int localRow)` — セル値取得
- `void SetCell(int localCol, int localRow, byte value)` — セル値設定
- `int ToIndex(int col, int row)` → row × Width + col

### ChunkManager.cs

チャンクの生成・保持・破棄を管理。Node2Dを継承。

```
namespace: CursedBlood.Core
partial class ChunkManager : Node2D
```

**定数**:
- `CellSize = 16` (px)
- `ChunkHeight = 16` (行)
- `Columns = 67`
- `FieldOffsetX = 4f` — 左マージン（(1080 - 67×16) / 2 = 4）
- `FieldOffsetY = 200f` — フィールド上端
- `FieldWidth = 1072f` — 67 × 16
- `FieldHeight = 1400f` — 87 × 16 ≒ 1392。フィールド高さ

**フィールド**:
- `Dictionary<int, ChunkData> _chunks` — チャンク番号→データ
- `int _cameraTopRow` — カメラの最上行
- `TerrainGenerator _generator`

**メソッド**:

`void Initialize()`:
- 画面表示に必要なチャンク（87行÷16≒6チャンク）+ 上下バッファ各1チャンク = 8チャンクを生成
- 最初の2チャンク（行0〜31）はプレイヤー開始エリアとして全セルEmpty

`ChunkData GetChunk(int chunkIndex)`:
- チャンクが存在しなければTerrainGeneratorで生成して格納

`byte GetCell(int col, int absoluteRow)`:
- col, rowからチャンクとローカル座標を算出してセル値を返す
- `chunkIndex = absoluteRow / ChunkHeight`
- `localRow = absoluteRow % ChunkHeight`

`void SetCell(int col, int absoluteRow, byte value)`:
- セル値を書き換え（掘削時に使用）

`void UpdateCamera(int playerRow)`:
- プレイヤーの行位置からカメラの最上行を算出（プレイヤーが画面中央になるよう）
- `_cameraTopRow = playerRow - 43`（87行の半分）
- 必要なチャンクを先読み生成
- 画面外（上方向）のチャンクを破棄

`Vector2 GridToScreen(int col, int row)`:
- グリッド座標 → スクリーン座標変換
- `x = FieldOffsetX + col * CellSize`
- `y = FieldOffsetY + (row - _cameraTopRow) * CellSize`

`void Reset()`:
- 全チャンク破棄、初期化し直し

**描画（_Draw）**:

パフォーマンスのため、描画を最適化する。

1. 表示範囲のチャンクのみ走査
2. Emptyセルは描画スキップ（背景色で代替）
3. 同一CellType の水平連続セルを1つの `DrawRect` にまとめる（バッチ描画）

```csharp
// 描画の疑似コード
for each visible row:
    CellType currentType = Empty;
    int runStart = 0;
    for col = 0 to Columns:
        byte cell = GetCell(col, row);
        if (cell != currentType || col == Columns):
            if (currentType != Empty && runLength > 0):
                DrawRect(batchedRect, GetColor(currentType));
            currentType = cell;
            runStart = col;
```

**背景**:
- フィールド全体の背景色を暗色 `(0.08, 0.08, 0.1)` で塗る（_Draw先頭で1回のDrawRect）
- Emptyセルは背景色のまま（描画しないことで高速化）

### TerrainGenerator.cs

地形生成。

```
namespace: CursedBlood.Core
class TerrainGenerator
```

- `Random _rng`

`void FillChunk(ChunkData chunk)`:
- チャンク内の全セルを深度に応じた確率で埋める
- パーリンノイズ風の分布で自然な地形を生成（高速化のためSimplexではなく、行ごとのシード値を使った簡易ノイズ）

**深度別のブロック分布**:

| 深度(m) | Dirt | Stone | HardRock | Bedrock | Empty |
|---|---|---|---|---|---|
| 0〜200 | 65% | 15% | 2% | 3% | 15% |
| 200〜500 | 50% | 25% | 8% | 5% | 12% |
| 500〜1000 | 35% | 30% | 15% | 8% | 12% |
| 1000〜3000 | 20% | 35% | 25% | 10% | 10% |
| 3000〜 | 10% | 30% | 35% | 15% | 10% |

**地形パターン**:
- 左右端の列（col 0〜2, 64〜66）は50%の確率でBedrock（壁を形成）
- 10行に1回程度、横方向にBedrock帯（横断壁）を生成。ただし必ず幅10セル以上の開口部を設ける（デッドエンド防止）
- 空洞は上下方向に連続しやすい（洞窟風）。乱数のシードを前行から引き継ぎ、空洞だった位置の近くは空洞になりやすくする

**開口部保証**:
- 各行で「横幅10セル以上の連続した掘削可能エリア」が存在することを保証する
- 全列がBedrockの場合は中央付近20セルをDirtに置き換え

### DigHelper.cs

掘削範囲の計算ユーティリティ。

```
namespace: CursedBlood.Core
static class DigHelper
```

**掘削形状 enum**:
```csharp
public enum DigShape : byte { Square, Diamond, Fan }
```

`List<Vector2I> GetDigArea(Vector2I playerPos, Vector2I direction, int width, DigShape shape, int playerSize)`:
- プレイヤー位置・進行方向・掘削幅・形状から、掘削すべきセル座標のリストを返す
- Phase 1では `width=5, shape=Square, playerSize=5` 固定

**Square形状の計算**（進行方向が(dx,dy)の場合）:
```
進行方向→(1,0)の場合:
  プレイヤー中心(cx, cy)から、
  掘削対象 = {(cx + 1, cy + offset) | offset = -width/2 ... +width/2}
  ※プレイヤーの占有範囲(5×5)の前方1列 + 左右拡張分

斜め方向→(1,1)の場合:
  掘削対象 = {(cx + 1, cy + 1 + offset_x, offset_y) ...}
  斜め方向に対して垂直方向に展開
```

実装上は、進行方向ベクトルとその法線ベクトルを使って一般化する:
```csharp
Vector2I forward = direction;
Vector2I lateral; // 進行方向に垂直な方向
if (forward == (1,0) || forward == (-1,0)):
    lateral = (0, 1)
elif (forward == (0,1) || forward == (0,-1)):
    lateral = (1, 0)
elif (forward == (1,1) || forward == (-1,-1)):
    lateral = (1, -1)  // 正規化不要（グリッド上の隣接）
elif (forward == (1,-1) || forward == (-1,1)):
    lateral = (1, 1)
```

`void ExecuteDig(ChunkManager chunks, List<Vector2I> area)`:
- 指定セル座標をすべてEmptyに書き換え
- Bedrockは書き換えない

### PlayerStats.cs

プレイヤーのステータス。純粋なデータクラス。

```
namespace: CursedBlood.Player
class PlayerStats
```

**プロパティ**:
- `float MaxLifespan = 60f`
- `float CurrentAge`
- `int MaxHp = 100`, `int CurrentHp`
- `Vector2I GridPosition = (33, 8)` — 初期位置（中央列, 開始行付近）
- `int PlayerSize` — 占有セルサイズ（少年期3, 青年期以降5）
- `int MaxDepthPixels` — 到達最大Y座標（ピクセル）
- `int MaxDepthMeters` → MaxDepthPixels / 16
- `float BaseMoveInterval = 0.02f` — 秒/セル
- `float DigPower = 10f`
- `int DigWidth = 5` — 掘削幅（Phase 1は固定。Phase 3で装備依存に）
- `DigShape DigShape = DigShape.Square` — 掘削形状
- `int EnemiesKilled, MaxCombo, CurrentCombo, Gold`
- `int Generation = 1`

**算出プロパティ**:
- `float HumanAge` — CurrentAge × 35 / MaxLifespan
- `LifePhase Phase` — Youth(0〜20秒), Prime(21〜45秒), Twilight(46〜60秒)
- `float PhaseMultiplier` — Youth=0.6, Prime=1.0, Twilight=0.7
- `float EffectiveMoveInterval` — BaseMoveInterval / PhaseMultiplier
- `bool IsAlive` — HP>0 かつ CurrentAge<MaxLifespan

**フェーズ遷移時の処理**:
- Youth→Prime: PlayerSize を 3→5 に変更（「成長」演出。周囲のセルを自動掘削して体が収まるようにする）
- Prime→Twilight: PlayerSize は 5 のまま

**メソッド**:
- `void AdvanceTime(float delta)`
- `void TakeDamage(int damage)`
- `long CalculateScore()` — 最大深度(m) × Max(1,撃破数) × コンボ補正 × 世代ボーナス
- `void Reset()`

### PlayerController.cs

入力処理と移動。Node2Dを継承。

```
namespace: CursedBlood.Player
partial class PlayerController : Node2D
```

**外部参照**:
- `ChunkManager Chunks`
- `PlayerStats Stats`

**8方向入力**:

キーボード:
```csharp
Vector2I inputDir = Vector2I.Zero;
if (Input.IsKeyPressed(Key.Up))    inputDir.Y -= 1;
if (Input.IsKeyPressed(Key.Down))  inputDir.Y += 1;
if (Input.IsKeyPressed(Key.Left))  inputDir.X -= 1;
if (Input.IsKeyPressed(Key.Right)) inputDir.X += 1;
// inputDirが(0,0)以外なら方向更新
```

スワイプ（8方向スナップ）:
```csharp
float angle = swipeDelta.Angle(); // ラジアン
int octant = Mathf.RoundToInt(angle / (Mathf.Pi / 4)) % 8;
// octant → 8方向のVector2Iにマッピング
Vector2I[] directions = {
    (1, 0),   // 0: 右
    (1, 1),   // 1: 右下
    (0, 1),   // 2: 下
    (-1, 1),  // 3: 左下
    (-1, 0),  // 4: 左
    (-1, -1), // 5: 左上
    (0, -1),  // 6: 上
    (1, -1)   // 7: 右上
};
```

**移動ロジック**:
- 基本は旧仕様と同じ（タイマーベースのセル移動）だが、間隔が0.02秒と非常に短い
- 移動先の全占有セル（PlayerSize × PlayerSize）がBedrock以外であることを確認
- 移動と同時に掘削: `DigHelper.GetDigArea()` で掘削範囲を算出し、`DigHelper.ExecuteDig()` で実行
- 硬いブロックがある場合: 掘削範囲内の最も硬いブロックの硬度で移動速度が決まる

**壁衝突処理**:
- 進行方向にBedrockがある場合、その方向には進まない
- 自動移動は停止するが、方向は保持。プレイヤーが新しい方向を入力するまで待機

**描画**:
- プレイヤーを四角形で描画（PlayerSize × CellSize px）
- フェーズ別色: Youth=緑, Prime=青, Twilight=オレンジ
- 方向インジケーター（矢印線）
- ガード中のシールド表示

### GameCamera.cs

カメラ追従。Camera2Dを継承。

```
namespace: CursedBlood.Camera
partial class GameCamera : Camera2D
```

- Lerp追従: `Position = Position.Lerp(targetPos, delta * 10f)` で滑らかに追従
- targetPosはプレイヤーのワールド座標
- カメラの上端がy=200、下端がy=1600に収まるようにクランプ
- `void Shake(float, float)` — 将来の画面揺れ用スタブ

**重要**: 旧仕様ではChunkManagerの_topVisibleRowでスクロールを管理していたが、新仕様ではカメラ自体を動かす方式に変更。ChunkManagerのGridToScreenはカメラのPositionを基準に計算する。

### HUDManager.cs

旧仕様と同じだが、深度表示が「深度 Xm」に変更。

- 寿命バー、年齢テキスト、フェーズ表示、深度(m)、スコア、HP
- CanvasLayerなのでカメラに追従しない

### DeathScreen.cs

旧仕様と同じ。深度表示が(m)単位に変更。

### GameManager.cs

旧仕様と基本同じだが、GridManager→ChunkManagerに変更。

**_Ready()**:
1. PlayerStats生成
2. ChunkManager生成・AddChild
3. PlayerController生成・Chunks/Statsセット・AddChild
4. GameCamera生成・AddChild
5. HUDManager生成・Initialize・AddChild
6. DeathScreen生成・RestartRequested接続・AddChild

**_Process(delta)**:
- Playing中: AdvanceTime → フェーズ遷移チェック（Youth→Primeで成長処理）→ IsAlive判定
- ChunkManager.UpdateCamera(playerRow) を毎フレーム呼ぶ

### CursedBloodMain.tscn

```
[gd_scene format=3]
[ext_resource type="Script" path="res://Scripts/CursedBlood/Core/GameManager.cs" id="1"]
[node name="CursedBloodMain" type="Node2D"]
script = ExtResource("1")
```

### project.godot 変更

```ini
[application]
run/main_scene="res://Scenes/CursedBlood/CursedBloodMain.tscn"

[display]
window/size/viewport_width=1080
window/size/viewport_height=1920
window/stretch/mode="canvas_items"
window/stretch/aspect="keep"
window/handheld/orientation="portrait"
```

## 実装順序

1. CellType.cs（1バイトenum、依存なし）
2. ChunkData.cs（byte配列、依存なし）
3. TerrainGenerator.cs（ChunkData依存）
4. DigHelper.cs（CellType依存）
5. ChunkManager.cs（ChunkData + TerrainGenerator + 描画）
6. PlayerStats.cs（データクラス、依存なし）
7. PlayerController.cs（ChunkManager + PlayerStats + DigHelper依存）
8. GameCamera.cs（単独）
9. HUDManager.cs（PlayerStats依存）
10. DeathScreen.cs（シグナルのみ）
11. GameManager.cs（全体結合）
12. CursedBloodMain.tscn + project.godot
13. dotnet build → 動作確認 → APKビルド

## パフォーマンス確認ポイント

- [ ] 67×87セルの全画面描画で60fps（バッチ描画の効果確認）
- [ ] チャンク生成が1ms以内で完了する
- [ ] 高速移動時（0.005秒/セル=200セル/秒）でもフレーム落ちしない
- [ ] 深度10000m（62,500行）到達時にメモリ使用量が安定している（チャンク破棄が機能）
- [ ] GCによるスパイクが体感できない

## 注意事項

- 旧コード（Scripts/Sample/, Scripts/Core/）は一切変更しない
- 全クラスは `CursedBlood.*` namespace配下に作成
- Godot 4.6.1 (.NET/C#) の partial class パターンを使用
- _Draw() による仮ビジュアル描画。スプライトは後のPhaseで差し替え
- CellDataクラスは廃止。byte配列 + CellTypeUtil静的メソッドで管理
- テスト後、project.godot の main_scene は `res://Scenes/CursedBlood/CursedBloodMain.tscn` のままにする
