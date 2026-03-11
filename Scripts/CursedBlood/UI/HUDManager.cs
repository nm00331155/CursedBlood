using System;
using CursedBlood.Core;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.UI
{
    public partial class HUDManager : CanvasLayer
    {
        private static readonly Rect2 ReferenceInfoPanelRect = new(new Vector2(18f, 18f), new Vector2(590f, 188f));
        private static readonly Rect2 ReferenceNotificationRect = new(new Vector2(220f, 218f), new Vector2(640f, 50f));
        private const float MapPanelTopMargin = 18f;
        private const float MapPanelRightMargin = 18f;
        private const float SonarBottomMargin = 264f;
        private const float ReturnRightMargin = 34f;
        private const float ReturnBottomMargin = 76f;

        private enum ReturnMode
        {
            None,
            Surface,
            Recovery
        }

        private const float OxygenBarWidth = 240f;

        private PlayerStats _stats;
        private ChunkManager _chunks;
        private RecoveryPointManager _recoveryPoints;
        private ChainManager _chainManager;
        private bool _built;
        private bool _visible;
        private bool _showControlHints;
        private bool _debugEnabled;
        private bool _sonarVisible = true;
        private string _debugText = string.Empty;
        private string _notificationText = string.Empty;
        private float _notificationTimeRemaining;
        private float _fontScale = 1.44f;
        private float _minimapOpacity = 0.80f;
        private Vector2 _minimapSize = new(250f, 224f);
        private GameplayLayoutMetrics _layoutMetrics;
        private ReturnMode _returnMode;
        private float _returnRetentionSeconds;
        private SonarReading _sonarReading = new(SonarSignalStrength.None, SonarTargetKind.None, CellType.Empty, Vector2I.Zero, 0, Vector2I.Zero);
        private float _oxygenBarCurrentWidth = OxygenBarWidth;
        private Control _root;
        private Panel _infoPanel;
        private Label _oxygenCaptionLabel;
        private ColorRect _oxygenBarBackground;
        private ColorRect _oxygenBarFill;
        private Label _diveLabel;
        private Label _timerLabel;
        private Label _depthLabel;
        private Label _economyLabel;
        private Label _debtLabel;
        private Label _salvageLabel;
        private Label _phaseLabel;
        private Label _statusLabel;
        private Label _chainLabel;
        private Label _chainTargetLabel;
        private Label _sonarLabel;
        private Label _notificationLabel;
        private Label _returnHintLabel;
        private Label _debugLabel;
        private Panel _mapPanel;
        private Panel _sonarPanel;
        private Panel _debugPanel;
        private Panel _returnPanel;
        private Panel _returnDialog;
        private ColorRect _dialogBackdrop;
        private MiniMapOverlay _miniMap;
        private VirtualPad _virtualPad;
        private Button _returnButton;

        public bool IsReturnDialogVisible => _returnDialog != null && _returnDialog.Visible;

        public event Action<DiveEndReason> ReturnConfirmed;

        public VirtualPad VirtualPadControl
        {
            get
            {
                BuildUiIfNeeded();
                return _virtualPad;
            }
        }

        public override void _Ready()
        {
            SetProcess(true);
        }

        public void ApplyLayoutTuning(float fontScale, Vector2 minimapSize, float minimapOpacity)
        {
            _fontScale = Mathf.Clamp(fontScale, 1f, 1.8f);
            _minimapSize = new Vector2(Mathf.Clamp(minimapSize.X, 180f, 320f), Mathf.Clamp(minimapSize.Y, 160f, 300f));
            _minimapOpacity = Mathf.Clamp(minimapOpacity, 0.2f, 0.95f);

            if (_built)
            {
                var fallbackLayout = _layoutMetrics.ScreenSize == Vector2.Zero
                    ? GameplayLayoutCalculator.Calculate(new Rect2(Vector2.Zero, GameplayLayoutCalculator.ReferenceScreenSize), _minimapSize)
                    : GameplayLayoutCalculator.Calculate(_layoutMetrics.VisibleRect, _minimapSize);
                ApplyViewportLayout(fallbackLayout);
            }
        }

        public void Initialize(PlayerStats stats, ChunkManager chunks, RecoveryPointManager recoveryPoints, ChainManager chainManager)
        {
            _stats = stats;
            _chunks = chunks;
            _recoveryPoints = recoveryPoints;
            _chainManager = chainManager;
            BuildUiIfNeeded();
            _miniMap.Configure(_minimapOpacity);
            _miniMap.Initialize(_chunks, _stats, _recoveryPoints, _chainManager);
            ApplyViewportLayout(_layoutMetrics.ScreenSize == Vector2.Zero
                ? GameplayLayoutCalculator.Calculate(new Rect2(Vector2.Zero, GameplayLayoutCalculator.ReferenceScreenSize), _minimapSize)
                : _layoutMetrics);
            HideReturnDialog();
            SetVisibleState(true);
        }

        public void ApplyViewportLayout(GameplayLayoutMetrics layoutMetrics)
        {
            _layoutMetrics = layoutMetrics;
            if (!_built)
            {
                return;
            }

            _root.Position = layoutMetrics.VisibleRect.Position;
            _root.Size = layoutMetrics.ScreenSize;

            var mapPanelSize = GameplayLayoutCalculator.ResolveMapPanelSize(_minimapSize);
            var topOverlayHeight = GameplayLayoutCalculator.ResolveTopOverlayHeight(_minimapSize);

            _infoPanel.Position = ReferenceInfoPanelRect.Position;
            _infoPanel.Size = new Vector2(
                Mathf.Max(624f, layoutMetrics.ScreenSize.X - layoutMetrics.ReservedRight - (ReferenceInfoPanelRect.Position.X * 2f)),
                topOverlayHeight);

            var infoInnerWidth = _infoPanel.Size.X - 40f;
            var rightColumnWidth = Mathf.Clamp(infoInnerWidth * 0.36f, 228f, 324f);
            var leftColumnWidth = Mathf.Max(268f, infoInnerWidth - rightColumnWidth - 18f);
            var leftX = 20f;
            var rightX = leftX + leftColumnWidth + 18f;
            _oxygenBarCurrentWidth = Mathf.Clamp(leftColumnWidth - 64f, 188f, 356f);

            _diveLabel.Position = new Vector2(leftX, 16f);
            _diveLabel.Size = new Vector2(leftColumnWidth, 28f);
            _diveLabel.AddThemeFontSizeOverride("font_size", ScaleFont(20));

            _timerLabel.Position = new Vector2(rightX, 8f);
            _timerLabel.Size = new Vector2(rightColumnWidth, 52f);
            _timerLabel.AddThemeFontSizeOverride("font_size", ScaleFont(36));

            _depthLabel.Position = new Vector2(leftX, 50f);
            _depthLabel.Size = new Vector2(leftColumnWidth, 40f);
            _depthLabel.AddThemeFontSizeOverride("font_size", ScaleFont(30));

            _economyLabel.Position = new Vector2(rightX, 60f);
            _economyLabel.Size = new Vector2(rightColumnWidth, 26f);
            _economyLabel.AddThemeFontSizeOverride("font_size", ScaleFont(18));

            _debtLabel.Position = new Vector2(rightX, 88f);
            _debtLabel.Size = new Vector2(rightColumnWidth, 26f);
            _debtLabel.AddThemeFontSizeOverride("font_size", ScaleFont(18));

            _salvageLabel.Position = new Vector2(rightX, 116f);
            _salvageLabel.Size = new Vector2(rightColumnWidth, 20f);
            _salvageLabel.AddThemeFontSizeOverride("font_size", ScaleFont(15));

            _oxygenCaptionLabel.Position = new Vector2(leftX, 96f);
            _oxygenCaptionLabel.Size = new Vector2(56f, 24f);
            _oxygenCaptionLabel.AddThemeFontSizeOverride("font_size", ScaleFont(16));

            _oxygenBarBackground.Position = new Vector2(leftX + 60f, 102f);
            _oxygenBarBackground.Size = new Vector2(_oxygenBarCurrentWidth, 16f);
            _oxygenBarFill.Position = _oxygenBarBackground.Position;
            _oxygenBarFill.Size = new Vector2(_oxygenBarCurrentWidth, 16f);

            _phaseLabel.Position = new Vector2(rightX, 144f);
            _phaseLabel.Size = new Vector2(rightColumnWidth, 22f);
            _phaseLabel.AddThemeFontSizeOverride("font_size", ScaleFont(17));

            _chainLabel.Position = new Vector2(leftX, 140f);
            _chainLabel.Size = new Vector2(leftColumnWidth, 22f);
            _chainLabel.AddThemeFontSizeOverride("font_size", ScaleFont(18));

            _chainTargetLabel.Position = new Vector2(leftX, 172f);
            _chainTargetLabel.Size = new Vector2(infoInnerWidth, 24f);
            _chainTargetLabel.AddThemeFontSizeOverride("font_size", ScaleFont(16));

            _statusLabel.Position = new Vector2(leftX, 206f);
            _statusLabel.Size = new Vector2(infoInnerWidth, Mathf.Max(54f, _infoPanel.Size.Y - 222f));
            _statusLabel.AddThemeFontSizeOverride("font_size", ScaleFont(15));

            var mapPanelRect = GameplayLayoutCalculator.AlignTopRight(layoutMetrics, mapPanelSize, MapPanelRightMargin, MapPanelTopMargin);
            _mapPanel.Position = mapPanelRect.Position;
            _mapPanel.Size = mapPanelRect.Size;
            _miniMap.Position = new Vector2(12f, 36f);
            _miniMap.Size = _minimapSize;

            var notificationWidth = Mathf.Min(ReferenceNotificationRect.Size.X, layoutMetrics.ScreenSize.X - 160f);
            _notificationLabel.Position = new Vector2((layoutMetrics.ScreenSize.X - notificationWidth) * 0.5f, _infoPanel.Position.Y + _infoPanel.Size.Y + 12f);
            _notificationLabel.Size = new Vector2(notificationWidth, ReferenceNotificationRect.Size.Y);
            _notificationLabel.AddThemeFontSizeOverride("font_size", ScaleFont(20));

            _sonarPanel.Size = new Vector2(Mathf.Clamp(layoutMetrics.PlayfieldRect.Size.X * 0.82f, 420f, 700f), 70f);
            _sonarPanel.Position = GameplayLayoutCalculator.AlignBottomCenter(layoutMetrics, _sonarPanel.Size, SonarBottomMargin).Position;
            _sonarLabel.Position = new Vector2(20f, 12f);
            _sonarLabel.Size = new Vector2(_sonarPanel.Size.X - 40f, 42f);
            _sonarLabel.AddThemeFontSizeOverride("font_size", ScaleFont(20));

            _returnPanel.Position = GameplayLayoutCalculator.AlignBottomRight(layoutMetrics, _returnPanel.Size, ReturnRightMargin, ReturnBottomMargin).Position;
            _returnButton.AddThemeFontSizeOverride("font_size", ScaleFont(22));
            _returnHintLabel.AddThemeFontSizeOverride("font_size", ScaleFont(14));

            _debugPanel.Position = new Vector2(18f, _notificationLabel.Position.Y + _notificationLabel.Size.Y + 10f);
            _debugLabel.AddThemeFontSizeOverride("font_size", ScaleFont(14));

            _dialogBackdrop.Position = Vector2.Zero;
            _dialogBackdrop.Size = layoutMetrics.ScreenSize;
            _returnDialog.Position = GameplayLayoutCalculator.AlignCentered(layoutMetrics, _returnDialog.Size).Position;
        }

        public void HideHud()
        {
            if (_built)
            {
                HideReturnDialog();
            }

            SetVisibleState(false);
        }

        public override void _Process(double delta)
        {
            if (_stats == null || !_built || !_visible)
            {
                return;
            }

            _notificationTimeRemaining = Mathf.Max(0f, _notificationTimeRemaining - (float)delta);
            _notificationLabel.Visible = _notificationTimeRemaining > 0f && !string.IsNullOrWhiteSpace(_notificationText);
            _notificationLabel.Text = _notificationText;

            var oxygenRatio = _stats.OxygenRatio;
            _oxygenBarFill.Size = new Vector2(_oxygenBarCurrentWidth * oxygenRatio, _oxygenBarFill.Size.Y);
            _oxygenBarFill.Color = oxygenRatio switch
            {
                < 0.18f => new Color(0.96f, 0.28f, 0.22f),
                < 0.45f => new Color(0.98f, 0.69f, 0.18f),
                _ => new Color(0.24f, 0.86f, 0.74f)
            };

            var waitingForStart = !_stats.HasDiveStarted;
            _diveLabel.Text = $"潜行 {_stats.DiveCount:00}";
            _timerLabel.Text = $"{_stats.RemainingDiveSeconds}s";
            _depthLabel.Text = waitingForStart ? "地上開始" : $"深度 {_stats.CurrentDepthMeters}m";
            _economyLabel.Text = $"所持 {_stats.CurrentMoney:N0}";
            _debtLabel.Text = $"借金 {_stats.CurrentDebt:N0}";
            _salvageLabel.Text = $"今回 {_stats.SalvageValue:N0} / 掘削 {_stats.BlocksDug} / 鉱石 {_stats.OresCollected}";
            _phaseLabel.Text = waitingForStart ? "準備中" : $"{_stats.PhaseLabel} x{_stats.PhaseMultiplier:0.00}";
            _phaseLabel.AddThemeColorOverride("font_color", waitingForStart
                ? new Color(0.82f, 0.92f, 1.00f)
                : _stats.Phase switch
                {
                    DivePhase.Stable => new Color(0.74f, 0.97f, 0.90f),
                    DivePhase.Worn => new Color(1.00f, 0.88f, 0.46f),
                    _ => new Color(1.00f, 0.62f, 0.62f)
                });

            _chainLabel.Text = $"CHAIN {_stats.CurrentChainCount} / BEST {_stats.BestChainCount} / x{_stats.CarryValueMultiplier:0.000}";
            _chainTargetLabel.Text = waitingForStart
                ? "↓入力で潜行開始"
                : _chainManager != null && _chainManager.HasActiveCheckpoint
                    ? (_stats.CurrentChainCount > 0 ? $"次目標 {_chainManager.TimeRemainingSeconds:0.0}s" : "下降目標通過でチェイン開始")
                    : "下降目標を探索中";

            _statusLabel.Text = waitingForStart
                ? "地上待機。ゲームパッドやスワイプで潜行開始。"
                : _returnMode switch
                {
                    ReturnMode.Recovery => _returnRetentionSeconds > 0f
                        ? $"回収ポイント確保 / 帰還可能 {_returnRetentionSeconds:0.0}s"
                        : "回収ポイント確保 / 帰還可能",
                    ReturnMode.Surface => "地上帰還可能 / 帰還ボタンで撤収",
                    _ => "掘削を進めつつ、回収ポイントと下降目標を追う"
                };
            if (!string.IsNullOrWhiteSpace(_stats.ActiveHazardLabel))
            {
                _statusLabel.Text += $" / {_stats.ActiveHazardLabel}";
            }
            _statusLabel.AddThemeColorOverride("font_color", _returnMode == ReturnMode.None
                ? new Color(0.84f, 0.90f, 0.96f)
                : new Color(0.98f, 0.94f, 0.72f));

            _sonarLabel.Visible = _sonarVisible;
            _sonarLabel.Text = _sonarVisible ? _sonarReading.GetDisplayText() : string.Empty;
            _sonarLabel.AddThemeColorOverride("font_color", _sonarReading.Strength switch
            {
                SonarSignalStrength.Near => new Color(0.62f, 1.00f, 0.96f),
                SonarSignalStrength.Medium => new Color(0.86f, 0.96f, 1.00f),
                SonarSignalStrength.Far => new Color(0.94f, 0.92f, 0.78f),
                _ => new Color(0.94f, 0.96f, 0.98f)
            });

            _returnPanel.Visible = _returnMode != ReturnMode.None;
            _returnButton.Visible = _returnMode != ReturnMode.None;
            _returnButton.Text = _returnMode == ReturnMode.Recovery ? "回収地点から帰還" : "地上から帰還";
            _returnHintLabel.Text = _returnMode == ReturnMode.Recovery
                ? "確認ダイアログ中は移動停止"
                : "地上帯から確実に撤収する";

            var showDebug = _debugEnabled && !string.IsNullOrWhiteSpace(_debugText);
            _debugPanel.Visible = showDebug;
            _debugLabel.Visible = showDebug;
            _debugLabel.Text = _showControlHints
                ? _debugText + "\nF4 Preview / F5 Hint / F6 Sonar / F7 Zoom"
                : _debugText;
        }

        public void SetSonarReading(SonarReading reading, bool visible)
        {
            _sonarReading = reading;
            _sonarVisible = visible;
            _miniMap?.SetSonarTarget(reading);
        }

        public void SetChainState(int currentChain, int bestChain, float carryMultiplier, float checkpointTimeRemaining, float checkpointTimeLimit, bool checkpointActive)
        {
            _chainLabel.Text = $"CHAIN {currentChain} / BEST {bestChain} / x{carryMultiplier:0.000}";
            _chainTargetLabel.Text = checkpointActive
                ? (currentChain > 0 ? $"次の下降目標 {checkpointTimeRemaining:0.0}s" : "下降目標を通過してチェイン開始")
                : "下降目標を探索中";
        }

        public void ShowNotification(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            _notificationText = message;
            _notificationTimeRemaining = 2.4f;
        }

        public void SetControlHintsVisible(bool visible)
        {
            _showControlHints = visible;
        }

        public void SetDebugState(bool enabled, string debugText)
        {
            _debugEnabled = enabled;
            _debugText = debugText ?? string.Empty;
        }

        public void SetReturnState(bool surfaceReady, bool recoveryReady, float retentionSeconds)
        {
            var nextMode = recoveryReady
                ? ReturnMode.Recovery
                : surfaceReady
                    ? ReturnMode.Surface
                    : ReturnMode.None;

            _returnMode = nextMode;
            _returnRetentionSeconds = Mathf.Max(0f, retentionSeconds);
            if (_returnMode == ReturnMode.None)
            {
                HideReturnDialog();
            }
        }

        private void BuildUiIfNeeded()
        {
            if (_built)
            {
                return;
            }

            _root = new Control
            {
                Position = Vector2.Zero,
                Size = new Vector2(1080f, 1920f),
                MouseFilter = Control.MouseFilterEnum.Pass
            };
            AddChild(_root);

            _infoPanel = CreatePanel(ReferenceInfoPanelRect.Position, ReferenceInfoPanelRect.Size);
            _root.AddChild(_infoPanel);

            _diveLabel = CreateLabel(new Vector2(22f, 16f), new Vector2(180f, 28f), ScaleFont(22), HorizontalAlignment.Left);
            _infoPanel.AddChild(_diveLabel);

            _timerLabel = CreateLabel(new Vector2(376f, 10f), new Vector2(180f, 58f), ScaleFont(46), HorizontalAlignment.Right);
            _infoPanel.AddChild(_timerLabel);

            _depthLabel = CreateLabel(new Vector2(22f, 52f), new Vector2(220f, 48f), ScaleFont(40), HorizontalAlignment.Left);
            _infoPanel.AddChild(_depthLabel);

            _economyLabel = CreateLabel(new Vector2(238f, 58f), new Vector2(318f, 28f), ScaleFont(22), HorizontalAlignment.Right);
            _infoPanel.AddChild(_economyLabel);

            _debtLabel = CreateLabel(new Vector2(238f, 86f), new Vector2(318f, 28f), ScaleFont(22), HorizontalAlignment.Right);
            _infoPanel.AddChild(_debtLabel);

            _salvageLabel = CreateLabel(new Vector2(238f, 114f), new Vector2(318f, 24f), ScaleFont(18), HorizontalAlignment.Right);
            _infoPanel.AddChild(_salvageLabel);

            _oxygenCaptionLabel = CreateLabel(new Vector2(22f, 118f), new Vector2(60f, 20f), ScaleFont(16), HorizontalAlignment.Left);
            _oxygenCaptionLabel.Text = "酸素";
            _infoPanel.AddChild(_oxygenCaptionLabel);

            _oxygenBarBackground = new ColorRect
            {
                Position = new Vector2(78f, 122f),
                Size = new Vector2(OxygenBarWidth, 14f),
                Color = new Color(0.17f, 0.23f, 0.29f, 0.9f),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            _infoPanel.AddChild(_oxygenBarBackground);

            _oxygenBarFill = new ColorRect
            {
                Position = _oxygenBarBackground.Position,
                Size = _oxygenBarBackground.Size,
                Color = new Color(0.24f, 0.86f, 0.74f),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            _infoPanel.AddChild(_oxygenBarFill);

            _phaseLabel = CreateLabel(new Vector2(336f, 116f), new Vector2(220f, 20f), ScaleFont(17), HorizontalAlignment.Right);
            _infoPanel.AddChild(_phaseLabel);

            _chainLabel = CreateLabel(new Vector2(22f, 150f), new Vector2(300f, 22f), ScaleFont(17), HorizontalAlignment.Left);
            _infoPanel.AddChild(_chainLabel);

            _chainTargetLabel = CreateLabel(new Vector2(22f, 168f), new Vector2(534f, 22f), ScaleFont(16), HorizontalAlignment.Center);
            _infoPanel.AddChild(_chainTargetLabel);

            _statusLabel = CreateLabel(new Vector2(22f, 198f), new Vector2(534f, 50f), ScaleFont(15), HorizontalAlignment.Left);
            _statusLabel.VerticalAlignment = VerticalAlignment.Top;
            _infoPanel.AddChild(_statusLabel);

            _mapPanel = CreatePanel(new Vector2(812f, 18f), GameplayLayoutCalculator.ResolveMapPanelSize(_minimapSize));
            _root.AddChild(_mapPanel);

            var mapLabel = CreateLabel(new Vector2(12f, 10f), new Vector2(180f, 22f), ScaleFont(18), HorizontalAlignment.Left);
            mapLabel.Text = "経路 MAP";
            _mapPanel.AddChild(mapLabel);

            _miniMap = new MiniMapOverlay
            {
                Position = new Vector2(12f, 36f),
                Size = _minimapSize
            };
            _mapPanel.AddChild(_miniMap);

            _virtualPad = new VirtualPad
            {
                Name = "VirtualPad"
            };
            _root.AddChild(_virtualPad);

            _notificationLabel = CreateLabel(ReferenceNotificationRect.Position, ReferenceNotificationRect.Size, ScaleFont(22), HorizontalAlignment.Center);
            _notificationLabel.AddThemeColorOverride("font_color", new Color(0.99f, 0.92f, 0.70f));
            _notificationLabel.Visible = false;
            _root.AddChild(_notificationLabel);

            _sonarPanel = CreatePanel(new Vector2(240f, 1586f), new Vector2(600f, 70f));
            _root.AddChild(_sonarPanel);

            _sonarLabel = CreateLabel(new Vector2(20f, 12f), new Vector2(560f, 42f), ScaleFont(22), HorizontalAlignment.Center);
            _sonarPanel.AddChild(_sonarLabel);

            _returnPanel = CreatePanel(new Vector2(706f, 1672f), new Vector2(340f, 172f));
            _returnPanel.Visible = false;
            _root.AddChild(_returnPanel);

            _returnButton = new Button
            {
                Position = new Vector2(20f, 58f),
                Size = new Vector2(300f, 70f),
                Text = "帰還",
                CustomMinimumSize = new Vector2(300f, 70f)
            };
            _returnButton.AddThemeFontSizeOverride("font_size", ScaleFont(24));
            _returnButton.Pressed += OpenReturnDialog;
            _returnPanel.AddChild(_returnButton);

            _returnHintLabel = CreateLabel(new Vector2(20f, 132f), new Vector2(300f, 24f), ScaleFont(15), HorizontalAlignment.Center);
            _returnPanel.AddChild(_returnHintLabel);

            _debugPanel = CreatePanel(new Vector2(18f, 218f), new Vector2(760f, 112f));
            _debugPanel.Visible = false;
            _root.AddChild(_debugPanel);

            _debugLabel = CreateLabel(new Vector2(16f, 14f), new Vector2(726f, 82f), ScaleFont(16), HorizontalAlignment.Left);
            _debugLabel.Visible = false;
            _debugLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _debugPanel.AddChild(_debugLabel);

            _dialogBackdrop = new ColorRect
            {
                Position = Vector2.Zero,
                Size = new Vector2(1080f, 1920f),
                Color = new Color(0f, 0f, 0f, 0.42f),
                Visible = false,
                MouseFilter = Control.MouseFilterEnum.Stop
            };
            _root.AddChild(_dialogBackdrop);

            _returnDialog = CreatePanel(new Vector2(224f, 666f), new Vector2(632f, 316f));
            _returnDialog.Visible = false;
            _returnDialog.MouseFilter = Control.MouseFilterEnum.Stop;
            _root.AddChild(_returnDialog);

            var dialogTitle = CreateLabel(new Vector2(36f, 28f), new Vector2(560f, 42f), ScaleFont(28), HorizontalAlignment.Center);
            dialogTitle.Text = "帰還を確認";
            _returnDialog.AddChild(dialogTitle);

            var dialogMessage = CreateLabel(new Vector2(46f, 92f), new Vector2(540f, 72f), ScaleFont(20), HorizontalAlignment.Center);
            dialogMessage.Text = "ここで撤収すると今回の回収分を持ち帰る。";
            _returnDialog.AddChild(dialogMessage);

            var confirmButton = new Button
            {
                Position = new Vector2(72f, 212f),
                Size = new Vector2(218f, 68f),
                Text = "帰還する"
            };
            confirmButton.AddThemeFontSizeOverride("font_size", ScaleFont(22));
            confirmButton.Pressed += ConfirmReturn;
            _returnDialog.AddChild(confirmButton);

            var cancelButton = new Button
            {
                Position = new Vector2(342f, 212f),
                Size = new Vector2(218f, 68f),
                Text = "まだ続ける"
            };
            cancelButton.AddThemeFontSizeOverride("font_size", ScaleFont(22));
            cancelButton.Pressed += HideReturnDialog;
            _returnDialog.AddChild(cancelButton);

            _built = true;
            ApplyViewportLayout(_layoutMetrics.ScreenSize == Vector2.Zero
                ? GameplayLayoutCalculator.Calculate(new Rect2(Vector2.Zero, GameplayLayoutCalculator.ReferenceScreenSize), _minimapSize)
                : _layoutMetrics);
            SetVisibleState(false);
        }

        private void OpenReturnDialog()
        {
            if (_returnMode == ReturnMode.None)
            {
                return;
            }

            _dialogBackdrop.Visible = true;
            _returnDialog.Visible = true;
        }

        private void ConfirmReturn()
        {
            if (_returnMode == ReturnMode.None)
            {
                return;
            }

            var reason = _returnMode == ReturnMode.Recovery
                ? DiveEndReason.RecoveryPointReturn
                : DiveEndReason.SurfaceReturn;
            HideReturnDialog();
            ReturnConfirmed?.Invoke(reason);
        }

        private void HideReturnDialog()
        {
            if (_dialogBackdrop != null)
            {
                _dialogBackdrop.Visible = false;
            }

            if (_returnDialog != null)
            {
                _returnDialog.Visible = false;
            }
        }

        private void SetVisibleState(bool visible)
        {
            _visible = visible;
            if (_root != null)
            {
                _root.Visible = visible;
            }
        }

        private int ScaleFont(int baseFontSize)
        {
            return Mathf.RoundToInt(baseFontSize * _fontScale);
        }

        private static Panel CreatePanel(Vector2 position, Vector2 size)
        {
            var panel = new Panel
            {
                Position = position,
                Size = size,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            var panelStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.05f, 0.07f, 0.10f, 0.74f),
                BorderColor = new Color(0.80f, 0.87f, 0.94f, 0.62f),
                BorderWidthTop = 2,
                BorderWidthBottom = 2,
                BorderWidthLeft = 2,
                BorderWidthRight = 2,
                CornerRadiusTopLeft = 20,
                CornerRadiusTopRight = 20,
                CornerRadiusBottomLeft = 20,
                CornerRadiusBottomRight = 20
            };
            panel.AddThemeStyleboxOverride("panel", panelStyle);
            return panel;
        }

        private static Label CreateLabel(Vector2 position, Vector2 size, int fontSize, HorizontalAlignment alignment)
        {
            var label = new Label
            {
                Position = position,
                Size = size,
                HorizontalAlignment = alignment,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            label.AddThemeFontSizeOverride("font_size", fontSize);
            label.AddThemeColorOverride("font_color", new Color(0.95f, 0.96f, 0.98f));
            return label;
        }
    }
}