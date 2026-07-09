# 导表工具（Excel Export Tool）

> **版本**: v1.0  
> **最后更新**: 2026-07-08  
> **用途**: 将策划维护的 Excel 数值表转换为客户端 JSON + 服务器 TSV

---

## 一、背景

《双生迷城》项目中，游戏配置数据（灾难参数、技能属性、天赋数值等）由策划在 Excel 中维护。需要一个导表工具将 Excel 数据转换为程序可用的格式：

```
策划 Excel → 导表工具 → 客户端 JSON + 服务器 TSV
```

---

## 二、快速开始

### 2.1 安装依赖

```bash
cd GameDesign/tool
pip install -r requirements.txt
```

### 2.2 导出全部

```bash
python src/main.py export
```

### 2.3 导出结果

```
tool/output/
├── client/         # JSON 文件
│   ├── disaster.json
│   └── skill.json
└── server/         # TSV 文件
    ├── disaster.tsv
    └── skill.tsv
```

---

## 三、目录结构

```
GameDesign/
├── Excel/                          # 策划维护的 Excel 文件
│   ├── disaster.xlsx               # 灾难配置表
│   ├── skill.xlsx                  # 技能配置表
│   └── ...
│
└── tool/                           # 导表工具
    ├── README.md                   # 本文档
    ├── requirements.txt            # Python 依赖
    ├── config/
    │   └── settings.json           # 工具配置
    ├── src/                        # 源码
    │   ├── main.py                 # 入口
    │   ├── excel_reader.py         # Excel 读取
    │   ├── json_exporter.py        # JSON 导出
    │   ├── tsv_exporter.py         # TSV 导出
    │   ├── validator.py            # 数据校验
    │   └── utils.py                # 工具函数
    ├── bin/
    │   ├── export.bat              # Windows 一键导出
    │   └── export.sh               # Linux/Mac 一键导出
    └── output/                     # 导出产物（gitignore）
        ├── client/
        └── server/
```

---

## 四、Excel 表格规范

### 4.1 表格结构

每张 Excel 表格必须遵循以下结构：

```
行1：字段名（英文，snake_case）
行2：C/S/CS 标记（控制导出范围）
行3：类型定义（控制数据类型和校验）
行4：字段描述（可选，用于生成文档）
行5起：数据行
```

### 4.2 标记说明（行2）

标记决定该字段导出到哪一端：

| 标记 | 含义 | 说明 |
|------|------|------|
| `C` | Client Only | 仅导出到客户端（UI显示名、特效ID、音效ID等） |
| `S` | Server Only | 仅导出到服务器（验证参数、反作弊阈值等） |
| `CS` | Both | 双端都导出（HP、伤害值、合成配方、位置坐标等） |

### 4.3 类型定义（行3）

类型定义决定数据的格式和校验规则。可与 C/S/CS 标记组合使用。

#### 基础类型

| 类型 | 说明 | Excel 值示例 | JSON 输出 |
|------|------|-------------|-----------|
| `int` | 整数 | `3` | `3` |
| `float` | 浮点数 | `3.5` | `3.5` |
| `string` | 字符串 | `熔岩潮` | `"熔岩潮"` |
| `bool` | 布尔 | `true` / `false` / `1` / `0` | `true` / `false` |

#### 枚举类型 `enum`

格式：`enum[值1,值2,值3,...]`

限制策划只能填入预定义的值，否则校验报错。

```excel
| category                                                    |
|-------------------------------------------------------------|
| CS:enum[element,environment,time,perception,physical,mechanism] |
| element    |  ← 合法
| fire       |  ← 非法，校验报错
```

#### 列表类型 `list`

格式：`list[元素类型]`

单元格内多个值用逗号 `,` 分隔。

| 类型 | Excel 值 | JSON 输出 |
|------|---------|-----------|
| `list[int]` | `1,2,3` | `[1, 2, 3]` |
| `list[float]` | `1.0,2.5,3.7` | `[1.0, 2.5, 3.7]` |
| `list[string]` | `a,b,c` | `["a", "b", "c"]` |
| `list[enum[v1,v2]]` | `v1,v2` | `["v1", "v2"]` |
| `list[list[int]]` | `1,2;3,4`（分号分隔内层） | `[[1,2], [3,4]]` |

#### 组合格式

类型与标记组合使用，格式为 `{标记}:{类型}`：

| 格式 | 说明 |
|------|------|
| `CS:int` | 双端共用，整数 |
| `C:string` | 仅客户端，字符串 |
| `S:enum[v1,v2]` | 仅服务器，枚举 |
| `CS:list[float]` | 双端共用，浮点列表 |

### 4.4 注释功能

使用 `#` 前缀标记注释，工具自动跳过。

#### Sheet 注释

Sheet 名称以 `#` 开头，整个 Sheet 跳过。

```
├── Sheet1          ← 正常处理
├── #Draft          ← 跳过
└── #Archive        ← 跳过
```

#### 列注释

字段名以 `#` 开头，整列不导出。

```
| id    | name    | #备注      | hpDamage |
|-------|---------|------------|----------|
| CS    | CS      | CS         | CS       |
| E1    | 熔岩潮  | 设计说明   | 3        |
```

导出结果不包含 `#备注` 列。

#### 行注释

第一列（主键列）值以 `#` 开头，整行跳过。

