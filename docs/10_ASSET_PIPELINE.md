# アセット管理・パイプライン

## 概要

Phase 1〜9の実装中は全て仮ビジュアル（プリミティブ描画）で進める。本ドキュメントでは、仮ビジュアルから本番アセットへの差し替え手順、必要素材の完全一覧、ファイル命名規則、ディレクトリ構成を定義する。

## ディレクトリ構成

```
Assets/
├── Sprites/
│   ├── Player/
│   │   ├── male_youth.png          ← 男・少年期スプライトシート
│   │   ├── male_prime.png          ← 男・青年期スプライトシート
│   │   ├── female_youth.png        ← 女・少年期スプライトシート
│   │   └── female_prime.png        ← 女・青年期スプライトシート
│   ├── Enemy/
│   │   ├── slime.png               ← スライム型スプライトシート
│   │   ├── shooter.png             ← 射撃型スプライトシート
│   │   └── spreader.png            ← 拡散型スプライトシート
│   ├── Boss/
│   │   ├── boss_tier0.png          ← ボス（深度0〜100帯）
│   │   ├── boss_tier1.png          ← ボス（深度100〜300帯）
│   │   ├── boss_tier2.png          ← ボス（深度300〜600帯）
│   │   ├── boss_tier3.png          ← ボス（深度600〜帯）
│   │   └── demon_lord.png          ← 魔王
│   ├── Blocks/
│   │   ├── normal_tier0.png        ← 通常ブロック（土色帯）
│   │   ├── normal_tier1.png        ← 通常ブロック（岩色帯）
│   │   ├── normal_tier2.png        ← 通常ブロック（溶岩色帯）
│   │   ├── normal_tier3.png        ← 通常ブロック（闇色帯）
│   │   ├── hard_tier0.png
│   │   ├── hard_tier1.png
│   │   ├── hard_tier2.png
│   │   ├── hard_tier3.png
│   │   ├── indestructible.png
│   │   └── ore.png                 ← 鉱石ブロック
│   ├── Items/
│   │   ├── Pickaxe/
│   │   │   ├── pickaxe_common_01.png  〜 pickaxe_common_05.png
│   │   │   ├── pickaxe_uncommon_01.png 〜 pickaxe_uncommon_04.png
│   │   │   ├── pickaxe_rare_01.png    〜 pickaxe_rare_04.png
│   │   │   ├── pickaxe_epic_01.png    〜 pickaxe_epic_03.png
│   │   │   ├── pickaxe_legendary_01.png 〜 pickaxe_legendary_02.png
│   │   │   └── pickaxe_cursed_01.png  〜 pickaxe_cursed_02.png
│   │   ├── Armor/
│   │   │   └── armor_01.png 〜 armor_12.png
│   │   ├── Accessory/
│   │   │   └── accessory_01.png 〜 accessory_12.png
│   │   └── Boots/
│   │       └── boots_01.png 〜 boots_09.png
│   ├── Effects/
│   │   ├── dig_particle.png        ← 掘削パーティクル素材
│   │   ├── hit_particle.png        ← 被弾パーティクル素材
│   │   ├── enemy_death.png         ← 敵撃破エフェクト
│   │   ├── levelup_flash.png       ← フェーズ遷移エフェクト
│   │   ├── skill_flash.png         ← スキル発動エフェクト
│   │   └── boss_explosion.png      ← ボス撃破爆発
│   └── UI/
│       ├── hud_frame.png           ← HUD枠
│       ├── lifespan_bar.png        ← 寿命バー素材
│       ├── hp_bar.png              ← HPバー素材
│       ├── skill_gauge.png         ← スキルゲージ（円形）
│       ├── rarity_frame_common.png 〜 rarity_frame_cursed.png  ← レアリティ枠（6枚）
│       ├── button_normal.png       ← ボタン通常状態
│       ├── button_pressed.png      ← ボタン押下状態
│       ├── family_tree_icon.png    ← 家系図アイコン
│       ├── achievement_icon.png    ← 実績アイコン
│       └── title_logo.png          ← タイトルロゴ
├── Audio/
│   ├── BGM/
│   │   ├── bgm_youth.ogg           ← 少年期BGM
│   │   ├── bgm_prime.ogg           ← 青年期BGM
│   │   ├── bgm_twilight.ogg        ← 晩年期BGM
│   │   ├── bgm_boss.ogg            ← ボス戦BGM
│   │   ├── bgm_demon_lord.ogg      ← 魔王戦BGM
│   │   ├── bgm_title.ogg           ← タイトルBGM
│   │   ├── bgm_result.ogg          ← リザルトBGM
│   │   └── bgm_ending.ogg          ← エンディングBGM
│   └── SE/
│       ├── se_dig.ogg              ← 掘削音
│       ├── se_dig_hard.ogg         ← 硬ブロック掘削音
│       ├── se_enemy_hit.ogg        ← 敵撃破音
│       ├── se_player_hit.ogg       ← 被弾音
│       ├── se_item_drop.ogg        ← アイテムドロップ音
│       ├── se_item_pickup.ogg      ← アイテム拾得音
│       ├── se_item_rare.ogg        ← Rare以上ドロップ音
│       ├── se_equip.ogg            ← 装備変更音
│       ├── se_skill.ogg            ← スキル発動音
│       ├── se_guard.ogg            ← ガード音
│       ├── se_bullet.ogg           ← 弾発射音
│       ├── se_bomb.ogg             ← 自爆型爆発音
│       ├── se_boss_appear.ogg      ← ボス出現音
│       ├── se_boss_death.ogg       ← ボス撃破音
│       ├── se_combo.ogg            ← コンボ加算音
│       ├── se_death.ogg            ← 死亡音
│       ├── se_generation.ogg       ← 世代交代音
│       ├── se_achievement.ogg      ← 実績解除音
│       ├── se_debt_pay.ogg         ← 借金返済音
│       └── se_menu.ogg             ← メニュー操作音
└── Fonts/
    ├── main_font.tres              ← メインフォント（Noto Sans JP推奨）
    └── number_font.tres            ← 数字用フォント（等幅）
```

