#!/usr/bin/env bash
# =============================================================================
#  VinXiangQi 编译脚本（Windows / git-bash 或 MSYS2 bash）
#  ---------------------------------------------------------------------------
#  用途：在没有 Visual Studio 的机器上，用 Roslyn 编译器 + net48 参考程序集
#        直接编译本仓库（源码目录 VinXiangQi/）为可运行的 WinForms 程序。
#
#  用法（在仓库根目录执行）：
#    bash scripts/build_vinxq.sh      # 编译主程序（默认）
#    bash scripts/build_vinxq.sh build        # 同上
#    bash scripts/build_vinxq.sh test         # 编译 + 回归测试（全过=退出码 0；任一失败=非 0；测试源码缺失则跳过）
#    bash scripts/build_vinxq.sh all          # build + test
#    bash scripts/build_vinxq.sh publish      # 编译并组装发布目录（exe + dll + 配置）
#    bash scripts/build_vinxq.sh clean        # 只清理本脚本产生的构建产物
#    bash scripts/build_vinxq.sh help         # 显示帮助
#
#  可用环境变量覆盖默认值：
#    VXQ_TOOLS   编译工具根目录（内含 roslyn311/ 与 refasm48/）
#    VXQ_PACKAGES NuGet 包目录（默认 <仓库>/packages）
#    VXQ_OUT     构建输出目录（默认 <仓库>/../vxq_build_out，不进源码树）
#    VXQ_NETSTD  netstandard.dll 路径
#    VXQ_TESTDIR 回归测试源码目录（内含 harness.cs / fens.txt / jieqi_valid_test.cs；默认探测 <仓库>/tests）
#
#  说明：
#    · 所有中间产物与日志都写在 $VXQ_OUT，不会污染源码目录；
#    · 任何一步失败立即以非 0 退出码结束，并打印日志末尾供排查；
#    · 结束时打印 exe 路径、大小、编译时间与 MD5，便于核对版本。
# =============================================================================
set -u

# ---------- 定位仓库与脚本自身 ----------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# MSYS/git-bash 的 pwd 返回 POSIX 形式（/d/...）；csc.exe 等原生程序需要 D:/ 形式，这里统一转换
if command -v cygpath >/dev/null 2>&1; then
    SCRIPT_DIR="$(cygpath -m "$SCRIPT_DIR")"
fi
# 本脚本位于 <仓库>/scripts/，仓库根是它的上一级
ROOT="$(dirname "$SCRIPT_DIR")"
SRC="$ROOT/VinXiangQi"                       # 源码主目录（含 *.cs / *.resx / icon.ico）

# ---------- 可配置路径 ----------
TOOLS="${VXQ_TOOLS:-}"   # 用 VXQ_TOOLS 指向你的 Roslyn + net48 参考程序集目录
PKG="${VXQ_PACKAGES:-$ROOT/packages}"
OUT="${VXQ_OUT:-$(dirname "$ROOT")/vxq_build_out}"
NETSTD="${VXQ_NETSTD:-C:/Windows/Microsoft.NET/assembly/GAC_MSIL/netstandard/v4.0_2.0.0.0__cc7b13ffcd2ddd51/netstandard.dll}"

CSC="$TOOLS/roslyn311/tools/csc.exe"
REF="$TOOLS/refasm48/build/.NETFramework/v4.8"

# ---------- 回归测试源码目录（默认仓库 tests/，可用 VXQ_TESTDIR 覆盖）----------
TESTDIR=""
if [ -n "${VXQ_TESTDIR:-}" ]; then
    TESTDIR="$VXQ_TESTDIR"
    # 规范化为绝对路径：测试在本脚本 cwd（$OUT/tests）下运行，相对路径会失效；
    # 原生 exe 需要 Windows 形式（D:/...），这里用 cygpath 统一（有则用）。
    if [ -d "$TESTDIR" ]; then
        TESTDIR="$(cd "$TESTDIR" && pwd)"
        if command -v cygpath >/dev/null 2>&1; then TESTDIR="$(cygpath -m "$TESTDIR")"; fi
    fi
