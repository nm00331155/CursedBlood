using Godot;

namespace CursedBlood.Enemy
{
    public partial class BossUI : CanvasLayer
    {
        private Label _nameLabel;
        private ProgressBar _progressBar;

        public override void _Ready()
        {
            _nameLabel = new Label
            {
                Position = new Vector2(120f, 220f),
                Size = new Vector2(840f, 36f),
                HorizontalAlignment = HorizontalAlignment.Center,
                Visible = false
            };
            _nameLabel.AddThemeFontSizeOverride("font_size", 24);
            AddChild(_nameLabel);

            _progressBar = new ProgressBar
            {
                Position = new Vector2(140f, 260f),
                Size = new Vector2(800f, 24f),
                Visible = false,
                MaxValue = 1f
            };
            AddChild(_progressBar);
        }

        public void UpdateBoss(BossData boss)
        {
            if (boss == null || boss.IsDefeated)
            {
                SetVisibleState(false);
                return;
            }

            SetVisibleState(true);
            _nameLabel.Text = boss.IsDemonLord ? "魔王" : $"深度{boss.Depth}の守護者";
            _progressBar.Value = boss.HealthRatio;
        }

        private void SetVisibleState(bool visible)
        {
            _nameLabel.Visible = visible;
            _progressBar.Visible = visible;
        }
    }
}