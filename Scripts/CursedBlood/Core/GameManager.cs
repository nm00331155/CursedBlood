using System.Linq;
using CursedBlood.Achievement;
using CursedBlood.Audio;
using CursedBlood.Camera;
using CursedBlood.Config;
using CursedBlood.Curse;
using CursedBlood.Debt;
using CursedBlood.Enemy;
using CursedBlood.Equipment;
using CursedBlood.Effects;
using CursedBlood.Generation;
using CursedBlood.Player;
using CursedBlood.Skill;
using CursedBlood.UI;
using Godot;

namespace CursedBlood.Core
{
    public partial class GameManager : Node2D
    {
        private enum GameState
        {
            Playing,
            Dead,
            Paused,
            Ending
        }

        private GameState _state = GameState.Playing;
        private GridManager _gridManager;
        private PlayerController _playerController;
        private PlayerStats _playerStats;
        private HUDManager _hud;
        private DeathScreen _deathScreen;
        private GameCamera _camera;
        private ThemeSettings _themeSettings;
        private BalanceConfig _balanceConfig;
        private GridGenerationContext _gridGenerationContext;
        private BulletManager _bulletManager;
        private EnemyManager _enemyManager;
        private BossController _bossController;
        private BossUI _bossUi;
        private EquipmentUI _equipmentUi;
        private FamilyTreeUI _familyTreeUi;
        private AchievementUI _achievementUi;
        private AchievementPopup _achievementPopup;
        private RankingUI _rankingUi;
        private CurseResearchUI _curseResearchUi;
        private PauseMenu _pauseMenu;
        private SettingsUI _settingsUi;
        private TutorialOverlay _tutorialOverlay;
        private EndingUI _endingUi;
        private ScreenEffects _screenEffects;
        private ParticleManager _particleManager;
        private AudioManager _audioManager;
        private SkillManager _skillManager;
        private DebtManager _debtManager;
        private GenerationManager _generationManager;
        private FamilyTree _familyTree;
        private AchievementManager _achievementManager;
        private RankingBoard _rankingBoard;
        private CurseResearchManager _curseResearchManager;
        private string _lastDeathCause = string.Empty;

        public override void _Ready()
        {
            GD.Randomize();
            LoadPersistentState();
            BuildSceneGraph();
            StartGeneration(null);
        }

        public override void _Process(double delta)
        {
            if (_state != GameState.Playing)
            {
                return;
            }

            _playerStats.AdvanceTime((float)delta);
            _curseResearchManager.UpdateFromPlay(_playerStats, (float)delta);
            _curseResearchManager.RegisterDepth(_playerStats.MaxDepth);
            _bossUi.UpdateBoss(_bossController.ActiveBoss);
            _screenEffects.SetCurseOverlay(_playerStats.Phase == LifePhase.Twilight
                ? Mathf.Clamp((_playerStats.CurrentAge - 45f) / 15f, 0f, 1f) * 0.22f
                : 0f);

            if (!_playerStats.IsAlive)
            {
                OnPlayerDeath();
            }
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
            {
                return;
            }

            if (_deathScreen.IsVisibleScreen || _state == GameState.Ending)
            {
                return;
            }

            switch (keyEvent.Keycode)
            {
                case Key.Tab:
                    OnEquipmentRequested();
                    break;
                case Key.F:
                    OnFamilyTreeRequested();
                    break;
                case Key.A:
                    OnAchievementsRequested();
                    break;
                case Key.R:
                    OnRankingRequested();
                    break;
                case Key.C:
                    OnCurseRequested();
                    break;
                case Key.Escape:
                    OnPauseRequested();
                    break;
            }
        }

