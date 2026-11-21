// ============================================================
// AppLogic.cs — 화면과 무관한 계산 로직
//
// WinForms에 의존하지 않아 Tests 프로젝트에서 그대로 검증합니다.
// ============================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace PersonalPro
{
    /// <summary>멀티뷰 칸의 위치와 크기를 계산합니다.</summary>
    public static class LayoutCalculator
    {
        /// <summary>경계선 비율을 끌 수 있는 범위로 제한합니다.</summary>
        public static double ClampSplit(double ratio) => Math.Clamp(ratio, 0.2, 0.8);

        /// <summary>
        /// 칸 수에 맞는 사각형 배열을 반환합니다.
        ///   mainIndex >= 0 → 해당 칸을 왼쪽에 크게, 나머지는 오른쪽에 세로로
        ///   1개 전체 / 2개 좌우 / 3개 위 2칸+아래 1칸 / 4개 2×2
        /// </summary>
        public static Rectangle[] Compute(int count, int w, int h, int mainIndex, double splitX, double splitY)
        {
            if (count <= 0) return Array.Empty<Rectangle>();
            if (count == 1) return new[] { new Rectangle(0, 0, w, h) };

            int x = (int)(w * splitX), y = (int)(h * splitY);

            if (mainIndex >= 0 && mainIndex < count)
            {
                var rects = new Rectangle[count];
                rects[mainIndex] = new Rectangle(0, 0, x, h);
                int subs = count - 1, subH = h / subs, row = 0;
                for (int i = 0; i < count; i++)
                {
                    if (i == mainIndex) continue;
                    int top = row * subH;
                    rects[i] = new Rectangle(x, top, w - x, row == subs - 1 ? h - top : subH);
                    row++;
                }
                return rects;
            }

            return count switch
            {
                2 => new[] { new Rectangle(0, 0, x, h), new Rectangle(x, 0, w - x, h) },
                3 => new[]
                {
                    new Rectangle(0, 0, x, y), new Rectangle(x, 0, w - x, y),
                    new Rectangle(0, y, w, h - y)
                },
                _ => new[]
                {
                    new Rectangle(0, 0, x, y), new Rectangle(x, 0, w - x, y),
                    new Rectangle(0, y, x, h - y), new Rectangle(x, y, w - x, h - y)
                }
            };
        }
    }

    /// <summary>OTT 이름과 URL 입력값을 검사합니다.</summary>
    public static class OttValidator
    {
        /// <summary>문제가 있으면 안내 문구를, 없으면 null을 반환합니다.</summary>
        public static string? Validate(string name, string url, string curName, IEnumerable<string> existingNames)
        {
            if (name.IndexOfAny(new[] { ',', '|' }) >= 0 || url.IndexOfAny(new[] { ',', '|' }) >= 0)
                return "이름과 URL에는 쉼표(,)와 세로줄(|)을 쓸 수 없습니다.";
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return "URL은 http:// 또는 https:// 로 시작하는 주소여야 합니다.";
            if (!name.Equals(curName, StringComparison.OrdinalIgnoreCase) &&
                existingNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                return "이미 같은 이름의 OTT가 있습니다.";
            return null;
        }
    }

    /// <summary>레이아웃 파일 형식 (예: NETFLIX,YOUTUBE,null,null)</summary>
    public static class PresetFormat
    {
        public static string Serialize(IEnumerable<string?> slots) =>
            string.Join(",", slots.Select(s => s ?? "null"));

        /// <summary>항상 4칸 배열을 반환합니다. 5번째 값부터는 무시합니다.</summary>
        public static string?[] Parse(string text)
        {
            var slots = new string?[4];
            var parts = text.Trim().Split(',');
            for (int i = 0; i < parts.Length && i < 4; i++)
                slots[i] = parts[i] is "null" or "" ? null : parts[i];
            return slots;
        }
    }

    public static class PathNames
    {
        /// <summary>파일명으로 못 쓰는 문자와 상위 폴더 이동(..)을 _ 로 바꿉니다.</summary>
        public static string Sanitize(string part) =>
            string.Concat(part.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)).Replace("..", "_");
    }

    /// <summary>사이드바 OTT 순서를 계산합니다.</summary>
    public static class OttOrdering
    {
        /// <summary>저장된 순서대로 정렬합니다. 순서에 없는 항목은 원래 순서로 뒤에 붙습니다.</summary>
        public static List<T> Sort<T>(IEnumerable<T> items, Func<T, string> name, IList<string> order) =>
            items.OrderBy(o =>
            {
                int i = order.IndexOf(name(o));
                return i < 0 ? int.MaxValue : i;
            }).ToList();

        /// <summary>이름을 delta칸 옮긴 새 순서를 반환합니다. 끝을 넘어가면 그대로입니다.</summary>
        public static List<string> Move(IEnumerable<string> names, string name, int delta)
        {
            var list = names.ToList();
            int from = list.IndexOf(name), to = from + delta;
            if (from < 0 || to < 0 || to >= list.Count) return list;
            (list[from], list[to]) = (list[to], list[from]);
            return list;
        }
    }

    /// <summary>시청 기록을 집계합니다. 데이터 형식: 날짜(yyyy-MM-dd) → OTT 이름 → 초</summary>
    public static class StatsAggregator
    {
        /// <summary>fromDate 이후 OTT별 합계를 큰 순서로 반환합니다. null이면 전체 기간입니다.</summary>
        public static List<(string Name, int Seconds)> Total(
            Dictionary<string, Dictionary<string, int>> data, string? fromDate)
        {
            return data
                .Where(day => fromDate == null || string.CompareOrdinal(day.Key, fromDate) >= 0)
                .SelectMany(day => day.Value)
                .GroupBy(e => e.Key)
                .Select(g => (Name: g.Key, Seconds: g.Sum(e => e.Value)))
                .OrderByDescending(e => e.Seconds)
                .ToList();
        }

        /// <summary>상위 max개만 남기고 나머지는 "기타"로 합칩니다.</summary>
        public static List<(string Name, int Seconds)> FoldOther(List<(string Name, int Seconds)> rows, int max)
        {
            if (rows.Count <= max) return rows;
            var top = rows.Take(max - 1).ToList();
            top.Add(("기타", rows.Skip(max - 1).Sum(r => r.Seconds)));
            return top;
        }

        public static string FormatDuration(int seconds)
        {
            int minutes = seconds / 60;
            if (minutes == 0) return "1분 미만";
            return minutes < 60 ? $"{minutes}분" : $"{minutes / 60}시간 {minutes % 60}분";
        }
    }
}
