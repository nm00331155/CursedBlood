using System;
using CursedBlood.Achievement;
using CursedBlood.Curse;
using CursedBlood.Generation;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.UI
{
    public partial class EndingUI : CanvasLayer
    {
        private bool _uiBuilt;
        private ColorRect _overlay;
        private Panel _panel;
        private Label _contentLabel;

        public event Action ContinueRequested;

        public void ShowSummary(FamilyTree familyTree, PlayerStats stats, CurseResearchManager curseResearch, RankingBoard rankingBoard)
        {
            BuildUiIfNeeded();
            _contentLabel.Text =
                "呪いは解かれた。しかし地底にはまだ秘密が眠っている…\n\n" +
                $"総世代数: {familyTree.Records.Count}\n" +
                $"最終世代: {stats.CharacterName}\n" +
                $"総研究度: {curseResearch.TotalPoints}\n" +
                $"最高深度: {rankingBoard.BestDepth}\n" +
                $"最高スコア: {rankingBoard.BestScore:N0}";
            _overlay.Visible = true;
            _panel.Visible = true;
        }

        public void HidePanel()
        {
            if (!_uiBuilt)
            {
                return;
            }

            _overlay.Visible = false;
            _panel.Visible = false;
        }

        private void BuildUiIfNeeded()
        {
            if (_uiBuilt)
            {
                return;
            }

            _overlay = new ColorRect
            {
                Position = Vector2.Zero,
                Size = new Vector2(1080f, 1920f),
                Color = new Color(0f, 0f, 0f, 0.78f),
                Visible = false
            };
            AddChild(_overlay);

            _panel = new Panel
            {
                Position = new Vector2(120f, 260f),
                Size = new Vector2(840f, 980f),
                Visible = false
            };
            AddChild(_panel);

            _contentLabel = new Label
            {
                Position = new Vector2(36f, 48f),
                Size = new Vector2(768f, 760f),
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            _contentLabel.AddThemeFontSizeOverride("font_size", 28);
            _panel.AddChild(_contentLabel);

            var button = new Button
            {
                Position = new Vector2(260f, 840f),
                Size = new Vector2(320f, 60f),
                Text = "続ける"
            };
            button.Pressed += () =>
            {
                HidePanel();
                ContinueRequested?.Invoke();
            };
            _panel.AddChild(button);

            _uiBuilt = true;
        }
    }
}