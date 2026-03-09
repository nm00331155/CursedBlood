using CursedBlood.Achievement;
using CursedBlood.Generation;
using Godot;

namespace CursedBlood.UI
{
    public partial class TitleScreen : Control
    {
        private Label _summaryLabel;

        public override void _Ready()
        {
            var background = new ColorRect
            {
                Position = Vector2.Zero,
                Size = new Vector2(1080f, 1920f),
                Color = new Color(0.08f, 0.06f, 0.09f)
            };
            AddChild(background);

            var title = new Label
            {
                Position = new Vector2(140f, 180f),
                Size = new Vector2(800f, 120f),
                Text = "CursedBlood",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            title.AddThemeFontSizeOverride("font_size", 72);
            AddChild(title);

            AddButton("Start", 520f, () => GetTree().ChangeSceneToFile("res://Scenes/CursedBlood/CursedBloodMain.tscn"));
            AddButton("Family Tree", 600f, ShowFamilyTreeSummary);
            AddButton("Achievements", 680f, ShowAchievementSummary);
            AddButton("Rankings", 760f, ShowRankingSummary);

            _summaryLabel = new Label
            {
                Position = new Vector2(160f, 920f),
                Size = new Vector2(760f, 720f),
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            _summaryLabel.AddThemeFontSizeOverride("font_size", 24);
            AddChild(_summaryLabel);
        }

        private void AddButton(string text, float y, System.Action action)
        {
            var button = new Button
            {
                Position = new Vector2(340f, y),
                Size = new Vector2(400f, 56f),
                Text = text
            };
            button.Pressed += () => action();
            AddChild(button);
        }

        private void ShowFamilyTreeSummary()
        {
            var familyTree = FamilyTree.Load();
            _summaryLabel.Text = familyTree.Records.Count == 0
                ? "家系図はまだ空です。"
                : string.Join("\n", familyTree.Records.GetRange(0, System.Math.Min(5, familyTree.Records.Count)).ConvertAll(record => $"第{record.Generation}世代 {record.Name} 深度:{record.MaxDepth} スコア:{record.Score:N0}"));
        }

        private void ShowAchievementSummary()
        {
            var achievements = AchievementManager.Load();
            _summaryLabel.Text = $"解除数: {achievements.GetUnlockedCount()} / {achievements.Entries.Count}\n" +
                string.Join("\n", achievements.Entries.FindAll(entry => entry.Unlocked).ConvertAll(entry => entry.Title));
        }

        private void ShowRankingSummary()
        {
            var rankings = RankingBoard.Load();
            _summaryLabel.Text =
                $"最高深度: {rankings.BestDepth}\n" +
                $"最高スコア: {rankings.BestScore:N0}\n" +
                $"深度1000最速: {(rankings.BestTimeToDepth1000 == float.MaxValue ? "未到達" : $"{rankings.BestTimeToDepth1000:F1}秒")}";
        }
    }
}