## 必要素材一覧

### スプライトシート仕様

全スプライトシートは以下の共通仕様に従う。

| 項目 | 値 |
|---|---|
| シートサイズ | 1024×1024 px |
| グリッド | 3列×3行 |
| 1フレームサイズ | 341×341 px |
| フレーム数 | 最大9フレーム（使用しない枠は透明） |
| 形式 | PNG（透過あり） |
| カラーモード | RGBA 32bit |

### キャラクタースプライト（16枚）

| ファイル名 | 内容 | アニメーション | フレーム数 |
|---|---|---|---|
| male_youth.png | 男・少年期 | 待機(3), 掘削(3), 被弾(2), 死亡(1) | 9 |
| male_prime.png | 男・青年期 | 待機(3), 掘削(3), 被弾(2), 死亡(1) | 9 |
| female_youth.png | 女・少年期 | 待機(3), 掘削(3), 被弾(2), 死亡(1) | 9 |
| female_prime.png | 女・青年期 | 待機(3), 掘削(3), 被弾(2), 死亡(1) | 9 |

晩年期は青年期スプライトにシェーダーで彩度低下＋暗くするエフェクトを適用する。専用スプライトは作らない。

### 敵スプライト（3枚 × 4色 = 実質3枚）

| ファイル名 | 内容 | フレーム数 |
|---|---|---|
| slime.png | スライム型 | 待機(3), 被弾(2) = 5 |
| shooter.png | 射撃型 | 待機(3), 被弾(2) = 5 |
| spreader.png | 拡散型 | 待機(3), 被弾(2) = 5 |

自爆型はスライム型のスプライトにカウントダウン数字オーバーレイで表現する。深度帯カラーバリエーションはシェーダーのパレットスワップで対応する。スプライトの追加作成は不要。

### ボススプライト（5枚）

| ファイル名 | 内容 | フレーム数 |
|---|---|---|
| boss_tier0〜3.png | 通常ボス（4深度帯） | 待機(3), 攻撃(3), 被弾(2) = 8 |
| demon_lord.png | 魔王 | 待機(3), 攻撃(3), 被弾(2), 特殊(1) = 9 |

ボスは3×3セルサイズ（420×420px表示）。魔王は5×5セルサイズ（700×700px表示）。シートの1フレーム341×341pxにデザインし、表示時にスケーリングする。

### アイテムアイコン（53枚）

| カテゴリ | 枚数 | サイズ | 命名規則 |
|---|---|---|---|
| 掘削具 | 20枚 | 128×128px | pickaxe_[rarity]_[nn].png |
| 防具 | 12枚 | 128×128px | armor_[nn].png |
| アクセサリ | 12枚 | 128×128px | accessory_[nn].png |
| 靴 | 9枚 | 128×128px | boots_[nn].png |

アイコンは個別PNGファイル。レアリティ枠はUI側で重ねて表示するため、アイコン自体にはレアリティ装飾を含まない。

### ブロックテクスチャ（10枚）

| ファイル名 | サイズ | 用途 |
|---|---|---|
| normal_tier0〜3.png | 140×140px | 通常ブロック（4深度帯） |
| hard_tier0〜3.png | 140×140px | 硬いブロック（4深度帯） |
| indestructible.png | 140×140px | 破壊不能ブロック |
| ore.png | 140×140px | 鉱石ブロック |

タイリング可能なテクスチャとして作成する。

### エフェクト素材（6枚）

各エフェクトはGPUParticles2Dのテクスチャとして使用する。

