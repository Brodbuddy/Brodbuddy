#!/usr/bin/env python3
# .github/scripts/generate_test_summary.py

import os
import sys
import xml.etree.ElementTree as ET
import traceback

from datetime import datetime, timedelta
from pathlib import Path
from typing import NamedTuple

TRX_REPORT_FILENAME = "test-results.trx"
TRX_REPORT_DIR = Path("TestResults")
GITHUB_SUMMARY_ENV_VAR = "GITHUB_STEP_SUMMARY"


class TestSummary(NamedTuple):
    total: int = 0
    executed: int = 0
    passed: int = 0
    failed: int = 0
    skipped: int = 0
    pass_percentage: float = 0.0
    duration_str: str = "N/A"


def _get_xml_namespace(element: ET.Element) -> str:
    if "}" in element.tag:
        return element.tag.split("}")[0] + "}"
    return ""


def format_timedelta(delta: timedelta) -> str:
    total_seconds = delta.total_seconds()
    if total_seconds < 0:
        return "N/A"

    seconds = int(total_seconds)
    milliseconds = int((total_seconds - seconds) * 1000)

    if seconds > 0 and milliseconds > 0:
        return f"{seconds}s {milliseconds}ms"
    elif seconds > 0:
        return f"{seconds}s"
    else:
        return f"{milliseconds}ms"


def parse_duration_str(duration_str: str) -> float:
    if duration_str == "N/A" or duration_str == "-":
        return 0

    total_ms = 0
    if "s" in duration_str:
        parts = duration_str.split()
        for part in parts:
            if "s" in part and "ms" not in part:
                total_ms += float(part.rstrip("s")) * 1000
            elif "ms" in part:
                total_ms += float(part.rstrip("ms"))
    else:
        total_ms = float(duration_str.rstrip("ms"))
    return total_ms