elif [ -d "$ROOT/tests" ]; then
    TESTDIR="$ROOT/tests"
fi

# ---------- 输出工具 ----------
hr()   { printf '%s\n' "----------------------------------------------------------------"; }
info() { printf '[INFO] %s\n' "$*"; }
ok()   { printf '[ OK ] %s\n' "$*"; }
warn() { printf '[WARN] %s\n' "$*"; }
die()  { printf '[FAIL] %s\n' "$*" >&2; exit 1; }

# ---------- 依赖检查 ----------
check_deps() {
    local missing=0
    if [ -z "$TOOLS" ]; then
        die "未指定编译工具目录：请用 VXQ_TOOLS=/path/to/vxq_build_tools 指定（内含 roslyn311/ 与 refasm48/）。详见 scripts/README.md"
    fi
    for f in "$CSC" "$REF/mscorlib.dll" "$SRC/VinXiangQi.csproj"; do
        [ -e "$f" ] || { warn "缺少：$f"; missing=1; }
    done
    [ -e "$NETSTD" ] || { warn "缺少 netstandard.dll：$NETSTD（可用 VXQ_NETSTD 指定）"; missing=1; }
    [ -e "$PKG" ] || { warn "缺少 NuGet 包目录：$PKG（可用 VXQ_PACKAGES 指定，或先做 NuGet 还原）"; missing=1; }
    # 关键包抽检（缺任何一个都无法编译）
    for d in Microsoft.ML.OnnxRuntime.Managed Yolov5Net Newtonsoft.Json; do
        ls "$PKG"/$d.* >/dev/null 2>&1 || { warn "缺少包：$d"; missing=1; }
    done
    [ "$missing" = 0 ] || die "依赖不完整，请检查 VXQ_TOOLS / VXQ_PACKAGES 后重试。"
    ok "依赖检查通过"
}

# ---------- 公共编译参数 ----------
build_refs() {
    REFS="-r:$REF/mscorlib.dll -r:$REF/System.dll -r:$REF/System.Core.dll -r:$REF/System.Data.dll"
    REFS="$REFS -r:$REF/System.Drawing.dll -r:$REF/System.Management.dll -r:$REF/System.Numerics.dll"
    REFS="$REFS -r:$REF/System.Security.dll -r:$REF/System.Web.dll -r:$REF/System.Xaml.dll"
    REFS="$REFS -r:$REF/System.Xml.dll -r:$REF/System.Xml.Linq.dll -r:$REF/System.Data.DataSetExtensions.dll"
    REFS="$REFS -r:$REF/Microsoft.CSharp.dll -r:$REF/System.Deployment.dll -r:$REF/System.Net.Http.dll"
    REFS="$REFS -r:$REF/System.Windows.Forms.dll -r:$NETSTD"

    PKGS="-r:$PKG/Microsoft.Data.Sqlite.Core.6.0.6/lib/netstandard2.0/Microsoft.Data.Sqlite.dll"
    PKGS="$PKGS -r:$PKG/Microsoft.ML.OnnxRuntime.Managed.1.11.0/lib/netstandard2.0/Microsoft.ML.OnnxRuntime.dll"
    PKGS="$PKGS -r:$PKG/Newtonsoft.Json.13.0.4/lib/net45/Newtonsoft.Json.dll"
    PKGS="$PKGS -r:$PKG/SQLitePCLRaw.bundle_e_sqlite3.2.0.6/lib/net461/SQLitePCLRaw.batteries_v2.dll"
    PKGS="$PKGS -r:$PKG/SQLitePCLRaw.core.2.0.6/lib/netstandard2.0/SQLitePCLRaw.core.dll"
    PKGS="$PKGS -r:$PKG/SQLitePCLRaw.provider.e_sqlite3.2.0.6/lib/netstandard2.0/SQLitePCLRaw.provider.e_sqlite3.dll"
    PKGS="$PKGS -r:$PKG/System.Buffers.4.5.1/lib/net461/System.Buffers.dll"
    PKGS="$PKGS -r:$PKG/System.Drawing.Common.5.0.3/lib/net461/System.Drawing.Common.dll"
    PKGS="$PKGS -r:$PKG/System.Memory.4.5.4/lib/net461/System.Memory.dll"
    PKGS="$PKGS -r:$PKG/System.Numerics.Vectors.4.5.0/lib/net46/System.Numerics.Vectors.dll"
    PKGS="$PKGS -r:$PKG/System.Runtime.CompilerServices.Unsafe.6.0.0/lib/net461/System.Runtime.CompilerServices.Unsafe.dll"
    PKGS="$PKGS -r:$PKG/System.ValueTuple.4.5.0/lib/net47/System.ValueTuple.dll"
    PKGS="$PKGS -r:$PKG/Yolov5Net.1.0.9/lib/netstandard2.0/Yolov5Net.Scorer.dll"
}

