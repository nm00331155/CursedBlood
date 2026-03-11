using CursedBlood.Camera;
using CursedBlood.Debt;
using CursedBlood.Enemy;
using CursedBlood.Player;
using CursedBlood.Save;
using CursedBlood.UI;
using Godot;

namespace CursedBlood.Core
{
    public partial class GameManager : Node2D
    {
        private enum GameState
        {
            Title,
            ProfileSelect,
            Hub,
            Diving,
            Result,
            DebtSettlement
        }

        private readonly SaveManager _saveManager = new();
        private readonly DebtManager _debtManager = new();
        private readonly SonarSystem _sonar = new();
        private readonly ChainManager _chain = new();
        private readonly EnemyManager _enemies = new();

        private ChunkManager _chunks;
        private PlayerController _playerController;
        private PlayerStats _playerStats;
        private GameCamera _camera;
        private HUDManager _hud;
        private RecoveryPointManager _recoveryPoints;
        private TitleScreen _titleScreen;
        private ProtagonistSelectScreen _protagonistSelectScreen;
        private BaseHubScreen _hubScreen;
        private ResultScreen _resultScreen;
        private DebtUI _debtUi;
        private SaveData _saveData;
        private DiveResultData _currentResult;
        private GameState _state;
        private bool _profileReturnToHub;
        private bool _previewVisible = true;
        private bool _directionHintsVisible = true;
        private bool _sonarVisible = true;
        private bool _debugVisible;
        private GameplayLayoutMetrics _gameplayLayout;
        private Vector2 _lastViewportSize = new(-1f, -1f);

        [ExportGroup("HUD")]
        [Export]
        public float HudFontScale { get; set; } = 1.44f;

        [Export]
        public Vector2 HudMinimapSize { get; set; } = new Vector2(250f, 224f);

        [Export]
        public float HudMinimapOpacity { get; set; } = 0.80f;

        [ExportGroup("Recovery")]
        [Export]
        public int RecoveryActivationRadiusCells { get; set; } = 8;

        [Export]
        public float RecoveryRetentionTimeSeconds { get; set; } = 1.6f;

        [Export]
        public int RecoveryRetentionDistanceCells { get; set; } = 10;

        [Export]
        public float RecoveryProximitySlowdownMultiplier { get; set; } = 1.22f;

        [Export]
        public float RecoverySpawnReductionByDepth { get; set; } = 0.10f;

        [Export]
        public float RecoverySpawnReductionByChain { get; set; } = 1.4f;

        [Export]
        public int RecoveryMinimumActivationRadiusCells { get; set; } = 4;

        [Export]
        public float RecoveryActivationPenaltyPer100m { get; set; } = 1f;

        [Export]
        public float RecoveryActivationPenaltyPerChain { get; set; } = 0.08f;

        [Export]
        public int RecoveryMinimumRetentionDistanceCells { get; set; } = 6;

        [Export]
        public float RecoveryRetentionPenaltyPer100m { get; set; } = 0.9f;

        [ExportGroup("Chain")]
        [Export]
        public float ChainTimeLimitSeconds { get; set; } = 5f;

        [Export]
        public float ChainRewardBaseRate { get; set; } = 0.0022f;

        [Export]
        public int ChainMilestoneInterval { get; set; } = 50;

        [Export]
        public float ChainMilestoneBonusRate { get; set; } = 0.06f;

        [Export]
        public float ChainBonusSecondsEveryFive { get; set; } = 1f;

        [Export]
        public float ChainTemporaryBoostDurationSeconds { get; set; } = 4.5f;

        [Export]
        public int ChainTemporarySonarBonusRadius { get; set; } = 6;

        [Export]
        public float ChainSonarGuidanceStrength { get; set; } = 0.35f;

        [ExportGroup("Enemy")]
        [Export]
        public float EnemySpawnCheckInterval { get; set; } = 0.85f;

        [Export]
        public int EnemyMaxActive { get; set; } = 10;

        [Export]
        public float EnemyBaseSpawnChance { get; set; } = 0.05f;

        [Export]
        public float EnemySpawnChancePerDepthMeter { get; set; } = 0.0018f;

        [Export]
        public float EnemySpawnChancePerChain { get; set; } = 0.028f;

