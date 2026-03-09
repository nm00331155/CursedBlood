using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CursedBlood.Achievement
{
    public partial class RankingUI : CanvasLayer
    {
        private enum RankingViewMode
        {
            Depth,
            Score,
            Speed
        }

        private RankingViewMode _currentViewMode = RankingViewMode.Depth;
        private RankingBoard _rankingBoard;
        private bool _uiBuilt;
        private ColorRect _overlay;
        private Panel _panel;
        private Label _contentLabel;
        private readonly Dictionary<RankingViewMode, Button> _tabButtons = new();

        public bool IsOpen => _panel?.Visible == true;

        public void SetData(RankingBoard rankingBoard)
        {
            BuildUiIfNeeded();
            _rankingBoard = rankingBoard;
            RefreshContent();
        }

        public void Toggle()
        {
            BuildUiIfNeeded();
            SetVisibleState(!IsOpen);
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
                Color = new Color(0f, 0f, 0f, 0.6f)
            };
            AddChild(_overlay);

            _panel = new Panel
            {
                Position = new Vector2(120f, 200f),
                Size = new Vector2(840f, 1260f)
            };
            AddChild(_panel);

            var title = new Label
            {
                Position = new Vector2(32f, 22f),
                Size = new Vector2(500f, 40f),
                Text = "ランキング"
            };
            title.AddThemeFontSizeOverride("font_size", 30);
            _panel.AddChild(title);

            var close = new Button
            {
                Position = new Vector2(660f, 22f),
                Size = new Vector2(120f, 40f),
                Text = "閉じる"
            };
            close.Pressed += Toggle;
            _panel.AddChild(close);

            AddTabButton("最大深度", RankingViewMode.Depth, new Vector2(32f, 78f));
            AddTabButton("最高スコア", RankingViewMode.Score, new Vector2(242f, 78f));
            AddTabButton("最速1000m", RankingViewMode.Speed, new Vector2(452f, 78f));

            _contentLabel = new Label
            {
                Position = new Vector2(32f, 138f),
                Size = new Vector2(760f, 1072f),
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            _contentLabel.AddThemeFontSizeOverride("font_size", 22);
            _panel.AddChild(_contentLabel);

            _uiBuilt = true;
            SetVisibleState(false);
        }

        private void AddTabButton(string text, RankingViewMode viewMode, Vector2 position)
        {
            var button = new Button
            {
                Position = position,
                Size = new Vector2(180f, 42f),
                Text = text
            };
            button.Pressed += () =>
            {
                _currentViewMode = viewMode;
                RefreshContent();
            };
            _tabButtons[viewMode] = button;
            _panel.AddChild(button);
        }

        private void RefreshContent()
        {
            if (_rankingBoard == null || _contentLabel == null)
            {
                return;
            }

            foreach (var pair in _tabButtons)
            {
                pair.Value.Disabled = pair.Key == _currentViewMode;
            }

            _contentLabel.Text = _currentViewMode switch
            {
                RankingViewMode.Depth => BuildDepthText(),
                RankingViewMode.Score => BuildScoreText(),
                RankingViewMode.Speed => BuildSpeedText(),
                _ => string.Empty
            };
        }

        private string BuildDepthText()
        {
            return BuildRankingText(
                "歴代最大深度 TOP10",
                _rankingBoard.TopDepths,
                (rank, entry) => $"{rank}. {entry.Name} (第{entry.Generation}世代)  {entry.Depth}m  {entry.DateString}");
        }

        private string BuildScoreText()
        {
            return BuildRankingText(
                "最高スコア TOP10",
                _rankingBoard.TopScores,
                (rank, entry) => $"{rank}. {entry.Name} (第{entry.Generation}世代)  {entry.Score:N0}  {entry.DateString}");
        }

        private string BuildSpeedText()
        {
            return BuildRankingText(
                "最速深度1000m到達 TOP10",
                _rankingBoard.FastestDepth1000s,
                (rank, entry) => $"{rank}. {entry.Name} (第{entry.Generation}世代)  {entry.RunSeconds:F1}秒  {entry.DateString}",
                $"最速記録: {(_rankingBoard.BestTimeToDepth1000 == float.MaxValue ? "未到達" : $"{_rankingBoard.BestTimeToDepth1000:F1}秒")}");
        }

        private static string BuildRankingText(string title, IReadOnlyList<RankingEntry> entries, System.Func<int, RankingEntry, string> lineBuilder, string summary = null)
        {
            var lines = new List<string> { title };
            if (!string.IsNullOrWhiteSpace(summary))
            {
                lines.Add(summary);
            }

            lines.Add(string.Empty);
            if (entries == null || entries.Count == 0)
            {
                lines.Add("まだ記録がありません。");
                return string.Join("\n", lines);
            }

            lines.AddRange(entries.Select((entry, index) => lineBuilder(index + 1, entry)));
            return string.Join("\n", lines);
        }

        private void SetVisibleState(bool visible)
        {
            _overlay.Visible = visible;
            _panel.Visible = visible;
        }
    }
}