        public override void _ExitTree()
        {
            SavePersistentState();

            if (_hud != null)
            {
                _hud.ThemeChanged -= OnThemeChanged;
                _hud.EquipmentRequested -= OnEquipmentRequested;
                _hud.FamilyTreeRequested -= OnFamilyTreeRequested;
                _hud.AchievementsRequested -= OnAchievementsRequested;
                _hud.RankingRequested -= OnRankingRequested;
                _hud.CurseRequested -= OnCurseRequested;
                _hud.PauseRequested -= OnPauseRequested;
            }

            if (_bulletManager != null)
            {
                _bulletManager.BulletGuarded -= OnBulletGuarded;
            }

            if (_deathScreen != null)
            {
                _deathScreen.ContinueRequested -= OnContinueRequested;
            }

            if (_enemyManager != null)
            {
                _enemyManager.EnemyDefeated -= OnEnemyDefeated;
            }

            if (_bossController != null)
            {
                _bossController.BossDefeated -= OnBossDefeated;
            }

            if (_pauseMenu != null)
            {
                _pauseMenu.ResumeRequested -= OnPauseResumeRequested;
                _pauseMenu.SettingsRequested -= OnPauseSettingsRequested;
                _pauseMenu.TitleRequested -= OnPauseTitleRequested;
            }

            if (_endingUi != null)
            {
                _endingUi.ContinueRequested -= OnEndingContinueRequested;
            }
        }

        private void LoadPersistentState()
        {
            _themeSettings = ThemeSettingsStore.Load();
            _balanceConfig = BalanceConfig.Load();
            _generationManager = new GenerationManager();
            _familyTree = FamilyTree.Load();
            _debtManager = DebtManager.Load();
            _achievementManager = AchievementManager.Load();
            _rankingBoard = RankingBoard.Load();
            _curseResearchManager = CurseResearchManager.Load();
        }

        private void SavePersistentState()
        {
            ThemeSettingsStore.Save(_themeSettings);
            _balanceConfig.Save();
            _familyTree.Save();
            _debtManager.Save();
            _achievementManager.Save();
            _rankingBoard.Save();
            _curseResearchManager.Save();
        }

        private void BuildSceneGraph()
        {
            _screenEffects = new ScreenEffects();
            AddChild(_screenEffects);

            _gridManager = new GridManager();
            AddChild(_gridManager);

            _bulletManager = new BulletManager();
            _bulletManager.BulletGuarded += OnBulletGuarded;
            AddChild(_bulletManager);

            _enemyManager = new EnemyManager();
            _enemyManager.EnemyDefeated += OnEnemyDefeated;
            AddChild(_enemyManager);

            _bossController = new BossController();
            _bossController.BossDefeated += OnBossDefeated;
            AddChild(_bossController);

            _playerController = new PlayerController();
            _playerController.CellEntered = OnPlayerCellEntered;
            _playerController.SkillRequested = OnSkillRequested;
            _playerController.BossAttackRequested = OnBossAttackRequested;
            AddChild(_playerController);

            _particleManager = new ParticleManager();
            AddChild(_particleManager);

            _camera = new GameCamera();
            AddChild(_camera);

            _audioManager = new AudioManager();
            AddChild(_audioManager);

            _skillManager = new SkillManager();
            AddChild(_skillManager);

            _hud = new HUDManager();
            _hud.ThemeChanged += OnThemeChanged;
            _hud.EquipmentRequested += OnEquipmentRequested;
            _hud.FamilyTreeRequested += OnFamilyTreeRequested;
            _hud.AchievementsRequested += OnAchievementsRequested;
            _hud.RankingRequested += OnRankingRequested;
            _hud.CurseRequested += OnCurseRequested;
            _hud.PauseRequested += OnPauseRequested;
            AddChild(_hud);

            _equipmentUi = new EquipmentUI();
            _equipmentUi.Closed += RefreshPauseState;
            AddChild(_equipmentUi);

            _familyTreeUi = new FamilyTreeUI();
            AddChild(_familyTreeUi);

            _achievementUi = new AchievementUI();
            AddChild(_achievementUi);

            _achievementPopup = new AchievementPopup();
            AddChild(_achievementPopup);

            _rankingUi = new RankingUI();
            AddChild(_rankingUi);

            _curseResearchUi = new CurseResearchUI();
            AddChild(_curseResearchUi);

            _pauseMenu = new PauseMenu();
            _pauseMenu.ResumeRequested += OnPauseResumeRequested;
            _pauseMenu.SettingsRequested += OnPauseSettingsRequested;
            _pauseMenu.TitleRequested += OnPauseTitleRequested;
            AddChild(_pauseMenu);

            _settingsUi = new SettingsUI();
            AddChild(_settingsUi);

            _tutorialOverlay = new TutorialOverlay();
            AddChild(_tutorialOverlay);

            _bossUi = new BossUI();
            AddChild(_bossUi);

            _deathScreen = new DeathScreen();
            _deathScreen.ContinueRequested += OnContinueRequested;
            AddChild(_deathScreen);
            _deathScreen.Initialize();

            _endingUi = new EndingUI();
            _endingUi.ContinueRequested += OnEndingContinueRequested;
            AddChild(_endingUi);
        }

