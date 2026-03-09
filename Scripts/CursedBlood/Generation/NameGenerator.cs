using System;

namespace CursedBlood.Generation
{
    public static class NameGenerator
    {
        private static readonly Random Rng = new();

        private static readonly string[] MaleNames =
        {
            "タケル", "ハヤト", "レン", "ソウタ", "ユウキ", "カイト", "リク", "ハルト", "アオイ", "シン",
            "ケンジ", "リュウ", "マサト", "ダイチ", "コウタ", "ナオキ", "ショウ", "ツバサ", "ゲンキ", "イサム",
            "テツヤ", "カズマ", "ヨシト", "リョウ", "シュウ", "トモヤ", "セイジ", "ミツル", "アキラ", "ジン"
        };

        private static readonly string[] FemaleNames =
        {
            "サクラ", "ヒナタ", "アオイ", "ミヅキ", "ユイ", "ホノカ", "リン", "カエデ", "ナツメ", "スズ",
            "マイ", "ハナ", "ミサキ", "シオリ", "アカネ", "コハル", "チヒロ", "ミオ", "ルイ", "カナデ",
            "ツムギ", "フウカ", "サヤ", "ノドカ", "ワカナ", "ヒカリ", "ユウナ", "レイ", "マコト", "イヅミ"
        };

        private static readonly string[] Surnames =
        {
            "深掘", "地底", "黒鉄", "呪井", "血脈", "闇堀", "鉱山", "掘田", "岩切", "地脈",
            "穴倉", "金堀", "土竜", "暗黒", "奈落", "深淵", "鍬形", "採掘", "鋼", "石割"
        };

        public static string Generate(bool isMale)
        {
            var givenNamePool = isMale ? MaleNames : FemaleNames;
            return $"{Surnames[Rng.Next(Surnames.Length)]} {givenNamePool[Rng.Next(givenNamePool.Length)]}";
        }
    }
}