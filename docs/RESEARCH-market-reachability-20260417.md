# 市场数据可达性探测与研究结论（2026-04-17）

## 研究目标

本次研究聚焦于“市场关键数据链路在真实运行环境中的可达性与业务可用性”，目标是回答以下问题：
- 哪些源在传输层可达但业务层不稳定。
- 哪些源可以作为主路径（Primary）承载实时口径。
- 哪些源仅适合作为补位路径（Secondary）。
- 哪些源当前应明确降级或剔除（Reject）。
- 当前是否可以宣称“已解决所有数据不可达”。

结论先行：目前不能判定“已解决所有数据不可达”。

## 探测方法与覆盖维度

本次采用矩阵化探测，按“transport / payload / business”三层评估每个源：
- transport：请求链路是否可达（HTTP、连接、超时、响应返回）。
- payload：返回结构是否满足解析契约（字段完整性、类型稳定性、空值/缺字段比例）。
- business：是否能稳定支撑业务语义（可持续输出、波动可控、与最终状态一致）。

覆盖维度包括：
- 板块快照主链路（bkzj 多参数组合）。
- 成交额链路（push2 ulist）。
- 宽度链路（push2 clist / push2ex ZDFenBu）。
- 涨停池与连板相关链路（THS / push2ex / datacenter）。
- 同步后状态一致性（source status vs recentSync 业务状态）。

## 关键结论

1. 不能判定“已解决所有数据不可达”。
2. bkzj dual merge（f3+f62）可用性高，优于单参数路径。
3. push2 ulist 的成交额链路整体可用，但存在波动，需要容错与连续观测。
4. push2 clist 的 breadth（市场宽度）稳定性不足，不宜继续作为主路径。
5. push2ex getTopicZDFenBu 可作为 breadth 主路径。
6. THS continuous_limit_up 可用，但需重试机制保障稳定产出。
7. source status 与 recentSync 业务状态存在语义脱节：源侧“ok”不等价于最终业务快照“complete”。
8. push2ex getTopicZTPool 与 datacenter RPT_LIMIT_UP_POOL 当前应判定为 Reject，不应进入主回退链。

## 源能力分级表（Primary / Secondary / Reject）

| 源 | 分级 | transport 可用性 | payload 可用性 | business 可用性 | 结论说明 |
|---|---|---|---|---|---|
| bkzj f3 only | Secondary | 高 | 中 | 中 | 可达但单维度信息不足，建议仅作补位。 |
| bkzj f62 only | Secondary | 高 | 中 | 中 | 可达但单维度信息不足，建议仅作补位。 |
| bkzj dual merge f3+f62 | Primary | 高 | 高 | 高 | 当前板块链路最稳方案，应作为主路径。 |
| push2 ulist turnover | Secondary | 高 | 中高 | 中 | 成交额可用但波动存在，宜保留为次级并加监控。 |
| push2 clist breadth | Reject | 中 | 中低 | 低 | 宽度结果不稳定，当前不宜作为生产主链路。 |
| push2ex getTopicZDFenBu | Primary | 高 | 高 | 中高 | 可作为 breadth 主路径，需持续观察盘中抖动。 |
| ths continuous_limit_up | Secondary | 中高 | 中高 | 中 | 可用但偶发失败，必须配置重试与退避。 |
| push2ex getTopicZTPool | Reject | 中高 | 中高 | 低 | transport可达但payload/business不可用或伪阳性。 |
| datacenter RPT_LIMIT_UP_POOL | Reject | 中高 | 中 | 低 | transport可达但payload/business不可用或伪阳性。 |

## 证据文件

- logs/market-reachability-matrix-20260417-173406.json
- logs/market-reachability-matrix-20260417-174513.json
- logs/market-audit-check-20260417-174822.json
- logs/market-audit-check-20260417-174823.json
- logs/market-audit-check-20260417-174824.json
- logs/market-audit-check-20260417-174825.json
- scripts/probe-market-reachability-matrix.py

## 立即实施建议（8条以内）

1. 将 breadth 主路径切换为 push2ex getTopicZDFenBu，并保留原链路灰度对照一周。
2. 将 push2 clist breadth 降级为 Reject，不再参与“完成态”判定。
3. 对 THS continuous_limit_up 统一接入重试（含指数退避与最大尝试次数）。
4. 对 push2 ulist turnover 增加波动阈值告警与短窗平滑，避免单点抖动误报。
5. 固化 bkzj dual merge（f3+f62）为板块主路径，单参数源仅作补位。
6. 在审计输出中显式拆分“source healthy”与“business complete”两个口径，避免语义混淆。
7. 将 recentSync 判定逻辑与 source status 做一致性校验，出现脱节时直接标注“半恢复”。
8. 明确从回退链移除上述两个 Reject 源，仅保留在离线探测清单。

## 风险与边界

- 风险1：盘中市场波动会放大链路抖动，短窗口探测可能高估不可用性。
- 风险2：第三方接口策略变更（限流、字段漂移）会造成 payload 契约突变。
- 风险3：source status 与 recentSync 语义脱节若不修复，会持续导致误判。
- 边界1：本结论基于 2026-04-17 当日探测样本，不等价于长期稳定性证明。
- 边界2：本报告聚焦可达性与业务可用性，不覆盖策略收益层面的有效性评估。
- 边界3：在连续盘中验证闭环完成前，结论应维持“部分恢复/可灰度”口径。
