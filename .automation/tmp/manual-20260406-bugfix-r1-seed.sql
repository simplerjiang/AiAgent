BEGIN;
DELETE FROM LocalSectorReports WHERE ExternalId LIKE 'manual-20260406-bugfix-r1-%';
DELETE FROM LocalStockNews WHERE ExternalId LIKE 'manual-20260406-bugfix-r1-%';

INSERT INTO LocalSectorReports (
    Symbol,
    SectorName,
    Level,
    Title,
    Source,
    SourceTag,
    ExternalId,
    PublishTime,
    CrawledAt,
    Url,
    IsAiProcessed,
    TranslatedTitle,
    AiSentiment,
    AiTarget,
    AiTags,
    ArticleExcerpt,
    ArticleSummary,
    ReadMode,
    ReadStatus,
    IngestedAt
) VALUES (
    NULL,
    '大盘环境',
    'market',
    'Market sample manual 20260406 bugfix r1',
    'CoinTelegraph',
    'cointelegraph-rss',
    'manual-20260406-bugfix-r1-market',
    datetime('now', 'localtime'),
    datetime('now', 'localtime'),
    'https://example.invalid/manual-20260406-bugfix-r1-market',
    0,
    NULL,
    '中性',
    NULL,
    NULL,
    NULL,
    NULL,
    'url_unavailable',
    'unverified',
    datetime('now', 'localtime')
);

INSERT INTO LocalSectorReports (
    Symbol,
    SectorName,
    Level,
    Title,
    Source,
    SourceTag,
    ExternalId,
    PublishTime,
    CrawledAt,
    Url,
    IsAiProcessed,
    TranslatedTitle,
    AiSentiment,
    AiTarget,
    AiTags,
    ArticleExcerpt,
    ArticleSummary,
    ReadMode,
    ReadStatus,
    IngestedAt
) VALUES (
    NULL,
    '半导体',
    'sector',
    'Sector sample semiconductor manual 20260406 bugfix r1',
    '东方财富公告',
    'eastmoney-announcement',
    'manual-20260406-bugfix-r1-sector',
    datetime('now', 'localtime'),
    datetime('now', 'localtime'),
    'https://example.invalid/manual-20260406-bugfix-r1-sector',
    0,
    NULL,
    '中性',
    NULL,
    NULL,
    NULL,
    NULL,
    'url_unavailable',
    'unverified',
    datetime('now', 'localtime')
);

INSERT INTO LocalStockNews (
    Symbol,
    Name,
    SectorName,
    Title,
    Category,
    Source,
    SourceTag,
    ExternalId,
    PublishTime,
    CrawledAt,
    Url,
    IsAiProcessed,
    TranslatedTitle,
    AiSentiment,
    AiTarget,
    AiTags,
    ArticleExcerpt,
    ArticleSummary,
    ReadMode,
    ReadStatus,
    IngestedAt
) VALUES (
    'sz000858',
    '五粮液',
    '酿酒行业',
    'Stock sample wuliangye manual 20260406 bugfix r1',
    '公告',
    '东方财富公告',
    'eastmoney-announcement',
    'manual-20260406-bugfix-r1-stock',
    datetime('now', 'localtime'),
    datetime('now', 'localtime'),
    'https://example.invalid/manual-20260406-bugfix-r1-stock',
    0,
    NULL,
    '中性',
    NULL,
    NULL,
    NULL,
    NULL,
    'url_unavailable',
    'unverified',
    datetime('now', 'localtime')
);

COMMIT;

SELECT 'sector_report' AS TableName, Id, Level, Title, ExternalId, IFNULL(IsAiProcessed, 0) AS IsAiProcessed
FROM LocalSectorReports
WHERE ExternalId LIKE 'manual-20260406-bugfix-r1-%'
UNION ALL
SELECT 'stock_news', Id, 'stock', Title, ExternalId, IFNULL(IsAiProcessed, 0)
FROM LocalStockNews
WHERE ExternalId LIKE 'manual-20260406-bugfix-r1-%'
ORDER BY TableName, Id;