# CCGS 技能库来源说明

> 拉取日期：2026-08-22
> 来源仓库：https://github.com/Donchitos/Claude-Code-Game-Studios
> 拉取方式：`git clone --depth 1`（经本地代理 127.0.0.1:7892）
> 内容：`.claude/skills/` 下全部 **73 个游戏开发流程技能**（每个技能一个 `SKILL.md`，共 0.9MB）

## 本次新增的 73 个技能（前缀分组速览）

| 分组 | 技能 |
|------|------|
| 流程启动/项目管理 | start, onboard, adopt, setup-engine, prototype, vertical-slice |
| 设计 | quick-design, design-system, design-review, art-bible, asset-spec, asset-audit, ux-design, ux-review, brainstorm |
| 架构/规划 | create-architecture, architecture-decision, architecture-review, map-systems, create-epics, create-stories, estimate, scope-check, gate-check |
| 开发/评审 | dev-story, story-readiness, story-done, code-review, review-all-gdds, consistency-check, propagate-design-change, reverse-document |
| QA/测试 | qa-plan, smoke-check, regression-suite, soak-test, test-setup, test-helpers, test-evidence-review, test-flakiness, playtest-report, balance-check |
| 发布/运维 | release-checklist, launch-checklist, day-one-patch, hotfix, patch-notes, changelog, milestone-review, retrospective, sprint-plan, sprint-status, tech-debt |
| 团队角色（team-*） | team-audio, team-combat, team-level, team-narrative, team-polish, team-qa, team-release, team-ui |
| 其他 | bug-report, bug-triage, changelog, content-audit, help, localize, perf-profile, project-stage-detect, security-audit, skill-improve, skill-test |

## 使用注意

1. **来源格式**：原为 Claude Code 技能（frontmatter 含 `allowed-tools`/`model`/`agent` 等 CCGS 专属字段）。Codely 读取其中的 `name`/`description` 即可正常发现与激活，其余字段为无害元数据。
2. **`.claude/docs` 引用**：部分技能步骤会读取 `.claude/docs/technical-preferences.md` 等仓库配套文档，本项目不存在这些文件——技能内已自带"未配置则跳过"的分支，激活时按缺省路径处理即可；如需完整配套文档，可从来源仓库 `.claude/docs/` 补拷。
3. **与本项目智能体架构的关系**：CCGS 的 team-* 技能与本项目的 8 agent（00/main_coder/...）体系是两套编排思路，并存不冲突——按需激活即可。
4. **更新方式**：重新克隆仓库后 robocopy 覆盖同名目录。
