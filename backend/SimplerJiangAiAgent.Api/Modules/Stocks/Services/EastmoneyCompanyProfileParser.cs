using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

internal static class EastmoneyCompanyProfileParser
{
    private static readonly Regex FinanceReportYearRegex = new("(?<year>20\\d{2})", RegexOptions.Compiled);
    private static readonly (string JsonKey, string Label)[] FactMappings =
    [
        ("zyyw", "主营业务"),
        ("jyfw", "经营范围"),
        ("gsmc", "公司全称"),
        ("ywmc", "英文名称"),
        ("zqlb", "证券类别"),
        ("ssjys", "上市交易所"),
        ("sshy", "所属行业"),
        ("sszjhhy", "证监会行业"),
        ("qy", "所属地区"),
        ("zcdz", "注册地址"),
        ("bgdz", "办公地址"),
        ("gswz", "公司网站"),
        ("zjl", "总经理"),
        ("frdb", "法人代表"),
        ("dsz", "董事长"),
        ("zczb", "注册资本"),
        ("lxdh", "联系电话"),
        ("dzxx", "电子邮箱")
    ];

    public static EastmoneyCompanyProfileDto Parse(string symbol, string json, string? shareholderJson = null)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("jbzl", out var profileNode) || profileNode.ValueKind != JsonValueKind.Object)
        {
            return new EastmoneyCompanyProfileDto(symbol, symbol, null, ParseShareholderCount(shareholderJson), ParseShareholderFacts(shareholderJson));
        }

        var name = profileNode.TryGetProperty("agjc", out var nameNode)
            ? nameNode.GetString() ?? symbol
            : symbol;
        var sectorName = profileNode.TryGetProperty("sshy", out var sectorNode)
            ? sectorNode.GetString()
            : null;

        var shareholderCount = ParseShareholderCount(shareholderJson);
        var facts = BuildFacts(profileNode, shareholderJson, shareholderCount);

        return new EastmoneyCompanyProfileDto(
            symbol,
            name,
            string.IsNullOrWhiteSpace(sectorName) ? null : sectorName.Trim(),
            shareholderCount,
            facts);
    }

    private static IReadOnlyList<StockFundamentalFactDto> BuildFacts(JsonElement profileNode, string? shareholderJson, int? shareholderCount)
    {
        var facts = new List<StockFundamentalFactDto>();
        foreach (var (jsonKey, label) in FactMappings)
        {
            if (!profileNode.TryGetProperty(jsonKey, out var valueNode) || valueNode.ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            var value = valueNode.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(value) || value == "--")
            {
                continue;
            }

            facts.Add(new StockFundamentalFactDto(label, value, "东方财富公司概况"));
        }

        // Derive 主营业务 from 经营范围 when zyyw is empty
        if (!facts.Any(f => f.Label == "主营业务"))
        {
            var derived = DeriveMainBusinessFromScope(facts.FirstOrDefault(f => f.Label == "经营范围")?.Value);
            if (!string.IsNullOrWhiteSpace(derived))
            {
                facts.Insert(0, new StockFundamentalFactDto("主营业务", derived, "东方财富公司概况(经营范围摘要)"));
            }
        }

        if (shareholderCount.HasValue)
        {
            facts.Add(new StockFundamentalFactDto("股东户数", shareholderCount.Value.ToString(), "东方财富股东研究"));
        }

        foreach (var fact in ParseShareholderFacts(shareholderJson))
        {
            if (facts.Any(existing => existing.Label == fact.Label && existing.Value == fact.Value))
            {
                continue;
            }

            facts.Add(fact);
        }

        return facts;
    }

    private static IReadOnlyList<StockFundamentalFactDto> ParseShareholderFacts(string? shareholderJson)
    {
        if (string.IsNullOrWhiteSpace(shareholderJson))
            return Array.Empty<StockFundamentalFactDto>();

        using var document = JsonDocument.Parse(shareholderJson);
        var root = document.RootElement;
        var facts = new List<StockFundamentalFactDto>();
        const string source = "东方财富股东研究";

        // ── 股东人数 (gdrs) ──
        if (root.TryGetProperty("gdrs", out var gdrsNode) && gdrsNode.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in gdrsNode.EnumerateArray())
            {
                if (item.TryGetProperty("END_DATE", out var endDate) && !string.IsNullOrWhiteSpace(endDate.GetString()))
                    facts.Add(new StockFundamentalFactDto("股东户数统计截止", endDate.GetString()!.Trim(), source));

                if (item.TryGetProperty("HOLD_FOCUS", out var focus) && !string.IsNullOrWhiteSpace(focus.GetString()))
                    facts.Add(new StockFundamentalFactDto("股权集中度", focus.GetString()!.Trim(), source));

                if (item.TryGetProperty("AVG_HOLD_AMT", out var avgAmt))
                {
                    var val = avgAmt.ValueKind == JsonValueKind.Number && avgAmt.TryGetDecimal(out var d)
                        ? Math.Round(d, 2).ToString("0.##") : avgAmt.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(val))
                        facts.Add(new StockFundamentalFactDto("户均持股市值", val, source));
                }

                if (item.TryGetProperty("AVG_FREE_SHARES", out var avgShares))
                {
                    var val = avgShares.ValueKind == JsonValueKind.Number && avgShares.TryGetDecimal(out var d)
                        ? Math.Round(d, 2).ToString("0.##") : avgShares.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(val))
                        facts.Add(new StockFundamentalFactDto("户均流通股", val, source));
                }

                break; // Only take the most recent period
            }
        }

        // ── 十大流通股东 (sdltgd) ──
        if (root.TryGetProperty("sdltgd", out var ltgdNode) && ltgdNode.ValueKind == JsonValueKind.Array)
        {
            var rank = 0;
            foreach (var holder in ltgdNode.EnumerateArray())
            {
                if (++rank > 10) break;
                var name = holder.TryGetProperty("HOLDER_NAME", out var hn) ? hn.GetString()?.Trim() : null;
                if (string.IsNullOrWhiteSpace(name)) continue;

                var holdNum = holder.TryGetProperty("HOLD_NUM", out var hnum) && hnum.ValueKind == JsonValueKind.Number
                    ? hnum.GetDecimal() : (decimal?)null;
                var holdRatio = holder.TryGetProperty("FREE_HOLDNUM_RATIO", out var ratio) && ratio.ValueKind == JsonValueKind.Number
                    ? ratio.GetDecimal() : (decimal?)null;
                var changeStr = holder.TryGetProperty("HOLD_NUM_CHANGE", out var change) && change.ValueKind == JsonValueKind.Number
                    ? change.GetDecimal().ToString("0.##") : (holder.TryGetProperty("HOLD_NUM_CHANGE", out var cs) ? cs.GetString()?.Trim() : null);
                var changeType = holder.TryGetProperty("CHANGE_TYPE", out var ct) ? ct.GetString()?.Trim() : null;

                var detail = $"{name}";
                if (holdNum.HasValue)
                    detail += $"，持股{FormatLargeNumber(holdNum.Value)}股";
                if (holdRatio.HasValue)
                    detail += $"，占比{holdRatio.Value:0.##}%";
                if (!string.IsNullOrWhiteSpace(changeType))
                    detail += $"，{changeType}";
                if (!string.IsNullOrWhiteSpace(changeStr) && changeStr != "0")
                    detail += $"（变动{changeStr}股）";

                facts.Add(new StockFundamentalFactDto($"十大流通股东#{rank}", detail, source));
            }
        }

        // ── 十大股东 (sdgd) ──
        if (root.TryGetProperty("sdgd", out var gdNode) && gdNode.ValueKind == JsonValueKind.Array)
        {
            var rank = 0;
            foreach (var holder in gdNode.EnumerateArray())
            {
                if (++rank > 10) break;
                var name = holder.TryGetProperty("HOLDER_NAME", out var hn) ? hn.GetString()?.Trim() : null;
                if (string.IsNullOrWhiteSpace(name)) continue;

                var holdNum = holder.TryGetProperty("HOLD_NUM", out var hnum) && hnum.ValueKind == JsonValueKind.Number
                    ? hnum.GetDecimal() : (decimal?)null;
                var holdRatio = holder.TryGetProperty("HOLD_NUM_RATIO", out var ratio) && ratio.ValueKind == JsonValueKind.Number
                    ? ratio.GetDecimal() : (decimal?)null;
                var holderType = holder.TryGetProperty("HOLDER_TYPE", out var ht) ? ht.GetString()?.Trim() : null;

                var detail = $"{name}";
                if (!string.IsNullOrWhiteSpace(holderType))
                    detail += $"（{holderType}）";
                if (holdNum.HasValue)
                    detail += $"，持股{FormatLargeNumber(holdNum.Value)}股";
                if (holdRatio.HasValue)
                    detail += $"，占比{holdRatio.Value:0.##}%";

                facts.Add(new StockFundamentalFactDto($"十大股东#{rank}", detail, source));
            }
        }

        return facts;
    }

    private static string FormatLargeNumber(decimal num)
    {
        if (Math.Abs(num) >= 100_000_000m)
            return $"{num / 100_000_000m:0.##}亿";
        if (Math.Abs(num) >= 10_000m)
            return $"{num / 10_000m:0.##}万";
        return num.ToString("0.##");
    }

    internal static string? DeriveMainBusinessFromScope(string? businessScope)
    {
        if (string.IsNullOrWhiteSpace(businessScope))
        {
            return null;
        }

        var segments = businessScope.Split(new[] { '；', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            return null;
        }

        // Take the first 3 segments or up to 200 chars
        var builder = new System.Text.StringBuilder();
        var taken = 0;
        foreach (var segment in segments)
        {
            if (taken >= 3 || builder.Length + segment.Length > 200)
            {
                break;
            }

            if (builder.Length > 0)
            {
                builder.Append('；');
            }

            builder.Append(segment);
            taken++;
        }

        var result = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static int? ParseShareholderCount(string? shareholderJson)
    {
        if (string.IsNullOrWhiteSpace(shareholderJson))
        {
            return null;
        }

        using var document = JsonDocument.Parse(shareholderJson);
        var root = document.RootElement;
        if (!root.TryGetProperty("gdrs", out var gdrsNode) || gdrsNode.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in gdrsNode.EnumerateArray())
        {
            if (!item.TryGetProperty("HOLDER_TOTAL_NUM", out var countNode))
            {
                continue;
            }

            if (countNode.ValueKind == JsonValueKind.Number && countNode.TryGetInt32(out var count))
            {
                return count;
            }

            if (countNode.ValueKind == JsonValueKind.String && int.TryParse(countNode.GetString(), out count))
            {
                return count;
            }
        }

        return null;
    }

    public static IReadOnlyList<StockFundamentalFactDto> ParseFinanceFacts(string? financeJson)
    {
        if (string.IsNullOrWhiteSpace(financeJson))
        {
            return Array.Empty<StockFundamentalFactDto>();
        }

        try
        {
            using var document = JsonDocument.Parse(financeJson);
            var root = document.RootElement;
            if (!root.TryGetProperty("data", out var dataNode) || dataNode.ValueKind != JsonValueKind.Array || dataNode.GetArrayLength() == 0)
            {
                return Array.Empty<StockFundamentalFactDto>();
            }

            var latest = SelectLatestFinanceNode(dataNode);
            var facts = new List<StockFundamentalFactDto>();

            void AddFactOrSkip(string jsonKey, string label, string unit = "", decimal divisor = 1m)
            {
                if (latest.TryGetProperty(jsonKey, out var valNode) && valNode.ValueKind == JsonValueKind.Number && valNode.TryGetDecimal(out var val))
                {
                    var formatted = (val / divisor).ToString("0.##");
                    facts.Add(new StockFundamentalFactDto(label, $"{formatted}{unit}", "东方财富最新财报"));
                }
            }

            if (latest.TryGetProperty("REPORT_DATE_NAME", out var reportNameNode) && reportNameNode.ValueKind == JsonValueKind.String)
            {
                facts.Add(new StockFundamentalFactDto("最新财报期", reportNameNode.GetString() ?? "", "东方财富最新财报"));
            }

            AddFactOrSkip("TOTALOPERATEREVE", "营业收入", "亿元", 100000000m);
            AddFactOrSkip("PARENTNETPROFIT", "归属净利润", "亿元", 100000000m);
            AddFactOrSkip("KCFJCXSYJLR", "扣非净利润", "亿元", 100000000m);
            AddFactOrSkip("TOTALOPERATEREVETZ", "营收同比", "%");
            AddFactOrSkip("PARENTNETPROFITTZ", "归属净利同比", "%");
            AddFactOrSkip("EPSJB", "基本每股收益", "元");
            AddFactOrSkip("BPS", "每股净资产", "元");
            AddFactOrSkip("ROEJQ", "净资产收益率(ROE)", "%");
            AddFactOrSkip("XSMLL", "销售毛利率", "%");
            AddFactOrSkip("XSJLL", "销售净利率", "%");
            AddFactOrSkip("ZCFZL", "资产负债率", "%");

            return facts;
        }
        catch
        {
            return Array.Empty<StockFundamentalFactDto>();
        }
    }

    private static JsonElement SelectLatestFinanceNode(JsonElement dataNode)
    {
        JsonElement? best = null;
        (DateTime? ReportDate, int ReportRank, int Index) bestKey = (null, int.MinValue, int.MaxValue);
        var index = 0;

        foreach (var item in dataNode.EnumerateArray())
        {
            var currentKey = ResolveFinanceSortKey(item, index);
            if (best is null || CompareFinanceSortKey(currentKey, bestKey) > 0)
            {
                best = item;
                bestKey = currentKey;
            }

            index += 1;
        }

        return best ?? dataNode[0];
    }

    private static (DateTime? ReportDate, int ReportRank, int Index) ResolveFinanceSortKey(JsonElement item, int index)
    {
        var reportDate = TryParseFinanceDate(item, "REPORT_DATE")
            ?? TryParseFinanceDate(item, "REPORTDATE")
            ?? TryParseFinanceDate(item, "NOTICE_DATE");
        var reportRank = ResolveFinanceReportRank(item);
        return (reportDate, reportRank, -index);
    }

    private static int CompareFinanceSortKey((DateTime? ReportDate, int ReportRank, int Index) left, (DateTime? ReportDate, int ReportRank, int Index) right)
    {
        var dateComparison = Nullable.Compare(left.ReportDate, right.ReportDate);
        if (dateComparison != 0)
        {
            return dateComparison;
        }

        var rankComparison = left.ReportRank.CompareTo(right.ReportRank);
        if (rankComparison != 0)
        {
            return rankComparison;
        }

        return left.Index.CompareTo(right.Index);
    }

    private static DateTime? TryParseFinanceDate(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var node) || node.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var raw = node.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out var parsed)
            ? parsed.Date
            : null;
    }

    private static int ResolveFinanceReportRank(JsonElement item)
    {
        if (!item.TryGetProperty("REPORT_DATE_NAME", out var node) || node.ValueKind != JsonValueKind.String)
        {
            return int.MinValue;
        }

        var name = node.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return int.MinValue;
        }

        var year = 0;
        var match = FinanceReportYearRegex.Match(name);
        if (match.Success)
        {
            _ = int.TryParse(match.Groups["year"].Value, out year);
        }

        var quarterWeight = name.Contains("年报", StringComparison.OrdinalIgnoreCase)
            ? 4
            : name.Contains("三季报", StringComparison.OrdinalIgnoreCase)
                ? 3
                : name.Contains("中报", StringComparison.OrdinalIgnoreCase) || name.Contains("半年报", StringComparison.OrdinalIgnoreCase)
                    ? 2
                    : name.Contains("一季报", StringComparison.OrdinalIgnoreCase)
                        ? 1
                        : 0;

        return year * 10 + quarterWeight;
    }

    public static IReadOnlyList<StockFundamentalFactDto> ParseMainBusinessComposition(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<StockFundamentalFactDto>();
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!root.TryGetProperty("result", out var resultNode)
                || resultNode.ValueKind != JsonValueKind.Object
                || !resultNode.TryGetProperty("data", out var dataNode)
                || dataNode.ValueKind != JsonValueKind.Array
                || dataNode.GetArrayLength() == 0)
            {
                return Array.Empty<StockFundamentalFactDto>();
            }

            // Find the latest REPORT_DATE
            string? latestDate = null;
            foreach (var item in dataNode.EnumerateArray())
            {
                var date = item.TryGetProperty("REPORT_DATE", out var dateNode) && dateNode.ValueKind == JsonValueKind.String
                    ? dateNode.GetString()?.Trim()
                    : null;
                if (!string.IsNullOrWhiteSpace(date) && (latestDate is null || string.Compare(date, latestDate, StringComparison.Ordinal) > 0))
                {
                    latestDate = date;
                }
            }

            if (latestDate is null)
            {
                return Array.Empty<StockFundamentalFactDto>();
            }

            var facts = new List<StockFundamentalFactDto>();
            foreach (var item in dataNode.EnumerateArray())
            {
                var date = item.TryGetProperty("REPORT_DATE", out var dateNode) && dateNode.ValueKind == JsonValueKind.String
                    ? dateNode.GetString()?.Trim()
                    : null;
                if (!string.Equals(date, latestDate, StringComparison.Ordinal))
                {
                    continue;
                }

                var mainopType = item.TryGetProperty("MAINOP_TYPE", out var typeNode) && typeNode.ValueKind == JsonValueKind.Number
                    ? typeNode.GetInt32()
                    : 0;
                if (mainopType != 2 && mainopType != 3)
                {
                    continue;
                }

                var itemName = item.TryGetProperty("ITEM_NAME", out var nameNode) && nameNode.ValueKind == JsonValueKind.String
                    ? nameNode.GetString()?.Trim()
                    : null;
                if (string.IsNullOrWhiteSpace(itemName))
                {
                    continue;
                }

                var income = item.TryGetProperty("MAIN_BUSINESS_INCOME", out var incomeNode) && incomeNode.ValueKind == JsonValueKind.Number
                    ? incomeNode.GetDecimal()
                    : (decimal?)null;
                var ratio = item.TryGetProperty("MBI_RATIO", out var ratioNode) && ratioNode.ValueKind == JsonValueKind.Number
                    ? ratioNode.GetDecimal()
                    : (decimal?)null;

                var category = mainopType == 2 ? "产品" : "地区";
                var label = $"主营构成({category})-{itemName}";
                var value = FormatMainBusinessValue(income, ratio);
                facts.Add(new StockFundamentalFactDto(label, value, "东方财富主营构成"));
            }

            // Add report-date summary fact
            var displayDate = latestDate.Length > 10 ? latestDate[..10] : latestDate;
            facts.Add(new StockFundamentalFactDto("主营构成报告期", displayDate, "东方财富主营构成"));

            return facts;
        }
        catch
        {
            return Array.Empty<StockFundamentalFactDto>();
        }
    }

    internal static string FormatMainBusinessValue(decimal? income, decimal? ratio)
    {
        var parts = new List<string>();
        if (income.HasValue)
        {
            if (Math.Abs(income.Value) >= 100_000_000m)
            {
                parts.Add($"{(income.Value / 100_000_000m):0.##}亿元");
            }
            else
            {
                parts.Add($"{(income.Value / 10_000m):0.##}万元");
            }
        }

        if (ratio.HasValue)
        {
            parts.Add($"{(ratio.Value * 100m):0.0#}%");
        }

        return parts.Count > 0 ? string.Join(" ", parts) : "--";
    }
}