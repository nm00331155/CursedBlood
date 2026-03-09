using CursedBlood.Camera;
using CursedBlood.Debt;
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
            ProtagonistSelect,
            Hub,
            Playing,
            Result,
            DebtRepay
        }

        private ChunkManager _chunks;
        private PlayerController _playerController;
        private PlayerStats _playerStats;
        private GameCamera _camera;
        private HUDManager _hud;
        private VirtualPad _virtualPad;
        private TitleScreen _titleScreen;
        private ProtagonistSelectScreen _protagonistSelectScreen;
        private BaseHubScreen _hubScreen;
        private ResultScreen _resultScreen;
        private DebtUI _debtScreen;
        private RecoveryPointManager _recoveryPoints;
        private SonarSystem _sonar;
        private DebtManager _debtManager;
        private SaveManager _saveManager;
        private SaveData _saveData;
        private DiveResultData _pendingDiveResult;
        private GameState _state;
        private bool _debugVisible;

        public override void _Ready()
        {
            SetProcess(true);
            SetProcessUnhandledInput(true);
            _saveManager = new SaveManager();
            _saveData = _saveManager.Load();
            _debtManager = new DebtManager();
            BuildSceneGraph();
            ShowTitleScreen();
        }

        public override void _Process(double delta)
        {
            UpdateDebugPresentation();

            if (_state != GameState.Playing || _playerStats == null)
            {
                return;
            }

            _playerStats.AdvanceTime((float)delta);
            _recoveryPoints.UpdateSpawn(_chunks, _playerStats.MaxDepthRow);
            _sonar.Update((float)delta, _chunks, _playerStats.GridPosition);

            _chunks.UpdateCamera(_playerStats.GridPosition.Y);
            UpdateHudState();
            UpdateDebugPresentation();

            if (_playerStats.IsTimeExpired)
            {
                EndDive(DiveEndReason.RescueTimeout, "時間切れ");
            }
            else if (_playerStats.CurrentHp <= 0)
            {
                EndDive(DiveEndReason.RescueDowned, "行動不能");
            }
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
            {
                return;
            }

            switch (keyEvent.Keycode)
            {
                case Key.F3:
                    if (_state == GameState.Playing)
                    {
                        _debugVisible = !_debugVisible;
                        _hud?.SetDebugVisible(_debugVisible);
                        UpdateDebugPresentation();
                        GetViewport().SetInputAsHandled();
                    }
                    break;
                case Key.Bracketleft:
                case Key.Minus:
                    if (_state == GameState.Playing)
                    {
                        AdjustVirtualPadOpacity(-0.05f);
                        GetViewport().SetInputAsHandled();
                    }
                    break;
                case Key.Bracketright:
                case Key.Equal:
                    if (_state == GameState.Playing)
                    {
                        AdjustVirtualPadOpacity(0.05f);
                        GetViewport().SetInputAsHandled();
                    }
                    break;
            }
        }

        public override void _ExitTree()
        {
            if (_hud != null)
            {
                _hud.ReturnRequested -= HandleReturnRequested;
            }

            if (_resultScreen != null)
            {
                _resultScreen.ContinueRequested -= HandleResultContinue;
            }

            if (_titleScreen != null)
            {
                _titleScreen.PrimaryRequested -= HandleTitlePrimaryRequested;
                _titleScreen.ProfileRequested -= ShowProtagonistSelectFromTitle;
            }

            if (_protagonistSelectScreen != null)
            {
                _protagonistSelectScreen.Confirmed -= HandleProfileConfirmed;
                _protagonistSelectScreen.CancelRequested -= HandleProfileSelectCancelled;
            }

            if (_hubScreen != null)
            {
                _hubScreen.StartDiveRequested -= HandleHubStartDive;
                _hubScreen.ProfileEditRequested -= ShowProtagonistSelectFromHub;
                _hubScreen.ReturnToTitleRequested -= ShowTitleScreen;
            }

            if (_debtScreen != null)
            {
                _debtScreen.RepaymentSelected -= HandleRepaymentSelected;
            }

            if (_saveData != null)
            {
                _saveManager?.Save(_saveData);
            }
        }

        private void BuildSceneGraph()
        {
            _chunks = new ChunkManager();
            AddChild(_chunks);

            _playerController = new PlayerController();
            AddChild(_playerController);

            _camera = new GameCamera();
            AddChild(_camera);

            _hud = new HUDManager();
            AddChild(_hud);
            _hud.ReturnRequested += HandleReturnRequested;
            _hud.Visible = false;

            _virtualPad = new VirtualPad();
            _virtualPad.ApplySettings(CreateVirtualPadSettings());
            _hud.AddChild(_virtualPad);

            _titleScreen = new TitleScreen();
            AddChild(_titleScreen);
            _titleScreen.Initialize();
            _titleScreen.PrimaryRequested += HandleTitlePrimaryRequested;
            _titleScreen.ProfileRequested += ShowProtagonistSelectFromTitle;

            _protagonistSelectScreen = new ProtagonistSelectScreen();
            AddChild(_protagonistSelectScreen);
            _protagonistSelectScreen.Initialize();
            _protagonistSelectScreen.Confirmed += HandleProfileConfirmed;
            _protagonistSelectScreen.CancelRequested += HandleProfileSelectCancelled;

            _hubScreen = new BaseHubScreen();
            AddChild(_hubScreen);
            _hubScreen.Initialize();
            _hubScreen.StartDiveRequested += HandleHubStartDive;
            _hubScreen.ProfileEditRequested += ShowProtagonistSelectFromHub;
            _hubScreen.ReturnToTitleRequested += ShowTitleScreen;

            _resultScreen = new ResultScreen();
            _resultScreen.ContinueRequested += HandleResultContinue;
            AddChild(_resultScreen);
            _resultScreen.Initialize();

            _debtScreen = new DebtUI();
            AddChild(_debtScreen);
            _debtScreen.Initialize();
            _debtScreen.RepaymentSelected += HandleRepaymentSelected;

            _recoveryPoints = new RecoveryPointManager();
            AddChild(_recoveryPoints);

            _sonar = new SonarSystem();
            SetGameplayVisible(false, false);
        }

        private void StartDive(int diveCount)
        {
            HideNonGameplayScreens();
            SetGameplayVisible(true, true);
            _playerStats = new PlayerStats();
            _playerStats.ConfigureDive(diveCount, _saveData.Debt.CurrentDebt, _saveData.PlayerProfile.CurrentMoney);
            _pendingDiveResult = null;

            _chunks.Initialize();
            DigHelper.ExecuteDig(_chunks, DigHelper.GetCenteredArea(_playerStats.GridPosition, _playerStats.DigWidth));
            _recoveryPoints.Reset(_chunks, _playerStats.GridPosition.Y);
            _sonar.Reset();

            _playerController.Chunks = _chunks;
            _playerController.Stats = _playerStats;
            _playerController.VirtualPad = _virtualPad;
            _playerController.InputEnabled = true;
            _playerController.Reset();

            _camera.Target = _playerController;
            _camera.Enabled = true;
            _camera.SnapToTarget();

            _hud.Initialize(_playerStats, _playerController);
            _hud.SetDebugVisible(_debugVisible);
            _resultScreen.HideScreen();
            _debtScreen.HideScreen();

            _chunks.UpdateCamera(_playerStats.GridPosition.Y);
            UpdateHudState();
            UpdateDebugPresentation();
            _state = GameState.Playing;
        }

        private void HandleReturnRequested()
        {
            if (_state != GameState.Playing || _playerStats == null)
            {
                return;
            }

            if (_recoveryPoints.TryConsumeAt(_playerStats.GridPosition, _playerStats.PlayerSize, _chunks, out _))
            {
                EndDive(DiveEndReason.RecoveryPointReturn, string.Empty);
                return;
            }

            if (_playerStats.CanReturnFromSurface)
            {
                EndDive(DiveEndReason.SurfaceReturn, string.Empty);
            }
        }

        private void EndDive(DiveEndReason endReason, string rescueReason)
        {
            if (_state == GameState.Result)
            {
                return;
            }

            _state = GameState.Result;
            _playerController.InputEnabled = false;
            _playerController.CancelTouchInput();
            UpdateDebugPresentation();

            var result = _playerStats.FinalizeDive(endReason, rescueReason);
            _pendingDiveResult = result;
            ApplyResult(result);
            _hud.Visible = false;
            _resultScreen.Show(result);
            UpdateHudState();
        }

        private void HandleResultContinue()
        {
            if (_pendingDiveResult == null)
            {
                ShowHubScreen(string.Empty);
                return;
            }

            var preview = _debtManager.BuildPreview(_saveData.PlayerProfile.CurrentMoney, _saveData.Debt.CurrentDebt);
            _resultScreen.HideScreen();
            _debtScreen.ShowScreen(_pendingDiveResult, preview);
            _state = GameState.DebtRepay;
        }

        private void UpdateDebugPresentation()
        {
            if (_chunks == null || _playerController == null)
            {
                return;
            }

            _chunks.SetMoveDebugInfo(_playerController.MoveDebugInfo, _debugVisible && _state == GameState.Playing);
        }

        private void UpdateHudState()
        {
            if (_hud == null || _playerStats == null)
            {
                return;
            }

            var hasRecoveryReturn = _state == GameState.Playing && _recoveryPoints.IsReturnPointAvailable(_playerStats.GridPosition, _playerStats.PlayerSize);
            var hasSurfaceReturn = _state == GameState.Playing && _playerStats.CanReturnFromSurface;
            var canReturn = hasRecoveryReturn || hasSurfaceReturn;
            var returnLabel = hasRecoveryReturn ? "回収ポイント帰還" : "地上帰還";

            var returnStatus = _state switch
            {
                GameState.Result => "潜行終了: 結果を確認してください",
                _ when hasRecoveryReturn => "回収ポイントを確保: ボタンで退避できます",
                _ when hasSurfaceReturn => "地上圏へ帰還: ボタンで報酬確保できます",
                _ when _playerStats.Phase == DivePhase.Critical => "危険域: 早めの帰還を推奨",
                _ => "潜行継続中"
            };

            _hud.SetDiveContext(_sonar.CurrentReading.GetDisplayText(), canReturn, returnLabel, returnStatus, _saveData.Settings.VirtualPadOpacity);
        }

        private void AdjustVirtualPadOpacity(float delta)
        {
            if (_saveData == null || _virtualPad == null)
            {
                return;
            }

            _saveData.Settings.VirtualPadOpacity = Mathf.Clamp(_saveData.Settings.VirtualPadOpacity + delta, 0.10f, 0.85f);
            _virtualPad.ApplySettings(CreateVirtualPadSettings());
            _saveManager.Save(_saveData);
            UpdateHudState();
        }

        private VirtualPadSettings CreateVirtualPadSettings()
        {
            var opacity = _saveData?.Settings.VirtualPadOpacity ?? 0.28f;
            return new VirtualPadSettings
            {
                BaseOpacity = opacity,
                KnobOpacity = Mathf.Clamp(opacity + 0.24f, 0f, 1f),
                DeadZoneRadius = 24f,
                MaxRadius = 72f,
                BaseRadius = 84f,
                KnobRadius = 34f
            };
        }

        private void ApplyResult(DiveResultData result)
        {
            _saveData.PlayerProfile.TotalDiveCount = result.DiveCount;
            _saveData.PlayerProfile.CurrentMoney = result.MoneyAfter;
            _saveData.Debt.CurrentDebt = result.DebtAfter;
            _saveData.Debt.DebtCleared = result.DebtAfter <= 0L;
            _saveData.Debt.TotalRescueCost += result.RescueCost;
            _saveData.Ranking.BestDepth = Math.Max(_saveData.Ranking.BestDepth, result.MaxDepthMeters);
            _saveData.Ranking.BestSingleProfit = Math.Max(_saveData.Ranking.BestSingleProfit, result.CarryValue - result.RescueCost);
            _saveData.Ranking.TotalMoneyEarned += result.CarryValue;
            _saveData.Meta.UpdatedAt = DateTimeOffset.UtcNow;
            _saveManager.Save(_saveData);
        }

        private void HandleTitlePrimaryRequested()
        {
            if (_saveData.PlayerProfile.IsProfileConfigured)
            {
                ShowHubScreen(string.Empty);
                return;
            }

            ShowProtagonistSelectScreen(false);
        }

        private void ShowProtagonistSelectFromTitle()
        {
            ShowProtagonistSelectScreen(false);
        }

        private void ShowProtagonistSelectFromHub()
        {
            ShowProtagonistSelectScreen(true);
        }

        private void ShowProtagonistSelectScreen(bool returnToHub)
        {
            SetGameplayVisible(false, false);
            _titleScreen.HideScreen();
            _hubScreen.HideScreen();
            _resultScreen.HideScreen();
            _debtScreen.HideScreen();
            _protagonistSelectScreen.ShowScreen(_saveData.PlayerProfile, returnToHub);
            _state = GameState.ProtagonistSelect;
        }

        private void HandleProfileConfirmed(string gender, string name)
        {
            _saveData.PlayerProfile.Gender = gender;
            _saveData.PlayerProfile.Name = name;
            _saveData.PlayerProfile.IsProfileConfigured = true;
            _saveData.Meta.UpdatedAt = DateTimeOffset.UtcNow;
            _saveManager.Save(_saveData);
            ShowHubScreen("主人公プロフィールを更新しました。");
        }

        private void HandleProfileSelectCancelled(bool returnToHub)
        {
            if (returnToHub && _saveData.PlayerProfile.IsProfileConfigured)
            {
                ShowHubScreen(string.Empty);
                return;
            }

            ShowTitleScreen();
        }

        private void HandleHubStartDive()
        {
            StartDive(Mathf.Max(1, _saveData.PlayerProfile.TotalDiveCount + 1));
        }

        private void HandleRepaymentSelected(DebtRepaymentChoice choice)
        {
            if (_pendingDiveResult == null)
            {
                ShowHubScreen(string.Empty);
                return;
            }

            var settlement = _debtManager.ApplyRepayment(_saveData, choice);
            _saveData.Debt.DebtCleared = settlement.DebtAfter <= 0L;
            if (_saveData.Debt.DebtCleared)
            {
                if (_saveData.Ranking.FastestDebtClear == 0 || _saveData.PlayerProfile.TotalDiveCount < _saveData.Ranking.FastestDebtClear)
                {
                    _saveData.Ranking.FastestDebtClear = _saveData.PlayerProfile.TotalDiveCount;
                }
            }

            _saveData.Records.DiveRecords.Add(DiveRecordData.FromResult(_pendingDiveResult, settlement));
            _saveData.Meta.UpdatedAt = DateTimeOffset.UtcNow;
            _saveManager.Save(_saveData);
            _pendingDiveResult = null;
            _debtScreen.HideScreen();
            ShowHubScreen(settlement.BuildSummaryText());
        }

        private void ShowTitleScreen()
        {
            SetGameplayVisible(false, false);
            HideNonGameplayScreens();
            _titleScreen.ShowScreen(_saveData.PlayerProfile, _saveData.Debt, _saveData.Ranking);
            _state = GameState.Title;
        }

        private void ShowHubScreen(string message)
        {
            SetGameplayVisible(false, false);
            HideNonGameplayScreens();
            _hubScreen.ShowScreen(_saveData, message);
            _state = GameState.Hub;
        }

        private void HideNonGameplayScreens()
        {
            _titleScreen?.HideScreen();
            _protagonistSelectScreen?.HideScreen();
            _hubScreen?.HideScreen();
            _resultScreen?.HideScreen();
            _debtScreen?.HideScreen();
        }

        private void SetGameplayVisible(bool worldVisible, bool hudVisible)
        {
            if (_chunks != null)
            {
                _chunks.Visible = worldVisible;
            }

            if (_playerController != null)
            {
                _playerController.Visible = worldVisible;
                _playerController.InputEnabled = worldVisible && _state == GameState.Playing;
                if (!worldVisible)
                {
                    _playerController.CancelTouchInput();
                }
            }

            if (_camera != null)
            {
                _camera.Enabled = worldVisible;
            }

            if (_hud != null)
            {
                _hud.Visible = hudVisible;
            }
        }
    }
}