        [Export]
        public float EnemyMoveIntervalSeconds { get; set; } = 0.72f;

        [Export]
        public float EnemyDamageMultiplier { get; set; } = 1f;

        [Export]
        public float EnemyOxygenPenaltyMultiplier { get; set; } = 1f;

        [Export]
        public float EnemyMoveSlowdownMultiplier { get; set; } = 1f;

        [Export]
        public float EnemyDigSlowdownMultiplier { get; set; } = 1f;

        [Export]
        public float EnemyDebuffDurationMultiplier { get; set; } = 1f;

        [Export]
        public int EnemySpawnDistanceMin { get; set; } = 10;

        [Export]
        public int EnemySpawnDistanceMax { get; set; } = 20;

        [Export]
        public float EnemySonarDangerWeight { get; set; } = 0.78f;

        [ExportGroup("Failure Cost")]
        [Export]
        public long FailureCostBase { get; set; } = 900L;

        [Export]
        public float FailureCostDepthMultiplier { get; set; } = 6.5f;

        [Export]
        public float FailureCostInventoryMultiplier { get; set; } = 0.06f;

        [Export]
        public float FailureCostChainMultiplier { get; set; } = 55f;

        [ExportGroup("Controls")]
        [Export]
        public Vector2 VirtualPadFixedOrigin { get; set; } = new Vector2(184f, 1706f);

        [Export]
        public float VirtualPadActivationRadius { get; set; } = 184f;

        [Export]
        public float VirtualPadBaseRadius { get; set; } = 106f;

        [Export]
        public float VirtualPadMaxRadius { get; set; } = 104f;

        [Export]
        public float VirtualPadKnobRadius { get; set; } = 42f;

        [Export]
        public bool AlwaysShowVirtualPadBase { get; set; } = true;

        [ExportGroup("Layout")]
        [Export]
        public bool EnableLayoutDebugLogs { get; set; }

        public override void _Ready()
        {
            SetProcess(true);
            BuildSceneGraph();
            if (GetViewport() != null)
            {
                GetViewport().SizeChanged += HandleViewportSizeChanged;
            }

            UpdateViewportLayout(force: true);
            _saveData = _saveManager.Load();
            ShowTitleScreen();
        }

