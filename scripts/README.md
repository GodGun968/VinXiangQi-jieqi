# 手动构建（无 Visual Studio）

本目录的 `build_vinxq.sh` 用于在**没有装 Visual Studio** 的 Windows 机器上，直接用
Roslyn 编译器 + .NET Framework 4.8 参考程序集，把 `VinXiangQi/` 源码编译成可运行的
WinForms 程序。

它和仓库里的 GitHub Actions（`.github/workflows/build.yml`）是**两条独立路径**：

| | 手动构建（本脚本） | GitHub Actions |
|---|---|---|
| 用途 | 本机快速迭代、跑回归测试 | 出 Release 产物 + 回归测试（regression 任务） |
| 依赖 | Roslyn + net48 参考程序集（本机目录） | MSBuild + `nuget restore` |
| 回归测试 | **会跑**（`test` / `all`） | **会跑**（regression 任务：拉取工具链后调用同一脚本） |
| 模型/引擎 | 从本机 `Models/`、`Engines/` 取 | 一律剔除，只预建空目录 |

---

## 一、环境准备

### 1. 运行时要求

- Windows（脚本用 git-bash / MSYS2 bash 执行）
- .NET Framework 4.8（Windows 10/11 自带）
- `curl` 不需要；**不联网也能构建**（依赖包放在仓库 `packages/`）

### 2. 编译工具链（必做）

脚本需要两个东西，放在**同一个目录**下：

```
<你的工具目录>/
├── roslyn311/
│   └── tools/csc.exe          # Roslyn 3.11 编译器
└── refasm48/
    └── build/.NETFramework/v4.8/
        └── mscorlib.dll …     # net48 参考程序集
```

获取方式（任选其一）：

- 从 NuGet 包解出：`Microsoft.Net.Compilers`（csc）与
  `Microsoft.NETFramework.ReferenceAssemblies.net48`（参考程序集）；
- 或从已装 Visual Studio 的机器复制 `MSBuild/Current/Bin/Roslyn/csc.exe` 与
  `Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8/`。

> 脚本**不会**自动下载任何东西，也不会往工具目录外写文件。

### 3. 指定工具目录

脚本通过环境变量 `VXQ_TOOLS` 定位工具链；**不设它会直接报错退出**（提示写明需要什么）：

```bash
export VXQ_TOOLS="D:/path/to/vxq_build_tools"
```

---

## 二、使用方法

所有命令**在仓库根目录执行**：

```bash
bash scripts/build_vinxq.sh              # 编译主程序（默认，等同 build）
bash scripts/build_vinxq.sh build        # 同上
bash scripts/build_vinxq.sh test         # 编译 + 回归测试（全过=退出码 0）
bash scripts/build_vinxq.sh all          # build + test + publish
bash scripts/build_vinxq.sh publish      # 编译 + 组装发布目录
bash scripts/build_vinxq.sh clean        # 清理本脚本产生的构建产物
bash scripts/build_vinxq.sh help         # 帮助
```

### 各命令做什么

| 命令 | 步骤 |
|---|---|
| `build` | 检查依赖 → 全部 `.resx` 转 `.resources` → 编译主程序 |
| `test` | `build` + 编译并运行回归测试（harness、揭棋合法性） |
| `all` | `build` + `test` + `publish` |
| `publish` | `build` + 组装可分发目录（exe + 运行库 + 配置 + 空 Models/Engines） |
| `clean` | 删除 `$VXQ_OUT` 与源码目录里残留的 `obj/`、`bin/` |

---

## 三、环境变量

| 变量 | 默认值 | 说明 |
|---|---|---|
| `VXQ_TOOLS` | *（必填，无默认）* | 编译工具根目录，内含 `roslyn311/` 与 `refasm48/` |
| `VXQ_PACKAGES` | `<仓库>/packages` | NuGet 包目录 |
| `VXQ_OUT` | `<仓库>/../vxq_build_out` | 构建输出目录（**默认在仓库外**，不污染源码树） |
| `VXQ_NETSTD` | 系统 GAC 里的 `netstandard.dll` | 若你的 GAC 路径不同，指定它 |
| `VXQ_TESTDIR` | 自动探测 `<仓库>/tests` | 回归测试源码目录（见下） |

