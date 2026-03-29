using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using GTranslate.Translators;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public interface IJsonKeyTranslationService
{
    Task<IDictionary<string, string>> TranslateKeysAsync(IEnumerable<string> keys, CancellationToken ct = default);
    IDictionary<string, string> GetCachedTranslations();
}

public sealed class JsonKeyTranslationService : IJsonKeyTranslationService
{
    private static readonly ConcurrentDictionary<string, string> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly SemaphoreSlim TranslateLock = new(1, 1);
    private readonly ILogger<JsonKeyTranslationService> _logger;

    static JsonKeyTranslationService()
    {
        // Pre-seed with known mappings so common keys never hit the network
        var seeds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Core metadata
            ["agent"] = "角色", ["summary"] = "摘要", ["analysis"] = "分析",
            ["confidence"] = "置信度", ["confidenceScore"] = "置信度",
            ["trigger"] = "触发条件", ["invalidation"] = "失效条件",
            ["risk"] = "风险", ["riskLimit"] = "风险限制",
            ["reason"] = "原因", ["direction"] = "方向", ["rating"] = "评级",
            ["symbol"] = "代码", ["name"] = "名称", ["sector"] = "板块",
            ["source"] = "来源", ["title"] = "标题", ["content"] = "内容",
            ["excerpt"] = "摘录", ["publishedAt"] = "发布时间",
            // Fundamentals
            ["peRatio"] = "市盈率", ["peTtm"] = "市盈率(TTM)",
            ["volumeRatio"] = "量比", ["shareholderCount"] = "股东户数",
            ["floatMarketCap"] = "流通市值", ["marketCap"] = "总市值",
            ["turnoverRate"] = "换手率", ["changePercent"] = "涨跌幅",
            ["probability"] = "概率", ["evidence"] = "证据",
            ["keyPoints"] = "关键要点",
            // MCP metadata
            ["traceId"] = "追踪ID", ["taskId"] = "任务ID",
            ["toolName"] = "工具名称", ["latencyMs"] = "延迟(ms)",
            ["warnings"] = "告警", ["degradedFlags"] = "降级标记",
            // Evidence
            ["crawledAt"] = "采集时间", ["ingestedAt"] = "入库时间",
            ["level"] = "级别", ["sentiment"] = "情绪",
            ["target"] = "目标", ["tags"] = "标签",
            ["localFactId"] = "本地事实ID",
            // Market
            ["stageConfidence"] = "阶段置信度", ["mainlineScore"] = "主线强度",
            ["mainlineSectorName"] = "主线板块",
            ["advancers"] = "上涨家数", ["decliners"] = "下跌家数",
            ["limitUpCount"] = "涨停数", ["limitDownCount"] = "跌停数",
            // Technical
            ["signal"] = "信号", ["numericValue"] = "数值",
            ["state"] = "状态", ["timeframe"] = "时间框架",
            ["interval"] = "周期", ["period"] = "周期",
            // Fundamentals extra
            ["label"] = "字段名", ["value"] = "字段值",
            ["facts"] = "事实列表", ["businessScope"] = "经营范围",
            ["listingBoard"] = "上市板块",
            // Misc
            ["headline"] = "标题", ["dataCoverage"] = "数据覆盖",
            ["acquiredCount"] = "已获取数", ["displayedCount"] = "已展示数",
            ["missingFields"] = "缺失字段", ["requestedAt"] = "请求时间",
            ["status"] = "状态", ["action"] = "操作",
            ["entryConditions"] = "入场条件", ["positionSizing"] = "仓位管理",
            ["stopLoss"] = "止损", ["takeProfit"] = "止盈",
            ["targetPrice"] = "目标价", ["timeHorizon"] = "时间视野",
            ["executiveSummary"] = "执行摘要", ["investmentThesis"] = "投资论点",
            ["riskConsensus"] = "风险共识", ["dissent"] = "异议",
            ["nextActions"] = "后续操作", ["invalidationConditions"] = "失效条件",
            ["supportingEvidence"] = "支持证据", ["counterEvidence"] = "反面证据",
            ["confidenceExplanation"] = "置信度说明",
            ["bullStrengths"] = "看多论据", ["bearStrengths"] = "看空论据",
            ["investmentPlan"] = "投资计划", ["keyTriggers"] = "关键触发",
            ["riskWarnings"] = "风险警告", ["decision"] = "决策",
            ["decisionConfidence"] = "决策置信度",
            ["finalDecision"] = "最终决策",
            ["riskAssessment"] = "风险评估", ["acceptableRisks"] = "可接受风险",
            ["riskLimits"] = "风险限制", ["supportArguments"] = "支持论据",
            ["counterArguments"] = "反驳论据",
            ["criticalRisks"] = "关键风险", ["worstCaseScenarios"] = "最差情景",
            ["mitigationStrategies"] = "缓解策略",
            ["claim"] = "论点", ["counterPoints"] = "反驳要点",
            ["openQuestions"] = "待解问题",
            ["researchConclusion"] = "研究结论",
            ["toolResults"] = "工具结果", ["resultJson"] = "返回数据",
            ["description"] = "描述", ["type"] = "类型",
            ["url"] = "链接", ["date"] = "日期",
            ["price"] = "价格", ["volume"] = "成交量",
            ["open"] = "开盘价", ["close"] = "收盘价",
            ["high"] = "最高价", ["low"] = "最低价",
            ["amount"] = "成交额", ["count"] = "数量",
            ["change"] = "涨跌额", ["percent"] = "百分比",
            ["totalShares"] = "总股本", ["floatShares"] = "流通股本",
            ["eps"] = "每股收益", ["bvps"] = "每股净资产",
            ["roe"] = "净资产收益率", ["pbRatio"] = "市净率",
            ["debtRatio"] = "资产负债率", ["currentRatio"] = "流动比率",
            ["quickRatio"] = "速动比率",
            ["revenueGrowth"] = "营收增长率", ["profitGrowth"] = "利润增长率",
            ["grossMargin"] = "毛利率", ["netMargin"] = "净利率",
            ["operatingMargin"] = "营业利润率",
            // Fundamentals – income statement
            ["revenue"] = "营业收入", ["revenueYoY"] = "营收同比",
            ["netProfit"] = "净利润", ["netProfitYoY"] = "净利同比",
            // General / analysis
            ["changeType"] = "变动类型", ["holder"] = "持有人",
            ["product"] = "产品/业务", ["dataPoint"] = "数据项",
            ["assessment"] = "评估", ["metric"] = "指标",
            ["readMode"] = "阅读方式", ["readStatus"] = "阅读状态",
            ["significance"] = "重要性", ["currentValue"] = "当前值",
            ["indicator"] = "技术指标", ["finding"] = "发现",
            ["aspect"] = "分析维度", ["category"] = "分类",
            ["note"] = "备注", ["interpretation"] = "解读",
            // News / events
            ["eventBias"] = "事件偏向", ["impactScore"] = "影响分数",
            ["keyEvents"] = "关键事件", ["coverage"] = "覆盖范围",
            ["positive"] = "正面", ["neutral"] = "中性",
            ["negative"] = "负面", ["overall"] = "总体",
            ["highQualityCount"] = "高质量数量", ["recentCount"] = "近期数量",
            // Quality / valuation
            ["qualityView"] = "质量评估", ["valuationView"] = "估值评估",
            ["metrics"] = "财务指标", ["highlights"] = "亮点",
            ["risks"] = "风险", ["evidenceTable"] = "证据表",
            // Technical analysis
            ["trendState"] = "趋势状态", ["keyLevels"] = "关键价位",
            ["support"] = "支撑位", ["resistance"] = "压力位",
            ["vwap"] = "成交量加权均价", ["indicators"] = "技术指标",
            ["volumeAnalysis"] = "成交量分析", ["structureSummary"] = "结构总结",
            ["trendLine"] = "趋势线", ["breakout"] = "突破",
            ["gap"] = "跳空缺口",
            // Company profile
            ["mainBusiness"] = "主营业务", ["operatingScope"] = "经营范围",
            ["fullName"] = "公司全称", ["englishName"] = "英文名称",
            ["securityType"] = "证券类别", ["exchange"] = "上市交易所",
            ["industryBoard"] = "所属行业", ["csrcIndustry"] = "证监会行业",
            // Scoring / ranking
            ["score"] = "得分", ["rank"] = "排名", ["weight"] = "权重",
            // Trading
            ["impact"] = "影响", ["stop"] = "止损",
            ["entry"] = "入场", ["exit"] = "出场",
        };
        foreach (var kv in seeds)
            Cache.TryAdd(kv.Key, kv.Value);
    }

    public JsonKeyTranslationService(ILogger<JsonKeyTranslationService> logger)
    {
        _logger = logger;
    }

    public IDictionary<string, string> GetCachedTranslations()
    {
        return new Dictionary<string, string>(Cache, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IDictionary<string, string>> TranslateKeysAsync(IEnumerable<string> keys, CancellationToken ct = default)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var missing = new List<string>();

        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (Cache.TryGetValue(key, out var cached))
                result[key] = cached;
            else
                missing.Add(key);
        }

        if (missing.Count == 0)
            return result;

        // Translate missing keys via GTranslate (Google)
        await TranslateLock.WaitAsync(ct);
        try
        {
            // Re-check after acquiring lock in case another thread translated them
            var stillMissing = missing.Where(k => !Cache.ContainsKey(k)).ToList();
            if (stillMissing.Count == 0)
            {
                foreach (var k in missing)
                    if (Cache.TryGetValue(k, out var v))
                        result[k] = v;
                return result;
            }

            var translator = new GoogleTranslator();
            foreach (var key in stillMissing)
            {
                try
                {
                    var english = CamelCaseToWords(key);
                    var translated = await translator.TranslateAsync(english, "zh-CN");
                    var chineseValue = translated.Translation;
                    if (!string.IsNullOrWhiteSpace(chineseValue))
                    {
                        Cache.TryAdd(key, chineseValue);
                        result[key] = chineseValue;
                    }
                    else
                    {
                        // Fallback to title-cased English
                        Cache.TryAdd(key, english);
                        result[key] = english;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to translate key '{Key}', using fallback", key);
                    var fallback = CamelCaseToWords(key);
                    Cache.TryAdd(key, fallback);
                    result[key] = fallback;
                }
            }
        }
        finally
        {
            TranslateLock.Release();
        }

        return result;
    }

    private static string CamelCaseToWords(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        var spaced = Regex.Replace(key, @"([a-z0-9])([A-Z])", "$1 $2");
        spaced = Regex.Replace(spaced, @"_", " ");
        return spaced.Trim();
    }
}