| ファイル名 | サイズ | 用途 |
|---|---|---|
| dig_particle.png | 32×32px | 掘削パーティクル（小さな破片） |
| hit_particle.png | 32×32px | 被弾パーティクル |
| enemy_death.png | 64×64px | 敵撃破エフェクト |
| levelup_flash.png | 1080×1920px | フェーズ遷移フラッシュ（半透明グラデ） |
| skill_flash.png | 1080×1920px | スキル発動フラッシュ |
| boss_explosion.png | 512×512px | ボス撃破爆発 |

### 音声ファイル

全音声ファイルはOGG Vorbis形式。

| カテゴリ | 本数 | 目安サイズ |
|---|---|---|
| BGM | 8曲 | 各1〜3MB（ループ対応） |
| SE | 20種 | 各10〜100KB |

BGMはGodotのAudioStreamPlayer、SEはAudioStreamPlayer2D（位置なし）で再生する。ループポイントはGodotのインポート設定で指定する。

### フォント

| ファイル名 | 用途 | 推奨フォント |
|---|---|---|
| main_font.tres | 全テキスト | Noto Sans JP Regular/Bold |
| number_font.tres | 数値表示 | Noto Sans Mono または専用ピクセルフォント |

Godot 4のFontFile (.ttf/.otf) をインポートし、FontVariationリソースでサイズ・太さを管理する。

## 仮ビジュアルから本番アセットへの差し替え手順

### Phase 1〜9（仮ビジュアル期間）の方針

全ての描画は `_Draw()` メソッドによるプリミティブ描画（DrawRect, DrawCircle, DrawLine等）で実装する。色定数はクラスの static readonly フィールドとして定義し、後から一括変更可能にしておく。

### 差し替え手順

差し替えは以下の順序で行う。Phase 9完了後に一括で実施することを推奨する。

**ステップ1: アセット配置**

Assets/ ディレクトリに全素材ファイルを配置する。Godotエディタでインポート設定を行う（テクスチャフィルタ: Nearest、圧縮: Lossless等）。

**ステップ2: ブロック差し替え**

GridManager.cs の `_Draw()` 内の `DrawRect()` を `DrawTexture()` に変更する。深度帯（depthTier = depth / 100 で0〜3にクランプ）に応じたテクスチャを選択する。

変更前:
```csharp
DrawRect(rect, ColorNormal);
```

変更後:
```csharp
var texture = _blockTextures[depthTier, (int)cell.Type];
DrawTextureRect(texture, rect, false);
```

**ステップ3: キャラクター差し替え**

PlayerController.cs の `_Draw()` 内の `DrawCircle()` をAnimatedSprite2D子ノードに置き換える。PlayerControllerのReadyでAnimatedSprite2Dを動的生成し、性別とフェーズに応じたスプライトシートを設定する。

**ステップ4: 敵差し替え**

EnemyManager.cs またはGridManager.cs の敵描画部分をSprite2Dに置き換える。パレットスワップはShaderMaterialで実装する。

パレットスワップシェーダー:
```gdshader
shader_type canvas_item;
uniform vec4 swap_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    // 赤チャンネルをキーとして色を差し替え
    if (tex.r > 0.8 && tex.g < 0.2 && tex.b < 0.2) {
        COLOR = vec4(swap_color.rgb, tex.a);
    } else {
        COLOR = tex;
    }
}
```

**ステップ5: UI差し替え**

HUDManager.cs, DeathScreen.cs等の動的生成UIをGodotのControl系ノード＋テーマリソースに移行する。または引き続きコードベースでTextureRectを使用する。

**ステップ6: エフェクト差し替え**

ParticleManager.csのGPUParticles2Dにテクスチャを設定する。

**ステップ7: 音声設定**

AudioManager.csで各AudioStreamにリソースパスを設定する。

### アセット調達オプション

| 方法 | メリット | デメリット |
|---|---|---|
| 自作（ドット絵） | 完全な統一感 | 時間がかかる |
| AI生成（DALL-E等） | 速い | 統一感が出にくい、ドット絵は苦手 |
| フリー素材 | 無料・即使用可能 | ゲームの世界観に合わないことがある |
| 有料アセット（itch.io等） | 高品質 | コスト発生 |
| 依頼（Skeb, ココナラ等） | プロ品質 | コスト大・時間がかかる |

推奨: Phase 9完了後に自作ドット絵またはitch.ioの有料ドット絵アセットパックを使用。開発中は仮ビジュアルのまま進行する。

## Godotインポート設定

### テクスチャ

全テクスチャに以下のインポート設定を適用する。

| 設定 | 値 |
|---|---|
| Filter | Nearest（ドット絵のシャープネス維持） |
| Repeat | Disabled |
| Compress/Mode | Lossless |
| Mipmaps | OFF |

### 音声（BGM）

| 設定 | 値 |
|---|---|
| Loop | ON |
| Loop Offset | 曲ごとに設定 |

### 音声（SE）

| 設定 | 値 |
|---|---|
| Loop | OFF |