# 保证文件带 UTF-8 BOM（Roslyn 对无 BOM 的中文源码会按本地代码页误读）
ensure_bom() {
    local f="$1"
    [ -f "$f" ] || return 0
    if [ "$(head -c3 "$f" | od -An -tx1 | tr -d ' \n')" != "efbbbf" ]; then
        printf '\xef\xbb\xbf' | cat - "$f" > "$f.bom" && mv "$f.bom" "$f"
        warn "已为 $(basename "$f") 补 UTF-8 BOM"
    fi
}

# ---------- 步骤 1：resx → resources ----------
# resx2res.cs：本机无 resgen 时用 ResXResourceReader 做转换（脚本内嵌生成，避免外部依赖）
write_resx_helper() {
    printf '\xef\xbb\xbf' > "$OUT/tools/resx2res.cs"
    cat >> "$OUT/tools/resx2res.cs" <<'RESXEOF'
using System;
using System.Collections;
using System.IO;
using System.Resources;

// resx → .resources 转换器（替代 resgen，本机无 VS/SDK 时的资源编译路径）
// 用法: resx2res.exe <in.resx> <out.resources>
class Resx2Res
{
    static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("usage: resx2res.exe <in.resx> <out.resources>");
            return 1;
        }
        string inPath = args[0], outPath = args[1];
        try
        {
            using (ResXResourceReader reader = new ResXResourceReader(inPath))
            {
                reader.BasePath = Path.GetDirectoryName(Path.GetFullPath(inPath)) + "\\";
                using (ResourceWriter writer = new ResourceWriter(outPath))
                {
                    int n = 0;
                    foreach (DictionaryEntry e in reader)
                    {
                        writer.AddResource((string)e.Key, e.Value);
                        n++;
                    }
                    Console.WriteLine("OK  " + n.ToString() + "  " + Path.GetFileName(outPath));
                }
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERR " + inPath + " : " + ex.GetType().Name + " : " + ex.Message);
            return 2;
        }
    }
}
RESXEOF
}

step_resx() {
    mkdir -p "$OUT/res" "$OUT/tools" "$OUT/logs"
    write_resx_helper

    info "[1/3] 编译 resx 转换器"
    "$CSC" -nologo -utf8output -langversion:7.3 -nostdlib \
        -r:"$REF/mscorlib.dll" -r:"$REF/System.dll" -r:"$REF/System.Core.dll" \
        -r:"$REF/System.Drawing.dll" -r:"$REF/System.Windows.Forms.dll" \
        -out:"$OUT/tools/resx2res.exe" "$OUT/tools/resx2res.cs" > "$OUT/logs/resx2res.log" 2>&1 \
        || { tail -20 "$OUT/logs/resx2res.log"; die "resx2res 编译失败"; }

    info "[1/3] 转换全部 .resx → .resources"
    local n=0
    while IFS= read -r f; do
        local rel="${f#$SRC/}"
        local base="${rel%.resx}"
        local logical="VinXiangQi.${base//\//.}"
        "$OUT/tools/resx2res.exe" "$f" "$OUT/res/$logical.resources" \
            || die "resx 转换失败：$rel"
        n=$((n+1))
    done < <(find "$SRC" -name '*.resx' | sort)
    ok "已转换 $n 个 .resx"
}