        private void StartGeneration(InheritanceData inheritanceData)
        {
            inheritanceData ??= CreateFreshGenerationData();

            _playerStats ??= new PlayerStats();
            _playerStats.ApplyRunSetup(
                _balanceConfig,
                inheritanceData.Generation,
                inheritanceData.CharacterName,
                inheritanceData.IsMale,
                inheritanceData.Gold,
                inheritanceData.Heirloom,
                _curseResearchManager.BonusSeconds,
                _debtManager.LiberationBonusActive,
                _achievementManager.GetBonuses());

            _gridGenerationContext = new GridGenerationContext
            {
                BalanceConfig = _balanceConfig,
                CollectorSpawnMultiplier = _debtManager.GetCollectorSpawnRate(),
                EnableBosses = true,
                EnableDemonLord = true
            };

            _gridManager.Configure(_gridGenerationContext);
            _gridManager.Reset();
            _gridManager.UpdateVisibleRange(_playerStats.GridPosition.Y);

            _playerController.Grid = _gridManager;
            _playerController.Stats = _playerStats;
            _playerController.Reset();

            _bulletManager.Grid = _gridManager;
            _bulletManager.Stats = _playerStats;
            _bulletManager.CellDuration = _balanceConfig.BulletCellDuration;
            _bulletManager.IsPlayerGuarding = () => _playerController.IsGuarding;
            _bulletManager.ClearAll();

            _enemyManager.Grid = _gridManager;
            _enemyManager.Stats = _playerStats;
            _enemyManager.BulletManager = _bulletManager;

            _bossController.Grid = _gridManager;
            _bossController.Stats = _playerStats;
            _bossController.BulletManager = _bulletManager;
            _bossController.BalanceConfig = _balanceConfig;
            _bossController.Reset();

            _skillManager.Grid = _gridManager;
            _skillManager.Stats = _playerStats;
            _skillManager.EnemyManager = _enemyManager;
            _skillManager.BossController = _bossController;
            _skillManager.ScreenEffects = _screenEffects;
            _skillManager.ParticleManager = _particleManager;

            _equipmentUi.Initialize(_playerStats);
            _familyTreeUi.SetFamilyTree(_familyTree);
            _achievementUi.SetData(_achievementManager);
            _rankingUi.SetData(_rankingBoard);
            _curseResearchUi.SetData(_curseResearchManager);
            _settingsUi.Initialize(_audioManager);
            _hud.Initialize(_playerStats, _themeSettings, () => _debtManager.RemainingDebt, () => _curseResearchManager.TotalPoints, () => _achievementManager.GetUnlockedCount());
            _hud.ShowNotification($"第{_playerStats.Generation}世代が始まる");

            _deathScreen.HideScreen();
            _pauseMenu.HideMenu();
            _settingsUi.HidePanel();
            _endingUi.HidePanel();
            _bossUi.UpdateBoss(null);
            _tutorialOverlay.ShowIfNeeded();

            ApplyTheme(_themeSettings.BuildTheme());
            SetSimulationPaused(false);
            _state = GameState.Playing;
            _audioManager.PlayBgm("play");
        }

        private InheritanceData CreateFreshGenerationData()
        {
            var isMale = GD.Randf() >= 0.5f;
            return new InheritanceData
            {
                Generation = _familyTree.Records.Count + 1,
                CharacterName = NameGenerator.Generate(isMale),
                IsMale = isMale,
                Gold = 0
            };
        }

        private void ApplyTheme(GameTheme theme)
        {
            _gridManager.ApplyTheme(theme);
            _playerController.ApplyTheme(theme);
            _hud.ApplyTheme(theme);
            _deathScreen.ApplyTheme(theme);
        }

        private void SetSimulationPaused(bool paused)
        {
            _playerController.InputEnabled = !paused;
            _bulletManager.SimulationEnabled = !paused;
            _enemyManager.SimulationEnabled = !paused;
            _bossController.SimulationEnabled = !paused;
        }