示例：

```bash
VXQ_TOOLS="D:/tools/vxq" VXQ_OUT="D:/tmp/out" bash scripts/build_vinxq.sh test
```

---

## 四、回归测试（`test` / `all`）

`test` 会去 `VXQ_TESTDIR` 里找测试源码并运行：

- `harness.cs` + `fens.txt` → FEN 往返与规则单元测试（全过时输出 `ALL TESTS PASSED`）
- `jieqi_valid_test.cs` + `fens_legal.txt` + `fens_junk.txt` → 揭棋合法性回归
  （全过时输出 `ALL JIEQI VALID TESTS PASSED`）

**找不到测试目录时会跳过并给出 WARN，不算失败**——所以只编译不测试用 `build` 即可。

> 回归测试源码已随仓库提供（`tests/` 目录，默认自动探测）；也可以 `VXQ_TESTDIR=/path/to/tests`
> 指定其它目录。GitHub Actions 的 `regression` 任务即用仓库 `tests/` 跑同一套回归。

---

## 五、输出与日志

默认输出到 `../vxq_build_out/`（仓库的上一级）：

```
vxq_build_out/
├── VinXiangQi.exe        # 主程序
├── res/                  # 转换后的 .resources
├── tools/resx2res.exe    # 脚本自带的 resx 转换器
├── tests/                # 回归测试 exe
├── logs/                 # 全部日志
│   ├── build.log         # 编译输出
│   ├── harness_run.log   # 回归测试结果
│   └── jvt_run.log       # 揭棋合法性结果
└── publish/              # publish 命令产出的发布目录
```

构建结束时脚本会打印：

```
[ OK ] 构建产物就绪
  路径：.../vxq_build_out/VinXiangQi.exe
  大小：13620736 字节
  时间：...
  MD5 ：...
```

---

## 六、常见问题

### 输出 exe 被占用（`CS2012` / `Device or resource busy`）

程序正在运行，Windows 锁住了 exe。**这不是源码错误**。两个办法：

1. 关掉正在运行的程序，重新构建；
2. 用备用输出目录构建：

```bash
VXQ_OUT="D:/tmp/out2" bash scripts/build_vinxq.sh test
```

### 缺少依赖包

```
[WARN] 缺少 NuGet 包目录：.../packages
```

在仓库根执行 `nuget restore VinXiangQi.sln`，或把 `packages/` 放回原位。

### 中文源码乱码 / 编译报奇怪的字符错误

Roslyn 对**无 BOM 的中文源码**会按本地代码页误读。脚本在编译回归测试前会自动补 BOM；
如果自己加新源文件，请确保保存为 **UTF-8 with BOM**。

### `未指定编译工具目录`

说明没设 `VXQ_TOOLS`，或设的路径里没有 `roslyn311/tools/csc.exe`。参见「一、环境准备」。

### 想让输出落在别的盘

```bash
VXQ_OUT="E:/build/vxq" bash scripts/build_vinxq.sh build
```

---

## 七、和 GitHub Actions 的关系

- Actions 分两个任务：`build`（MSBuild + `nuget restore` 构建 Release，产物剔除 `.onnx`/`.pdb`，
  预建空的 `Models/`、`Engines/`）与 `regression`（拉取固定版本 Roslyn + net48 参考程序集到 `.tools/`，
  随后执行本脚本的 `test`——与本地同一条路径）；
- 本脚本用 csc 直接编译，**产物包含本机 `Models/` 里的模型**（publish 时复制）；
- 两者产出的 exe 不保证字节一致（编译器与参数不同），但都能运行；
- 改源码后两条路径都应能通过，改动前建议本地先跑一次
  `bash scripts/build_vinxq.sh test`。