```
| id    | name     | hpDamage |
|-------|----------|----------|
| CS    | CS       | CS       |
| E1    | 熔岩潮   | 3        |
| #TODO | 旧版本   | 5        |  ← 跳过
| E2    | 冰封领域 | 2        |
```

### 4.5 完整示例

```excel
| id    | name     | nameClient | category                    | environment               | hpDamage | duration | intensityCurve | skills     |
|-------|----------|------------|-----------------------------|---------------------------|----------|----------|----------------|------------|
| CS    | CS       | C          | CS:enum[element,environment]| CS:enum[volcano,flood]    | CS:int   | CS:int   | CS:list[float] | CS:list[string] |
| 灾难ID| 内部名称 | 显示名称   | 灾难分类                    | 庇护环境                  | 每秒伤害 | 持续时间 | 强度曲线       | 关联技能   |
|-------|----------|------------|-----------------------------|---------------------------|----------|----------|----------------|------------|
| E1    | lava_tide| 熔岩潮     | element                     | volcano                   | 3        | 20       | 0.3,0.6,1.0,0.8 | skill_a,skill_b |
| E2    | ice_field| 冰封领域   | element                     | blizzard                  | 2        | 20       | 0.2,0.5,1.0,0.7 | skill_c    |
```

> 第一列自动作为主键，导出时以此为 JSON key。

---

## 五、配置文件

### config/settings.json

```json
{
  "input": "../Excel",
  "output": "./output",
  "comment_prefix": "#",
  "header_row": 1,
  "marker_row": 2,
  "type_row": 3,
  "desc_row": 4,
  "data_start_row": 5
}
```

| 字段 | 说明 | 默认值 |
|------|------|--------|
| input | Excel 文件目录 | `../Excel` |
| output | 导出输出目录 | `./output` |
| comment_prefix | 注释前缀 | `#` |
| header_row | 字段名所在行 | 1 |
| marker_row | C/S/CS 标记所在行 | 2 |
| type_row | 类型定义所在行 | 3 |
| desc_row | 字段描述所在行 | 4 |
| data_start_row | 数据起始行 | 5 |

---

## 六、导出规则

### 6.1 自动扫描

工具自动扫描 `input` 目录下所有 `.xlsx` 文件：

1. 文件名以 `#` 开头 → 跳过整个文件
2. Sheet 名以 `#` 开头 → 跳过该 Sheet
3. 每个文件处理所有非 `#` 的 Sheet（一个 Sheet 导出为一个文件）
4. 输出文件名 = Sheet 名

### 6.2 客户端 JSON

输出路径：`output/client/{Sheet名}.json`

- 仅导出标记为 `C` 或 `CS` 的字段
- 第一列作为主键，输出为 JSON 对象

```json
{
  "E1": {
    "id": "E1",
    "nameClient": "熔岩潮",
    "category": "element",
    "hpDamage": 3,
    "duration": 20,
    "intensityCurve": [0.3, 0.6, 1.0, 0.8],
    "skills": ["skill_a", "skill_b"]
  }
}
```

### 6.3 服务器 TSV

输出路径：`output/server/{Sheet名}.tsv`

- 仅导出标记为 `S` 或 `CS` 的字段
- Tab 分隔
- 第一行：字段名
- 第二行：字段类型
- 第三行起：数据

```
id	name	category	environment	hpDamage	duration	intensityCurve	skills
string	string	enum[element,environment]	enum[volcano,flood]	int	int	list[float]	list[string]
E1	lava_tide	element	volcano	3	20	0.3,0.6,1.0,0.8	skill_a,skill_b
E2	ice_field	element	blizzard	2	20	0.2,0.5,1.0,0.7	skill_c
```

---

## 七、校验规则

导出前自动执行校验，校验失败则中止导出。

### 7.1 校验项

| 校验项 | 规则 | 失败行为 |
|--------|------|---------|
| 主键唯一 | 第一列不能有重复值 | ERROR，中止导出 |
| 必填字段 | 主键字段和所有 CS 字段不能为空 | ERROR，中止导出 |
| 类型匹配 | 字段值与行3类型定义一致 | ERROR，中止导出 |
| 枚举值 | `enum` 类型的值必须在定义范围内 | ERROR，中止导出 |
| 列表格式 | `list` 类型用逗号分隔，内层用分号 | WARN，跳过该行 |

### 7.2 输出格式

```
[ERROR] disaster.xlsx: 主键 "E1" 重复（第5行、第12行）
[WARN]  skill.xlsx: 字段 "cooldown" 第8行值为空，已跳过
[OK]    disaster.xlsx: 校验通过，共35条数据
```

---

## 八、命令行

```bash
# 导出全部表
python src/main.py export

# 导出指定表（逗号分隔）
python src/main.py export --table disaster
python src/main.py export --table disaster,skill

# 仅校验不导出
python src/main.py validate
python src/main.py validate --table disaster

# 指定输出目录
python src/main.py export --output ./output
```

---

## 九、依赖

```
openpyxl>=3.1.0
```

安装：

```bash
pip install -r requirements.txt
```

---

## 十、扩展计划（v2.0）

| 功能 | 说明 |
|------|------|
| ScriptableObject 代码生成 | 自动生成 C# 配置类定义 |
| 增量导出 | 仅导出修改过的表 |
| 多语言支持 | Excel 多语言 sheet，导出多语言 JSON |
| 数据预览 | 生成 HTML 预览页面 |
| Git 钩子 | 提交时自动校验 |