        private void RefreshPauseState()
        {
            var shouldPause = _equipmentUi.IsOpen || _familyTreeUi.IsOpen || _achievementUi.IsOpen || _rankingUi.IsOpen || _curseResearchUi.IsOpen || _pauseMenu.IsOpen || _settingsUi.IsOpen || _deathScreen.IsVisibleScreen || _state == GameState.Ending;
            SetSimulationPaused(shouldPause);
            if (_deathScreen.IsVisibleScreen)
            {
                _state = GameState.Dead;
            }
            else if (_state != GameState.Ending)
            {
                _state = shouldPause ? GameState.Paused : GameState.Playing;
            }
        }

        private void OnPlayerCellEntered(CellData cell, bool wasSolid)
        {
            if (cell == null)
            {
                return;
            }

            if (wasSolid && (cell.Type == CellType.Normal || cell.Type == CellType.Hard))
            {
                _playerStats.RegisterDig(_balanceConfig.SkillChargePerDig);
                _achievementManager.IncrementCounter(CounterType.HardBlocksBroken);
                var digColor = cell.Type == CellType.Hard ? _themeSettings.BuildTheme().HardCellColor : _themeSettings.BuildTheme().NormalCellColor;
                _particleManager.SpawnDigParticle(_gridManager.GridToWorld(cell.GridPosition.X, cell.GridPosition.Y), digColor);
                _audioManager.PlaySe("dig");
            }

            if (cell.Type == CellType.Ore)
            {
                var oreValue = cell.OreValue;
                _achievementManager.IncrementCounter(CounterType.OresBroken);
                _playerStats.AddGold(oreValue);
                _playerStats.RegisterDig(_balanceConfig.SkillChargePerDig);
                cell.ClearOre();
                _hud.ShowNotification($"鉱石 +{oreValue:N0}G");
                _particleManager.SpawnItemDropParticle(_gridManager.GridToWorld(cell.GridPosition.X, cell.GridPosition.Y), new Color(1f, 0.85f, 0.2f));
                TryDropEquipment(cell.GridPosition, cell.GridPosition.Y, false);
            }

            if (cell.HasEnemy)
            {
                _enemyManager.ResolveEnemyEncounter(cell);
            }

            if (cell.HasDrop)
            {
                PickupItem(cell.TakeDroppedItem());
            }

            _curseResearchManager.RegisterDepth(_playerStats.MaxDepth);
            RefreshMetaUi();
            _playerStats.RefreshDerivedStats();
            _gridManager.QueueRefresh();
        }

        private void OnEnemyDefeated(CellData cell, EnemyData enemy)
        {
            var goldReward = enemy.IsDebtCollector ? 20 + cell.GridPosition.Y : 8 + cell.GridPosition.Y / 2;
            _achievementManager.IncrementCounter(CounterType.EnemiesKilled);
            _playerStats.RegisterEnemyKill(_balanceConfig.SkillChargePerKill);
            _playerStats.AddGold(goldReward);
            _hud.ShowNotification($"敵撃破 +{goldReward:N0}G");
            _particleManager.SpawnEnemyDeathParticle(
                _gridManager.GridToWorld(cell.GridPosition.X, cell.GridPosition.Y),
                enemy.GetColor(GridGenerator.GetDepthTier(cell.GridPosition.Y)));
            _audioManager.PlaySe("kill");

            TryDropEquipment(cell.GridPosition, cell.GridPosition.Y, false, enemy.IsDebtCollector);

            if (cell.HasDrop)
            {
                PickupItem(cell.TakeDroppedItem());
            }

            RefreshMetaUi();
        }