def format_total_duration(total_ms: float) -> str:
    if total_ms == 0:
        return "-"

    seconds = int(total_ms // 1000)
    ms = int(total_ms % 1000)

    if seconds > 0:
        return f"{seconds}s {ms}ms"
    return f"{ms}ms"


def parse_trx_report(file_path: Path) -> TestSummary | None:
    print(f"Parsing TRX report: {file_path}")
    if not file_path.is_file():
        print(f"Error: Test report file not found at {file_path}")
        return None

    try:
        tree = ET.parse(file_path)
        root = tree.getroot()
        namespace = _get_xml_namespace(root)

        summary_node = root.find(f".//{namespace}ResultSummary")
        if summary_node is None:
            print("Error: Could not find ResultSummary node in TRX report.")
            return None

        counters_node = summary_node.find(f".//{namespace}Counters")
        if counters_node is None:
            print("Error: Could not find Counters node in ResultSummary.")
            return None

        total = int(counters_node.get("total", "0"))
        executed = int(counters_node.get("executed", "0"))
        passed = int(counters_node.get("passed", "0"))
        failed = int(counters_node.get("failed", "0"))
        skipped = int(counters_node.get("skipped", "0")) + int(
            counters_node.get("inconclusive", "0")
        )
        if skipped == 0 and total > 0 and executed < total:
            skipped = total - executed

        if executed > 0:
            pass_percentage = (passed / executed) * 100
        else:
            pass_percentage = 100.0 if total == 0 or passed == total else 0.0

        duration_str = "N/A"
        times_node = root.find(f".//{namespace}Times")
        if times_node is not None:
            try:
                start_str = times_node.get("start")
                finish_str = times_node.get("finish")
                if start_str and finish_str:
                    start_time = datetime.fromisoformat(
                        start_str.replace("Z", "+00:00")
                    )
                    finish_time = datetime.fromisoformat(
                        finish_str.replace("Z", "+00:00")
                    )
                    duration_delta = finish_time - start_time
                    duration_str = format_timedelta(duration_delta)
                else:
                    print(
                        "Warning: Missing 'start' or 'finish' attributes in Times node."
                    )
            except (ValueError, TypeError) as e:
                print(
                    f"Warning: Could not parse test run duration from Times node: {e}"
                )
                duration_str = "Error"

        print("Successfully parsed TRX report.")
        return TestSummary(
            total=total,
            executed=executed,
            passed=passed,
            failed=failed,
            skipped=skipped,
            pass_percentage=pass_percentage,
            duration_str=duration_str,
        )

    except ET.ParseError as e:
        print(f"Error parsing TRX XML file '{file_path}': {e}")
    except (KeyError, ValueError, AttributeError, TypeError) as e:
        print(f"Error processing data in TRX XML file '{file_path}': {e}")
    except Exception as e:
        print(f"An unexpected error occurred during XML parsing: {e}")
        traceback.print_exc()

    return None


def generate_markdown_summary(summaries: list[tuple[str, TestSummary]]) -> str:
    print("Generating markdown summary...")

    markdown = """### Test Results

| Project | Status | Total | Passed | Failed | Skipped | Pass % | Duration |
|---------|--------|-------|---------|---------|----------|---------|-----------|
"""

    total_tests = 0
    total_passed = 0
    total_failed = 0
    total_skipped = 0
    total_duration_ms = 0
    any_failure = False

    for project_name, summary in summaries:
        if summary.failed > 0:
            any_failure = True
            project_status = "❌"
        else:
            project_status = "✅"
            
        percentage_str = f"{summary.pass_percentage:.1f}%"
        
        markdown += (
            f"| {project_name} | {project_status} | {summary.total} | {summary.passed} | "
            f"{summary.failed} | {summary.skipped} | "
            f"{percentage_str} | {summary.duration_str} |\n"
        )
        
        total_tests += summary.total
        total_passed += summary.passed
        total_failed += summary.failed
        total_skipped += summary.skipped
        total_duration_ms += parse_duration_str(summary.duration_str)

    total_percentage = (total_passed / total_tests * 100) if total_tests > 0 else 0
    total_status = "❌" if any_failure else "✅"
    total_duration = format_total_duration(total_duration_ms)
    
    markdown += f"""| **Total** | **{total_status}** | **{total_tests}** | **{total_passed}** | \
**{total_failed}** | **{total_skipped}** | \
**{total_percentage:.1f}%** | **{total_duration}** |\n\n"""

    markdown += "_Note: Se artifacts for detaljeret HTML rapport._"
    return markdown


def write_github_summary(markdown: str) -> None:
    summary_path_str = os.environ.get(GITHUB_SUMMARY_ENV_VAR)
    if summary_path_str:
        summary_path = Path(summary_path_str)
        try:
            with summary_path.open("a", encoding="utf-8") as f:
                f.write(markdown + "\n\n")
            print(f"Successfully wrote test summary to ${GITHUB_SUMMARY_ENV_VAR}")
        except IOError as e:
            print(f"Error writing to GITHUB_STEP_SUMMARY file '{summary_path}': {e}")
            _print_summary_stdout(markdown)
    else:
        print(f"{GITHUB_SUMMARY_ENV_VAR} environment variable not set.")
        _print_summary_stdout(markdown)


def _print_summary_stdout(markdown: str) -> None:
    print("\n--- Test Summary (stdout fallback) ---")
    print(markdown)
    print("--- End Summary ---\n")


def main() -> int:
    print("Starting test results processing...")
    trx_files = list(TRX_REPORT_DIR.glob("*.trx"))

    if not trx_files:
        print(f"Error: No test report files found in '{TRX_REPORT_DIR}'.")
        write_github_summary(
            f"### Test Results\n\n🔴 No test report files found in `{TRX_REPORT_DIR}`.\n"
        )
        return 1

    summaries = []
    for trx_file in trx_files:
        project_name = trx_file.stem
        summary = parse_trx_report(trx_file)
        if summary:
            summaries.append((project_name, summary))

    if summaries:
        markdown_summary = generate_markdown_summary(summaries)
        write_github_summary(markdown_summary)

        for project_name, summary in summaries:
            print(
                f"{project_name}: Total={summary.total}, Passed={summary.passed}, "
                f"Failed={summary.failed}, Skipped={summary.skipped}, "
                f"Percentage={summary.pass_percentage:.1f}%, Duration={summary.duration_str}"
            )
        print("Test results processing completed successfully.")
        return 0
    else:
        print("Failed to parse any test results TRX. Exiting.")
        write_github_summary(
            "### Test Results\n\n🔴 Failed to parse test results TRX. Check workflow logs.\n"
        )
        return 1


if __name__ == "__main__":
    # sys.exit accepterer int, så vi fanger returkoden fra main()
    sys.exit(main())