        public override void _Process(double delta)
        {
            UpdateViewportLayout();

            if (_state != GameState.Diving || _playerStats == null)
            {
                return;
            }

            UpdateDive((float)delta);
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (_state != GameState.Diving || @event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
            {
                return;
            }

            switch (keyEvent.Keycode)
            {
                case Key.F3:
                    _debugVisible = !_debugVisible;
                    ApplyVisualizationSettings();
                    GetViewport().SetInputAsHandled();
                    break;
                case Key.F4:
                    _previewVisible = !_previewVisible;
                    ApplyVisualizationSettings();
                    GetViewport().SetInputAsHandled();
                    break;
                case Key.F5:
                    _directionHintsVisible = !_directionHintsVisible;
                    ApplyVisualizationSettings();
                    GetViewport().SetInputAsHandled();
                    break;
                case Key.F6:
                    _sonarVisible = !_sonarVisible;
                    ApplyVisualizationSettings();
                    GetViewport().SetInputAsHandled();
                    break;
                case Key.F7:
                    GD.Print($"[Debug] camera zoom scale={_camera.CycleDebugZoomPreset()}");
                    GetViewport().SetInputAsHandled();
                    break;
            }
        }

        public override void _ExitTree()
        {
            if (GetViewport() != null)
            {
                GetViewport().SizeChanged -= HandleViewportSizeChanged;
            }

            if (_titleScreen != null)
            {
                _titleScreen.PrimaryRequested -= HandleTitlePrimary;
                _titleScreen.ProfileRequested -= HandleTitleProfile;
            }

            if (_protagonistSelectScreen != null)
            {
                _protagonistSelectScreen.Confirmed -= HandleProfileConfirmed;
                _protagonistSelectScreen.CancelRequested -= HandleProfileCancelled;
            }

            if (_hubScreen != null)
            {
                _hubScreen.StartDiveRequested -= StartDive;
                _hubScreen.ProfileEditRequested -= OpenProfileFromHub;
                _hubScreen.ReturnToTitleRequested -= ShowTitleScreen;
            }

            if (_resultScreen != null)
            {
                _resultScreen.ContinueRequested -= OpenDebtSettlement;
            }

            if (_hud != null)
            {
                _hud.ReturnConfirmed -= HandleReturnConfirmed;
            }

            if (_debtUi != null)
            {
                _debtUi.RepaymentSelected -= ApplyDebtSettlement;
            }
        }

        private void BuildSceneGraph()
        {
            _chunks = new ChunkManager
            {
                Visible = false
            };
            AddChild(_chunks);

            _playerController = new PlayerController
            {
                Visible = false,
                InputEnabled = false
            };
            AddChild(_playerController);

            _recoveryPoints = new RecoveryPointManager();
            AddChild(_recoveryPoints);

            _camera = new GameCamera
            {
                Enabled = false
            };
            AddChild(_camera);

            _hud = new HUDManager();
            _hud.ReturnConfirmed += HandleReturnConfirmed;
            AddChild(_hud);
            _hud.HideHud();

            _titleScreen = new TitleScreen();
            _titleScreen.PrimaryRequested += HandleTitlePrimary;
            _titleScreen.ProfileRequested += HandleTitleProfile;
            AddChild(_titleScreen);
            _titleScreen.Initialize();

            _protagonistSelectScreen = new ProtagonistSelectScreen();
            _protagonistSelectScreen.Confirmed += HandleProfileConfirmed;
            _protagonistSelectScreen.CancelRequested += HandleProfileCancelled;
            AddChild(_protagonistSelectScreen);
            _protagonistSelectScreen.Initialize();

            _hubScreen = new BaseHubScreen();
            _hubScreen.StartDiveRequested += StartDive;
            _hubScreen.ProfileEditRequested += OpenProfileFromHub;
            _hubScreen.ReturnToTitleRequested += ShowTitleScreen;
            AddChild(_hubScreen);
            _hubScreen.Initialize();

            _resultScreen = new ResultScreen();
            _resultScreen.ContinueRequested += OpenDebtSettlement;
            AddChild(_resultScreen);
            _resultScreen.Initialize();

            _debtUi = new DebtUI();
            _debtUi.RepaymentSelected += ApplyDebtSettlement;
            AddChild(_debtUi);
            _debtUi.Initialize();
        }

        private void HandleTitlePrimary()
        {
            if (!_saveData.PlayerProfile.IsProfileConfigured)
            {
                ShowProfileScreen(false);
                return;
            }

            ShowHubScreen(string.Empty);
        }

        private void HandleTitleProfile()
        {
            ShowProfileScreen(false);
        }

        private void OpenProfileFromHub()
        {
            ShowProfileScreen(true);
        }

        private void ShowProfileScreen(bool returnToHub)
        {
            HideAllScreens();
            SetDiveWorldVisible(false);
            _profileReturnToHub = returnToHub;
            _protagonistSelectScreen.ShowScreen(_saveData.PlayerProfile, returnToHub);
            _state = GameState.ProfileSelect;
        }

        private void HandleProfileConfirmed(string gender, string name)
        {
            _saveData.PlayerProfile.Gender = string.IsNullOrWhiteSpace(gender) ? "男" : gender;
            _saveData.PlayerProfile.Name = string.IsNullOrWhiteSpace(name) ? "Diver" : name.Trim();
            _saveData.PlayerProfile.IsProfileConfigured = true;
            _saveManager.Save(_saveData);
            ShowHubScreen(_profileReturnToHub ? "主人公設定を更新しました。" : "準備完了。拠点から次の潜行を開始できます。");
        }

        private void HandleProfileCancelled(bool returnToHub)
        {
            if (returnToHub)
            {
                ShowHubScreen("主人公設定は変更していません。");
                return;
            }

            ShowTitleScreen();
        }

        private void ShowTitleScreen()
        {
            HideAllScreens();
            SetDiveWorldVisible(false);
            _titleScreen.ShowScreen(_saveData.PlayerProfile, _saveData.Debt, _saveData.Ranking);
            _state = GameState.Title;
        }

        private void ShowHubScreen(string message)
        {
            HideAllScreens();
            SetDiveWorldVisible(false);
            _hubScreen.ShowScreen(_saveData, message);
            _state = GameState.Hub;
        }

        private void StartDive()
        {
            HideAllScreens();
            _playerStats = new PlayerStats();
            _playerStats.ConfigureDive(_saveData.PlayerProfile.TotalDiveCount + 1, _saveData.Debt.CurrentDebt, _saveData.PlayerProfile.CurrentMoney);

            ApplyDiveTuning();

            _chunks.Initialize();
            _recoveryPoints.Reset(_chunks, PlayerStats.StartGridPosition);
            _chain.Reset(_chunks, _playerStats.GridPosition, _playerStats);
            _enemies.Reset(_chunks, _playerStats.GridPosition);
            _chunks.MarkExplored(_playerStats.GridPosition, 9);
            _chunks.UpdateCamera(_playerStats.GridPosition);
            _chunks.SetChainVisualization(_chain.BuildVisualState());

            _playerController.Chunks = _chunks;
            _playerController.Stats = _playerStats;
            _playerController.EnemyManager = _enemies;
            _playerController.InputEnabled = true;

            _hud.Initialize(_playerStats, _chunks, _recoveryPoints, _chain);
            _playerController.VirtualPad = _hud.VirtualPadControl;
            ApplyVirtualPadLayout();
            _playerController.Reset();

            _camera.Target = _playerController;
            _camera.Enabled = true;
            _camera.MakeCurrent();
            _camera.SnapToTarget();

            UpdateViewportLayout(force: true);
            _hud.SetReturnState(false, false, 0f);
            _hud.SetChainState(_playerStats.CurrentChainCount, _playerStats.BestChainCount, _playerStats.CarryValueMultiplier, _chain.TimeRemainingSeconds, _chain.ChainTimeLimitSeconds, _chain.HasActiveCheckpoint);
            _hud.ShowNotification("↓入力で潜行開始");
            _sonar.Reset();
            SetDiveWorldVisible(true);
            ApplyVisualizationSettings();
            _currentResult = null;
            _state = GameState.Diving;
        }

        private void UpdateDive(float delta)
        {
            var diveStarted = _playerStats.HasDiveStarted;
            if (diveStarted)
            {
                _playerStats.AdvanceTime(delta);

                _chain.Update(delta, _chunks, _playerStats.GridPosition, _playerStats);
                if (_chain.TryConsumeNotification(out var chainNotification))
                {
                    _hud.ShowNotification(chainNotification);
                }

                _recoveryPoints.UpdateSpawn(_chunks, _playerStats.GridPosition, _playerStats.MaxDepthRow, _playerStats.CurrentChainCount);
            }

            var recoveryState = diveStarted
                ? _recoveryPoints.UpdateAvailability(_playerStats.GridPosition, _playerStats.PlayerSize, delta, _playerStats.CurrentDepthMeters, _playerStats.CurrentChainCount)
                : RecoveryReturnState.Unavailable;
            if (recoveryState.JustActivated)
            {
                _hud.ShowNotification("回収ポイント確保 / 帰還ボタンで撤収可能");
            }

            if (diveStarted)
            {
                _enemies.Update(delta, _chunks, _playerStats.GridPosition, _playerStats.PlayerSize, _playerStats);
                if (_enemies.TryConsumeNotification(out var enemyNotification))
                {
                    _hud.ShowNotification(enemyNotification);
                }
            }

            _sonar.Update(
                diveStarted ? delta : 0f,
                _chunks,
                _playerStats.GridPosition,
                _chain.HasActiveCheckpoint ? _chain.ActiveCheckpoint : null,
                _playerStats.CurrentSonarBonusRadius,
                _chain.SonarGuidanceStrength,
                _enemies.CurrentDanger.HasDanger ? _enemies.CurrentDanger.Hotspot : null,
                _enemies.SonarDangerWeight);
            _playerController.SetSonarReading(_sonar.CurrentReading);
            _playerController.ContextSlowdownMultiplier = recoveryState.IsAvailable ? _recoveryPoints.ProximitySlowdownMultiplier : 1f;

            _hud.SetReturnState(_playerStats.CanReturnFromSurface, recoveryState.IsAvailable, recoveryState.SecondsRemaining);
            _playerController.MovementPaused = _hud.IsReturnDialogVisible;
            _hud.SetSonarReading(_sonar.CurrentReading, _sonarVisible);
            _hud.SetChainState(_playerStats.CurrentChainCount, _playerStats.BestChainCount, _playerStats.CarryValueMultiplier, _chain.TimeRemainingSeconds, _chain.ChainTimeLimitSeconds, _chain.HasActiveCheckpoint);
            _hud.SetDebugState(_debugVisible, _playerController.GetDebugSummary());
            _chunks.SetChainVisualization(_chain.BuildVisualState());
            _chunks.UpdateCamera(_playerStats.GridPosition);

            if (diveStarted && !_playerStats.IsAlive)
            {
                EndDive(_playerStats.IsTimeExpired ? DiveEndReason.RescueTimeout : DiveEndReason.RescueDowned, _playerStats.IsTimeExpired ? "酸素切れ" : "行動不能");
            }
        }

        private void HandleReturnConfirmed(DiveEndReason endReason)
        {
            if (_state != GameState.Diving || _playerStats == null)
            {
                return;
            }

            var canReturn = endReason == DiveEndReason.RecoveryPointReturn
                ? _recoveryPoints.UpdateAvailability(_playerStats.GridPosition, _playerStats.PlayerSize, 0f, _playerStats.CurrentDepthMeters, _playerStats.CurrentChainCount).IsAvailable
                : _playerStats.CanReturnFromSurface;
            if (!canReturn)
            {
                return;
            }

            EndDive(endReason, string.Empty);
        }

        private void EndDive(DiveEndReason endReason, string rescueReason)
        {
            if (_state != GameState.Diving || _playerStats == null)
            {
                return;
            }

            _state = GameState.Result;
            _playerController.InputEnabled = false;
            _playerController.MovementPaused = false;
            _playerController.CancelTouchInput();
            _hud.HideHud();
            _chunks.SetChainVisualization(default);

            _currentResult = _playerStats.FinalizeDive(endReason, rescueReason);
            _saveData.PlayerProfile.CurrentMoney = _currentResult.MoneyAfter;
            _saveData.Debt.CurrentDebt = _currentResult.DebtAfter;
            if (!_currentResult.ReturnedSafely)
            {
                _saveData.Debt.TotalRescueCost += _currentResult.RescueCost;
            }

            _resultScreen.Show(_currentResult);
        }

        private void OpenDebtSettlement()
        {
            if (_currentResult == null)
            {
                ShowHubScreen(string.Empty);
                return;
            }

            _resultScreen.HideScreen();
            var preview = _debtManager.BuildPreview(_saveData.PlayerProfile.CurrentMoney, _saveData.Debt.CurrentDebt);
            _debtUi.ShowScreen(_currentResult, preview);
            _state = GameState.DebtSettlement;
        }

        private void ApplyDebtSettlement(DebtRepaymentChoice choice)
        {
            if (_currentResult == null)
            {
                ShowHubScreen(string.Empty);
                return;
            }

            var settlement = _debtManager.ApplyRepayment(_saveData, choice);
            _saveData.PlayerProfile.TotalDiveCount = Mathf.Max(_saveData.PlayerProfile.TotalDiveCount, _currentResult.DiveCount);
            _saveData.Records.DiveRecords.Add(DiveRecordData.FromResult(_currentResult, settlement));
            UpdateRanking(_currentResult, settlement);
            _saveManager.Save(_saveData);

            _debtUi.HideScreen();
            SetDiveWorldVisible(false);
            var message = settlement.ClearedDebt
                ? "借金完済。深層ライセンス要素は次段階で追加予定です。"
                : settlement.BuildSummaryText();
            _currentResult = null;
            ShowHubScreen(message);
        }

        private void UpdateRanking(DiveResultData result, DebtSettlementResult settlement)
        {
            _saveData.Ranking.BestDepth = Mathf.Max(_saveData.Ranking.BestDepth, result.MaxDepthMeters);
            _saveData.Ranking.BestSingleProfit = System.Math.Max(_saveData.Ranking.BestSingleProfit, result.CarryValue);
            _saveData.Ranking.TotalMoneyEarned += result.CarryValue;

            if (settlement.ClearedDebt)
            {
                if (_saveData.Ranking.FastestDebtClear == 0 || result.DiveCount < _saveData.Ranking.FastestDebtClear)
                {
                    _saveData.Ranking.FastestDebtClear = result.DiveCount;
                }
            }
        }

        private void HideAllScreens()
        {
            _titleScreen.HideScreen();
            _protagonistSelectScreen.HideScreen();
            _hubScreen.HideScreen();
            _resultScreen.HideScreen();
            _debtUi.HideScreen();
        }

        private void SetDiveWorldVisible(bool visible)
        {
            _chunks.Visible = visible;
            _playerController.Visible = visible;
            _camera.Enabled = visible;
            if (!visible)
            {
                _chunks.SetChainVisualization(default);
                _playerController.InputEnabled = false;
                _playerController.MovementPaused = false;
                _hud.HideHud();
            }
        }

        private void ApplyDiveTuning()
        {
            _hud.ApplyLayoutTuning(HudFontScale, HudMinimapSize, HudMinimapOpacity);
            UpdateViewportLayout(force: true);

            _recoveryPoints.ActivationRadiusCells = RecoveryActivationRadiusCells;
            _recoveryPoints.RetentionTimeSeconds = RecoveryRetentionTimeSeconds;
            _recoveryPoints.RetentionDistanceCells = RecoveryRetentionDistanceCells;
            _recoveryPoints.ProximitySlowdownMultiplier = RecoveryProximitySlowdownMultiplier;
            _recoveryPoints.SpawnReductionByDepth = RecoverySpawnReductionByDepth;
            _recoveryPoints.SpawnReductionByChain = RecoverySpawnReductionByChain;
            _recoveryPoints.MinActivationRadiusCells = RecoveryMinimumActivationRadiusCells;
            _recoveryPoints.ActivationRadiusPenaltyPer100m = RecoveryActivationPenaltyPer100m;
            _recoveryPoints.ActivationRadiusPenaltyPerChain = RecoveryActivationPenaltyPerChain;
            _recoveryPoints.MinRetentionDistanceCells = RecoveryMinimumRetentionDistanceCells;
            _recoveryPoints.RetentionDistancePenaltyPer100m = RecoveryRetentionPenaltyPer100m;

            _chain.ChainTimeLimitSeconds = ChainTimeLimitSeconds;
            _chain.ChainRewardBaseRate = ChainRewardBaseRate;
            _chain.ChainMilestoneInterval = ChainMilestoneInterval;
            _chain.ChainMilestoneBonusRate = ChainMilestoneBonusRate;
            _chain.EveryFiveBonusSeconds = ChainBonusSecondsEveryFive;
            _chain.TemporaryBoostDurationSeconds = ChainTemporaryBoostDurationSeconds;
            _chain.TemporarySonarBonusRadius = ChainTemporarySonarBonusRadius;
            _chain.SonarGuidanceStrength = ChainSonarGuidanceStrength;

            _enemies.SpawnCheckInterval = EnemySpawnCheckInterval;
            _enemies.MaxActiveEnemies = EnemyMaxActive;
            _enemies.BaseSpawnChance = EnemyBaseSpawnChance;
            _enemies.SpawnChancePerDepthMeter = EnemySpawnChancePerDepthMeter;
            _enemies.SpawnChancePerChain = EnemySpawnChancePerChain;
            _enemies.MoveIntervalSeconds = EnemyMoveIntervalSeconds;
            _enemies.DamageMultiplier = EnemyDamageMultiplier;
            _enemies.OxygenPenaltyMultiplier = EnemyOxygenPenaltyMultiplier;
            _enemies.MoveSlowdownMultiplier = EnemyMoveSlowdownMultiplier;
            _enemies.DigSlowdownMultiplier = EnemyDigSlowdownMultiplier;
            _enemies.DebuffDurationMultiplier = EnemyDebuffDurationMultiplier;
            _enemies.SpawnDistanceMin = EnemySpawnDistanceMin;
            _enemies.SpawnDistanceMax = EnemySpawnDistanceMax;
            _enemies.SonarDangerWeight = EnemySonarDangerWeight;

            _playerStats.FailureCostCalculator = new FailureCostCalculator
            {
                BaseCost = FailureCostBase,
                DepthMultiplier = FailureCostDepthMultiplier,
                InventoryMultiplier = FailureCostInventoryMultiplier,
                ChainMultiplier = FailureCostChainMultiplier
            };

            ApplyVirtualPadLayout();
        }

        private void ApplyVisualizationSettings()
        {
            if (_playerController == null || _chunks == null || _hud == null)
            {
                return;
            }

            _playerController.SetDirectionHintsVisible(_directionHintsVisible);
            _playerController.SetSonarVisualsVisible(_sonarVisible);
            _chunks.SetMovePreview(_playerController.MoveDebugInfo, _previewVisible);
            _chunks.SetMoveDebugInfo(_playerController.MoveDebugInfo, _debugVisible);
            _hud.SetControlHintsVisible(_debugVisible);
            _hud.SetDebugState(_debugVisible, _playerController.GetDebugSummary());
            _hud.SetSonarReading(_sonar.CurrentReading, _sonarVisible);
            _hud.SetChainState(_playerStats?.CurrentChainCount ?? 0, _playerStats?.BestChainCount ?? 0, _playerStats?.CarryValueMultiplier ?? 1f, _chain.TimeRemainingSeconds, _chain.ChainTimeLimitSeconds, _chain.HasActiveCheckpoint);
            _chunks.RequestRefresh();
        }

        private void HandleViewportSizeChanged()
        {
            UpdateViewportLayout(force: true);
        }

        private void UpdateViewportLayout(bool force = false)
        {
            var viewport = GetViewport();
            var visibleRect = viewport?.GetVisibleRect() ?? new Rect2(Vector2.Zero, GameplayLayoutCalculator.ReferenceScreenSize);
            if (!force && _lastViewportSize.IsEqualApprox(visibleRect.Size))
            {
                return;
            }

            _lastViewportSize = visibleRect.Size;
            _gameplayLayout = GameplayLayoutCalculator.Calculate(visibleRect, HudMinimapSize);
            _camera?.ApplyViewportLayout(_gameplayLayout, EnableLayoutDebugLogs);
            _hud?.ApplyViewportLayout(_gameplayLayout);
            _titleScreen?.ApplyViewportLayout();
            _protagonistSelectScreen?.ApplyViewportLayout();
            _hubScreen?.ApplyViewportLayout();
            _resultScreen?.ApplyViewportLayout();
            _debtUi?.ApplyViewportLayout();
            ApplyVirtualPadLayout();

            if (EnableLayoutDebugLogs)
            {
                GD.Print($"[Layout] screen={visibleRect.Size.X:0}x{visibleRect.Size.Y:0} reservedTop={_gameplayLayout.ReservedTop:0} reservedRight={_gameplayLayout.ReservedRight:0} reservedBottom={_gameplayLayout.ReservedBottom:0} reservedLeft={_gameplayLayout.ReservedLeft:0} available={_gameplayLayout.AvailableSize.X:0}x{_gameplayLayout.AvailableSize.Y:0}");
            }
        }

        private void ApplyVirtualPadLayout()
        {
            if (_playerController == null)
            {
                return;
            }

            _playerController.ApplyVirtualPadSettings(BuildVirtualPadSettings());
        }

        private VirtualPadSettings BuildVirtualPadSettings()
        {
            var fixedOrigin = _gameplayLayout.ScreenSize == Vector2.Zero
                ? VirtualPadFixedOrigin
                : GameplayLayoutCalculator.ResolveVirtualPadOrigin(_gameplayLayout, VirtualPadFixedOrigin);
            return new VirtualPadSettings
            {
                UseFixedOrigin = true,
                FixedOrigin = fixedOrigin,
                ActivationRadius = VirtualPadActivationRadius,
                BaseRadius = VirtualPadBaseRadius,
                MaxRadius = VirtualPadMaxRadius,
                KnobRadius = VirtualPadKnobRadius,
                AlwaysShowBase = AlwaysShowVirtualPadBase
            };
        }
    }
}