        private void PickupItem(DroppedItem droppedItem)
        {
            if (droppedItem?.Item == null)
            {
                return;
            }

            var added = _playerStats.Inventory.TryAddToBag(droppedItem.Item);
            EquipmentData removed = null;
            if (!added)
            {
                removed = _playerStats.Inventory.ReplaceLowest(droppedItem.Item);
            }

            _achievementManager.IncrementCounter(CounterType.EquipmentFound);
            switch (droppedItem.Item.Rarity)
            {
                case Rarity.Rare:
                case Rarity.Epic:
                case Rarity.Legendary:
                    _achievementManager.IncrementCounter(CounterType.RarePlusFound);
                    break;
            }

            if (droppedItem.Item.Rarity == Rarity.Legendary)
            {
                _achievementManager.IncrementCounter(CounterType.LegendaryFound);
            }

            if (droppedItem.Item.Rarity == Rarity.Cursed)
            {
                _achievementManager.IncrementCounter(CounterType.CursedFound);
            }

            _playerStats.RefreshDerivedStats();
            _equipmentUi.NotifyReplacement(droppedItem.Item, removed);
            _hud.ShowNotification(added
                ? $"{droppedItem.Item.Name} 入手"
                : $"{droppedItem.Item.Name} 入手 / {removed?.Name} 破棄");
            _particleManager.SpawnItemDropParticle(_gridManager.GridToWorld(droppedItem.GridPosition.X, droppedItem.GridPosition.Y), droppedItem.Item.GetRarityColor());
            if (droppedItem.Item.Rarity >= Rarity.Legendary)
            {
                _screenEffects.Flash(droppedItem.Item.GetRarityColor(), 0.25f);
            }

            _audioManager.PlaySe("drop");
        }

        private void TryDropEquipment(Vector2I position, int depth, bool bossDrop, bool forceDrop = false)
        {
            var cell = _gridManager.GetCell(position.X, position.Y);
            if (cell == null || cell.HasDrop)
            {
                return;
            }

            if (!bossDrop && !forceDrop && GD.Randf() >= _balanceConfig.EquipmentDropChance + _playerStats.EffectiveDropRateBonus)
            {
                return;
            }

            var item = bossDrop
                ? EquipmentGenerator.GenerateBossDrop(depth, _balanceConfig)
                : EquipmentGenerator.Generate(depth, _balanceConfig);

            cell.SetDroppedItem(new DroppedItem
            {
                Item = item,
                GridPosition = position
            });
            _particleManager.SpawnItemDropParticle(_gridManager.GridToWorld(position.X, position.Y), item.GetRarityColor());
            _gridManager.QueueRefresh();
        }

        private bool OnBossAttackRequested(Vector2I playerPosition, Vector2I direction)
        {
            var attacked = _bossController.TryAttack(playerPosition, direction);
            if (!attacked)
            {
                return false;
            }

            _screenEffects.Flash(new Color(1f, 0.8f, 0.4f), 0.12f);
            _playerStats.RegisterDig(_balanceConfig.SkillChargePerDig);
            _particleManager.SpawnDigParticle(_gridManager.GridToWorld(playerPosition.X + direction.X, playerPosition.Y + direction.Y), new Color(1f, 0.4f, 0.2f));
            return true;
        }

        private void OnBossDefeated(BossData boss)
        {
            _playerStats.RegisterBossKill();
            _curseResearchManager.RegisterBossKill();
            _camera.Shake(8f, 0.25f);
            _screenEffects.Flash(new Color(1f, 0.4f, 0.2f), 0.3f);
            _particleManager.SpawnBossExplosion(_gridManager.GridToWorld(boss.CenterPosition.X, boss.CenterPosition.Y));
            _audioManager.PlaySe("kill");
            TryDropEquipment(boss.CenterPosition, boss.Depth, true);
            _bossUi.UpdateBoss(null);
            RefreshMetaUi();

            if (boss.IsDemonLord)
            {
                _curseResearchManager.EndingCleared = true;
                _curseResearchManager.Save();
                _state = GameState.Ending;
                SetSimulationPaused(true);
                _endingUi.ShowSummary(_familyTree, _playerStats, _curseResearchManager, _rankingBoard);
                return;
            }

            RefreshMetaUi();
        }

        private void OnPlayerDeath()
        {
            if (_state == GameState.Dead)
            {
                return;
            }

            _state = GameState.Dead;
            SetSimulationPaused(true);
            _lastDeathCause = _playerStats.CurrentAge >= _playerStats.MaxLifespan
                ? "寿命が尽きた - 呪われた血脈の定め"
                : "力尽きた - 地底の闇に飲まれた";
            _screenEffects.TriggerDeathFlash();
            _audioManager.PlaySe("kill");
            _deathScreen.Show(_playerStats, _lastDeathCause, _debtManager.GetPaymentOptions(_playerStats.Gold));
        }

