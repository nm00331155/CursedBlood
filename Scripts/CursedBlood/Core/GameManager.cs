using CursedBlood.Camera;
using CursedBlood.Player;
using CursedBlood.UI;
using Godot;

namespace CursedBlood.Core
{
    public partial class GameManager : Node2D
    {
        private enum GameState
        {
            Playing,
            Dead
        }

        private ChunkManager _chunks;
        private PlayerController _playerController;
        private PlayerStats _playerStats;
        private GameCamera _camera;
        private HUDManager _hud;
        private DeathScreen _deathScreen;
        private GameState _state;
        private int _currentGeneration = 1;

        public override void _Ready()
        {
            SetProcess(true);
            BuildSceneGraph();
            StartGeneration(_currentGeneration);
        }

        public override void _Process(double delta)
        {
            if (_state != GameState.Playing || _playerStats == null)
            {
                return;
            }

            _playerStats.AdvanceTime((float)delta);
            if (_playerStats.UpdatePhaseState())
            {
                DigHelper.ExecuteDig(_chunks, DigHelper.GetCenteredArea(_playerStats.GridPosition, _playerStats.PlayerSize));
                _playerController.SyncToStatsPosition();
            }

            _chunks.UpdateCamera(_playerStats.GridPosition.Y);

            if (!_playerStats.IsAlive)
            {
                OnPlayerDeath();
            }
        }

        public override void _ExitTree()
        {
            if (_deathScreen != null)
            {
                _deathScreen.RestartRequested -= RestartGeneration;
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

            _deathScreen = new DeathScreen();
            _deathScreen.RestartRequested += RestartGeneration;
            AddChild(_deathScreen);
            _deathScreen.Initialize();
        }

        private void StartGeneration(int generation)
        {
            _playerStats = new PlayerStats
            {
                Generation = generation
            };
            _playerStats.Reset();

            _chunks.Initialize();
            DigHelper.ExecuteDig(_chunks, DigHelper.GetCenteredArea(_playerStats.GridPosition, _playerStats.DigWidth));

            _playerController.Chunks = _chunks;
            _playerController.Stats = _playerStats;
            _playerController.InputEnabled = true;
            _playerController.Reset();

            _camera.Target = _playerController;
            _camera.SnapToTarget();

            _hud.Initialize(_playerStats);
            _deathScreen.HideScreen();

            _chunks.UpdateCamera(_playerStats.GridPosition.Y);
            _state = GameState.Playing;
        }

        private void OnPlayerDeath()
        {
            if (_state == GameState.Dead)
            {
                return;
            }

            _state = GameState.Dead;
            _playerController.InputEnabled = false;
            var cause = _playerStats.CurrentAge >= _playerStats.MaxLifespan ? "寿命が尽きた" : "HPが尽きた";
            _deathScreen.Show(_playerStats, cause);
        }

        private void RestartGeneration()
        {
            _currentGeneration++;
            StartGeneration(_currentGeneration);
        }
    }
}