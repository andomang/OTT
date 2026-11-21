// ============================================================
// AppSettings.cs — PC에 저장하는 설정과 시청 기록
//
// settings.json     : 창 위치, 다크 모드, 사용자별 OTT 순서
// watch_stats.json  : 날짜별·OTT별 시청 시간(초)
// ============================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PersonalPro
{
    public class AppSettings
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Maximized { get; set; }
        public bool DarkMode { get; set; }
        public Dictionary<string, List<string>> OttOrder { get; set; } = new();

        private static string FilePath => DatabaseHelper.DataPath("settings.json");

        public static AppSettings Current { get; } = Load();

        private static AppSettings Load()
        {
            try
            {
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                return new AppSettings();  // 파일 없음 또는 손상 → 기본값
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
        }
    }

    public static class WatchStats
    {
        private static string FilePath(string userId) => DatabaseHelper.DataPath(userId, "watch_stats.json");

        public static Dictionary<string, Dictionary<string, int>> Load(string userId)
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(
                    File.ReadAllText(FilePath(userId))) ?? new();
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                return new();  // 파일 없음 또는 손상 → 기록 없음
            }
        }

        /// <summary>오늘 날짜에 시청 시간을 더합니다. 저장 실패는 시청에 영향이 없으므로 무시합니다.</summary>
        public static void Add(string userId, string name, int seconds)
        {
            var data = Load(userId);
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            if (!data.TryGetValue(today, out var day)) data[today] = day = new();
            day[name] = day.GetValueOrDefault(name) + seconds;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath(userId))!);
                File.WriteAllText(FilePath(userId), JsonSerializer.Serialize(data));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }
}