# ---------- 步骤 2：编译主程序 ----------
step_build() {
    build_refs
    local resargs=() r
    for r in "$OUT"/res/*.resources; do
        [ -e "$r" ] || continue
        resargs+=("-resource:$r,$(basename "$r")")
    done
    [ "${#resargs[@]}" -gt 0 ] || die "没有可嵌入的 resources，先执行 resx 步骤"

    local files
    files=$(find "$SRC" -name '*.cs' -not -path '*obj*' -not -path '*bin*')
    local nsrc
    nsrc=$(printf '%s\n' "$files" | grep -c . )
    info "[2/3] 编译主程序（$nsrc 个源文件，${#resargs[@]} 个资源）"

    rm -f "$OUT/VinXiangQi.exe"
    # shellcheck disable=SC2086
    "$CSC" -nologo -utf8output -target:winexe -platform:x86 -langversion:7.3 -unsafe -nostdlib \
        -win32icon:"$SRC/icon.ico" \
        -win32manifest:"$SRC/app.manifest" \
        $REFS $PKGS "${resargs[@]}" \
        -out:"$OUT/VinXiangQi.exe" $files > "$OUT/logs/build.log" 2>&1
    local rc=$?

    local errs
    errs=$(grep -c 'error CS' "$OUT/logs/build.log" || true)
    if [ "$rc" != 0 ] || [ ! -s "$OUT/VinXiangQi.exe" ]; then
        grep -E 'error CS' "$OUT/logs/build.log" | head -30
        die "编译失败（csc 退出码 $rc，错误 $errs 条）"
    fi
    if [ "$errs" != 0 ]; then
        grep -E 'error CS' "$OUT/logs/build.log" | head -30
        die "存在 $errs 条编译错误"
    fi
    ok "编译通过（0 错误）"
}

# ---------- 步骤 3：可选回归测试 ----------
step_test() {
    info "[3/3] 回归测试"
    mkdir -p "$OUT/tests" "$OUT/logs"
    if [ -z "$TESTDIR" ] || [ ! -d "$TESTDIR" ]; then
        warn "未找到回归测试源码目录，跳过（如需运行，用 VXQ_TESTDIR 指定）"
        return 0
    fi
    # .NET 程序集解析只看 exe 所在目录：先把回归所需运行库与配置复制到 tests/，
    # 否则回归 exe 会抛 FileNotFoundException（Newtonsoft.Json 等）。
    # 来源①：TESTDIR 自带（本地开发目录）；来源②：缺的从 $PKG 补齐（仓库 tests/ 等干净场景、CI）。
    local d
    for d in "$TESTDIR"/*.dll; do
        [ -e "$d" ] && cp -f "$d" "$OUT/tests/" 2>/dev/null
    done
    for d in "$TESTDIR"/*.config; do
        [ -e "$d" ] && cp -f "$d" "$OUT/tests/" 2>/dev/null
    done
    local test_dll
    for test_dll in Microsoft.Data.Sqlite Microsoft.ML.OnnxRuntime Newtonsoft.Json \
                    SQLitePCLRaw.batteries_v2 SQLitePCLRaw.core SQLitePCLRaw.provider.e_sqlite3 \
                    System.Buffers System.Drawing.Common System.Memory System.Numerics.Vectors \
                    System.Runtime.CompilerServices.Unsafe System.ValueTuple Yolov5Net.Scorer; do
        find "$PKG" -name "$test_dll.dll" -path '*net*' 2>/dev/null | head -1 | while read -r p; do
            [ -f "$OUT/tests/$test_dll.dll" ] || cp -f "$p" "$OUT/tests/" 2>/dev/null
        done
    done
    # 原生库（win-x86）：个别测试可能触发原生加载
    find "$PKG" -path '*win-x86*' \( -name 'onnxruntime.dll' -o -name 'onnxruntime_providers_shared.dll' -o -name 'e_sqlite3.dll' \) 2>/dev/null | while read -r p; do
        [ -f "$OUT/tests/$(basename "$p")" ] || cp -f "$p" "$OUT/tests/" 2>/dev/null
    done
    build_refs
    local files
    files=$(find "$SRC" -name '*.cs' -not -path '*obj*' -not -path '*bin*')
    local ran=0 failed=0

    if [ -f "$TESTDIR/harness.cs" ] && [ -f "$TESTDIR/fens.txt" ]; then
        ensure_bom "$TESTDIR/harness.cs"
        info "  编译回归 harness"
        # shellcheck disable=SC2086
        "$CSC" -nologo -utf8output -target:exe -langversion:7.3 -unsafe -main:Harness -nostdlib \
            $REFS $PKGS -out:"$OUT/tests/harness.exe" $files "$TESTDIR/harness.cs" \
            > "$OUT/logs/harness.log" 2>&1
        if [ $? -eq 0 ]; then
            mkdir -p "$OUT/tests" 2>/dev/null
            ( cd "$OUT/tests" && ./harness.exe "$TESTDIR/fens.txt" > "$OUT/logs/harness_run.log" 2>&1 )
            local hrc=$?
            if grep -q 'ALL TESTS PASSED' "$OUT/logs/harness_run.log"; then ok "harness：ALL TESTS PASSED"
            else tail -20 "$OUT/logs/harness_run.log"; warn "harness 未全通过（退出码 $hrc）"; failed=$((failed+1)); fi
            ran=$((ran+1))
        else
            grep -E 'error CS' "$OUT/logs/harness.log" | head -10
            warn "harness 编译失败"; failed=$((failed+1))
        fi
    else
        warn "  $TESTDIR 下没有 harness.cs/fens.txt，跳过"
    fi

    if [ -f "$TESTDIR/jieqi_valid_test.cs" ]; then
        ensure_bom "$TESTDIR/jieqi_valid_test.cs"
        info "  编译揭棋合法性回归"
        # shellcheck disable=SC2086
        "$CSC" -nologo -utf8output -target:exe -langversion:7.3 -unsafe -main:JieqiValidTest -nostdlib \
            $REFS $PKGS -out:"$OUT/tests/jieqi_valid_test.exe" $files "$TESTDIR/jieqi_valid_test.cs" \
            > "$OUT/logs/jvt.log" 2>&1
        if [ $? -eq 0 ]; then
            local a="$TESTDIR/fens_legal.txt" b="$TESTDIR/fens_junk.txt"
            ( cd "$OUT/tests" && ./jieqi_valid_test.exe "$a" "$b" > "$OUT/logs/jvt_run.log" 2>&1 )
            if grep -q 'ALL JIEQI VALID TESTS PASSED' "$OUT/logs/jvt_run.log"; then ok "揭棋合法性：全部通过"
            else tail -20 "$OUT/logs/jvt_run.log"; warn "揭棋合法性回归未全通过"; failed=$((failed+1)); fi
            ran=$((ran+1))
        else
            grep -E 'error CS' "$OUT/logs/jvt.log" | head -10
            warn "揭棋合法性测试编译失败"; failed=$((failed+1))
        fi
    else
        warn "  $TESTDIR 下没有 jieqi_valid_test.cs，跳过"
    fi

    if [ "$ran" -eq 0 ]; then warn "没有任何回归测试被执行"; failed=$((failed+1)); fi
    if [ "$failed" -gt 0 ]; then
        warn "回归测试未全通过（$failed 项）—— 详见 $OUT/logs/"
        return 1
    fi
    return 0
}

# ---------- 发布目录组装 ----------
step_publish() {
    info "组装发布目录"
    local rel="$OUT/publish"
    rm -rf "$rel"; mkdir -p "$rel/Models" "$rel/Engines" "$rel/OpenBooks" "$rel/Solutions"
    cp -f "$OUT/VinXiangQi.exe" "$rel/VinXiangQi.exe"
    # 应用配置：源码里的 App.config 即 exe.config（.NET 命名规则）
    if [ -f "$SRC/App.config" ]; then
        cp -f "$SRC/App.config" "$rel/VinXiangQi.exe.config"
    else
        warn "未找到 App.config（可忽略）"
    fi
    # 运行库：从 NuGet 包中取出与 exe 同目录运行的 DLL
    local dll
    for dll in Microsoft.Data.Sqlite Microsoft.ML.OnnxRuntime Newtonsoft.Json \
               SQLitePCLRaw.batteries_v2 SQLitePCLRaw.core SQLitePCLRaw.provider.e_sqlite3 \
               System.Buffers System.Drawing.Common System.Memory System.Numerics.Vectors \
               System.Runtime.CompilerServices.Unsafe System.ValueTuple Yolov5Net.Scorer; do
        find "$PKG" -name "$dll.dll" -path '*net*' 2>/dev/null | head -1 | while read -r p; do
            [ -n "$p" ] && cp -f "$p" "$rel/" 2>/dev/null
        done
    done
    cp -f "$ROOT/Models/jieqi.onnx" "$rel/Models/" 2>/dev/null || true
    # 原生运行库（onnxruntime / sqlite3）优先从 packages 找
    find "$PKG" -name 'onnxruntime.dll' -o -name 'e_sqlite3.dll' -o -name 'onnxruntime_providers_shared.dll' 2>/dev/null \
        | while read -r p; do cp -f "$p" "$rel/" 2>/dev/null; done
    ok "发布目录：$rel"
    warn "Models/Engines/OpenBooks 下的模型与引擎需按需自行放入（体积大，不入库）"
}

# ---------- 汇总 ----------
summary() {
    local exe="$OUT/VinXiangQi.exe"
    hr
    if [ -f "$exe" ]; then
        local size md5 when
        size=$(stat -c%s "$exe")
        md5=$(md5sum "$exe" | cut -d' ' -f1)
        when=$(stat -c '%y' "$exe" | cut -d'.' -f1)
        ok "构建产物就绪"
        printf '  路径：%s\n' "$exe"
        printf '  大小：%s 字节\n' "$size"
        printf '  时间：%s\n' "$when"
        printf '  MD5 ：%s\n' "$md5"
    else
        warn "构建目录中没有 exe"
    fi
    printf '  日志：%s/logs/\n' "$OUT"
    hr
}

# ---------- 清理 ----------
step_clean() {
    info "清理本脚本产生的构建产物（仅 $OUT）"
    if [ -d "$OUT" ]; then
        rm -rf "$OUT"
        ok "已删除：$OUT"
    else
        info "无需清理：$OUT 不存在"
    fi
    # 顺带清掉源码目录里可能残留的构建中间物（obj/bin 不属于本仓库，出现即垃圾）
    local n=0 d
    for d in "$SRC/obj" "$SRC/bin"; do
        if [ -d "$d" ]; then rm -rf "$d"; n=$((n+1)); fi
    done
    [ "$n" -gt 0 ] && ok "已删除源码目录残留的 obj/bin（$n 个）" || info "源码目录无 obj/bin 残留"
}

# ---------- 入口 ----------
CMD="${1:-build}"
case "$CMD" in
    build)
        hr; info "VinXiangQi 编译（仓库：$ROOT）"; hr
        check_deps; step_resx; step_build; summary ;;
    test)
        hr; info "VinXiangQi 编译 + 回归测试"; hr
        check_deps; step_resx; step_build
        step_test || { summary; exit 3; }
        summary ;;
    all)
        hr; info "VinXiangQi 全流程（编译 + 测试 + 发布）"; hr
        check_deps; step_resx; step_build
        step_test || { summary; exit 3; }
        step_publish; summary ;;
    publish)
        hr; info "VinXiangQi 编译 + 发布"; hr
        check_deps; step_resx; step_build; step_publish; summary ;;
    clean)
        step_clean ;;
    help|-h|--help)
        grep '^#' "${BASH_SOURCE[0]}" | head -n 40 | sed 's/^# \{0,1\}//' ;;
    *)
        die "未知命令：$CMD（可用：build / test / all / publish / clean / help）" ;;
esac