        private void OnContinueRequested(DebtPaymentKind debtPaymentKind, EquipmentData heirloom)
        {
            var option = _debtManager.GetPaymentOptions(_playerStats.Gold).FirstOrDefault(candidate => candidate.Kind == debtPaymentKind) ?? new DebtPaymentOption
            {
                Kind = DebtPaymentKind.None,
                Amount = 0,
                IsAvailable = true
            };

            var debtBefore = _debtManager.RemainingDebt;
            var repayment = option.IsAvailable ? option.Amount : 0L;
            _playerStats.LastRepaymentAmount = repayment;
            _playerStats.LastRepaymentRate = debtBefore <= 0 ? 0f : repayment / (float)debtBefore;
            _playerStats.SpendGold(repayment);
            _debtManager.Repay(repayment);

            var inheritance = _generationManager.ProcessInheritance(_playerStats, heirloom, _playerStats.Gold);
            _achievementManager.IncrementCounter(CounterType.GoldInherited, inheritance.Gold);
            var record = _generationManager.CreateRecord(_playerStats, _lastDeathCause, repayment, inheritance.Gold);
            _familyTree.AddRecord(record);
            _rankingBoard.RegisterRun(_playerStats);
            ApplyAchievementUnlocks();

            _debtManager.ApplyInterest();
            SavePersistentState();

            _deathScreen.HideScreen();
            StartGeneration(inheritance);
        }

        private void OnThemeChanged(ThemeSettings settings)
        {
            _themeSettings = settings;
            ThemeSettingsStore.Save(settings);
            ApplyTheme(settings.BuildTheme());
        }

        private void OnEquipmentRequested()
        {
            _equipmentUi.Toggle();
            RefreshPauseState();
        }

        private void OnFamilyTreeRequested()
        {
            _familyTreeUi.SetFamilyTree(_familyTree);
            _familyTreeUi.Toggle();
            RefreshPauseState();
        }

        private void OnAchievementsRequested()
        {
            _achievementUi.SetData(_achievementManager);
            _achievementUi.Toggle();
            RefreshPauseState();
        }

        private void OnRankingRequested()
        {
            _rankingUi.SetData(_rankingBoard);
            _rankingUi.Toggle();
            RefreshPauseState();
        }

        private void OnCurseRequested()
        {
            _curseResearchUi.SetData(_curseResearchManager);
            _curseResearchUi.Toggle();
            RefreshPauseState();
        }

        private void OnPauseRequested()
        {
            _pauseMenu.Toggle();
            RefreshPauseState();
        }

        private void OnPauseResumeRequested()
        {
            RefreshPauseState();
        }

        private void OnPauseSettingsRequested()
        {
            _settingsUi.Toggle();
            RefreshPauseState();
        }

        private void OnPauseTitleRequested()
        {
            SavePersistentState();
            GetTree().ChangeSceneToFile("res://Scenes/CursedBlood/TitleScene.tscn");
        }

        private void OnEndingContinueRequested()
        {
            SavePersistentState();
            GetTree().ChangeSceneToFile("res://Scenes/CursedBlood/TitleScene.tscn");
        }

        private void OnSkillRequested(Vector2I direction)
        {
            if (_state != GameState.Playing)
            {
                return;
            }

            if (!_skillManager.TryActivate(direction))
            {
                return;
            }

            _audioManager.PlaySe("skill");
            _hud.ShowNotification($"{SkillData.FromType(_playerStats.CurrentSkillType).Name} 発動");
            RefreshMetaUi();
        }

        private void RefreshMetaUi()
        {
            ApplyAchievementUnlocks();
            _familyTreeUi.SetFamilyTree(_familyTree);
            _achievementUi.SetData(_achievementManager);
            _rankingUi.SetData(_rankingBoard);
            _curseResearchUi.SetData(_curseResearchManager);
            _hud.UpdateSources(() => _debtManager.RemainingDebt, () => _curseResearchManager.TotalPoints, () => _achievementManager.GetUnlockedCount());
        }

        private void OnBulletGuarded()
        {
            _achievementManager.IncrementCounter(CounterType.GuardBlocks);
            RefreshMetaUi();
        }

        private void ApplyAchievementUnlocks()
        {
            var unlockedEntries = _achievementManager.CheckAndUnlock(_playerStats, _familyTree, _debtManager, _curseResearchManager);
            foreach (var entry in unlockedEntries)
            {
                _achievementPopup.QueueUnlock(entry);
                _hud.ShowNotification($"実績解除: {entry.Title}");
            }
        }
    }
}