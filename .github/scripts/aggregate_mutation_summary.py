#!/usr/bin/env python3
# .github/scripts/aggregate_mutation_summary.py

import json
import os
import sys
import re
import traceback
import argparse

from collections import defaultdict
from pathlib import Path
from typing import NamedTuple
from enum import Enum

STRYKER_REPORT_FILENAME = "mutation-report.json"
GITHUB_SUMMARY_ENV_VAR = "GITHUB_STEP_SUMMARY"
GITHUB_ENV_VAR = "GITHUB_ENV"

STATUS_KILLED = "Killed"
STATUS_SURVIVED = "Survived"
STATUS_TIMEOUT = "Timeout"
STATUS_NO_COVERAGE = "NoCoverage"
STATUS_IGNORED = "Ignored"
STATUS_COMPILE_ERROR = "CompileError"
STATUS_RUNTIME_ERROR = "RuntimeError"


class MutationStats(NamedTuple):
    killed: int = 0
    survived: int = 0
    timeout: int = 0
    no_coverage: int = 0
    ignored: int = 0
    compile_error: int = 0
    runtime_error: int = 0
    total_mutants: int = 0

    def __add__(self, other: "MutationStats") -> "MutationStats":
        return MutationStats(
            killed=self.killed + other.killed,
            survived=self.survived + other.survived,
            timeout=self.timeout + other.timeout,
            no_coverage=self.no_coverage + other.no_coverage,
            ignored=self.ignored + other.ignored,
            compile_error=self.compile_error + other.compile_error,
            runtime_error=self.runtime_error + other.runtime_error,
            total_mutants=self.total_mutants + other.total_mutants,
        )


class MutationSummary(NamedTuple):
    overall: MutationStats
    directories: dict[str, MutationStats]


class CoverageStatus(Enum):
    GREEN = "🟢"
    YELLOW = "🟡"
    RED = "🔴"


def extract_directory(file_path_str: str) -> str:
    parts = Path(file_path_str).parts
    try:
        server_index = parts.index("server")
        if len(parts) > server_index + 1:
            potential_dir = parts[server_index + 1]
            if potential_dir in [
                "Core",
                "Application",
                "Infrastructure",
                "Infrastructure.Data",
            ]:
                if (
                    potential_dir == "Infrastructure"
                    and len(parts) > server_index + 2
                    and parts[server_index + 2] == "Data"
                ):
                    return "Infrastructure.Data"
                return potential_dir
            return (
                parts[server_index + 1]
                if "." not in parts[server_index + 1]
                else "unknown_dir"
            )

    except ValueError:
        parent_dir = Path(file_path_str).parent.name
        return parent_dir if parent_dir and parent_dir != "." else "unknown_dir"

    return "unknown_dir"


def parse_single_stryker_report(
    file_path: Path,
) -> tuple[MutationStats, dict[str, MutationStats]] | None:
    """Parses a single report and returns overall stats and directory stats for THAT report."""
    print(f"Parsing individual Stryker report: {file_path}")
    if not file_path.is_file():
        print(f"Error: Stryker report file not found at {file_path}")
        return None

    try:
        with file_path.open("r", encoding="utf-8") as file:
            data = json.load(file)

        if not isinstance(data, dict) or "files" not in data:
            print(f"Error: Invalid format in '{file_path}'. Missing 'files' key.")
            return None

        report_overall_counts: dict[str, int] = defaultdict(int)
        report_directory_stats: dict[str, defaultdict[str, int]] = defaultdict(
            lambda: defaultdict(int)
        )

        for file_path_str, file_data in data.get("files", {}).items():
            if not isinstance(file_data, dict) or "mutants" not in file_data:
                print(f"Warning: Skipping invalid file entry for '{file_path_str}'")
                continue

            directory = extract_directory(file_path_str)
            dir_counts = report_directory_stats[directory]

            num_mutants_in_file = 0
            for mutant in file_data.get("mutants", []):
                if not isinstance(mutant, dict) or "status" not in mutant:
                    print(
                        f"Warning: Skipping invalid mutant in file '{file_path_str}': {mutant}"
                    )
                    continue

                status = mutant["status"]
                dir_counts[status] += 1
                report_overall_counts[status] += 1
                num_mutants_in_file += 1

            dir_counts["total_mutants"] += num_mutants_in_file
            report_overall_counts["total_mutants"] += num_mutants_in_file

        individual_directory_metrics: dict[str, MutationStats] = {}
        for dir_name, counts in report_directory_stats.items():
            stats_data = {
                k.lower().replace("error", "_error"): v for k, v in counts.items()
            }
            for field in MutationStats._fields:
                stats_data.setdefault(field, 0)
            individual_directory_metrics[dir_name] = MutationStats(**stats_data)

        overall_stats_data = {
            k.lower().replace("error", "_error"): v
            for k, v in report_overall_counts.items()
        }
        for field in MutationStats._fields:
            overall_stats_data.setdefault(field, 0)
        individual_overall_metrics = MutationStats(**overall_stats_data)

        print(f"Successfully parsed individual report: {file_path}")
        return individual_overall_metrics, individual_directory_metrics

    except json.JSONDecodeError as e:
        print(f"Error decoding JSON from '{file_path}': {e}")
    except (KeyError, ValueError, AttributeError, TypeError) as e:
        print(f"Error processing data in report '{file_path}': {e}")
    except Exception as e:
        print(f"An unexpected error occurred parsing report '{file_path}': {e}")
        traceback.print_exc()

    return None


def get_status_emoji(score: float) -> CoverageStatus:
    if score >= 80:
        return CoverageStatus.GREEN
    elif score >= 60:
        return CoverageStatus.YELLOW
    else:
        return CoverageStatus.RED


def format_score(score: float) -> str:
    return f"{score:.1f}%"


def calculate_scores(stats: MutationStats) -> tuple[float, float]:
    detected = stats.killed + stats.timeout
    undetected = stats.survived + stats.no_coverage
    valid_mutants = detected + undetected
    covered_mutants = detected + stats.survived

    score = 100.0 if valid_mutants == 0 else (detected / valid_mutants) * 100.0
    score_covered = (
        100.0 if covered_mutants == 0 else (detected / covered_mutants) * 100.0
    )

    return score, score_covered


def generate_markdown_summary(summary: MutationSummary) -> str:
    print("Generating aggregated markdown summary...")
    lines: list[str] = []
    lines.append("### Mutation Testing Summary (Aggregated)")
    lines.append("")
    lines.append(
        "| Directory | Status | Score | Covered | Killed | Survived | Timeout | No Coverage | Ignored | Runtime errors | Compile errors | Detected | Undetected | Total |"
    )
    lines.append(
        "|-----------|--------|-------|---------|--------|----------|---------|-------------|---------|----------------|----------------|----------|------------|-------|"
    )

    overall = summary.overall
    overall_score, overall_score_covered = calculate_scores(overall)
    emoji = get_status_emoji(overall_score).value
    detected = overall.killed + overall.timeout
    undetected = overall.survived + overall.no_coverage
    lines.append(
        f"| **Overall** | {emoji} "
        f"| **{format_score(overall_score)}** "
        f"| {format_score(overall_score_covered)} "
        f"| {overall.killed} | {overall.survived} "
        f"| {overall.timeout} | {overall.no_coverage} "
        f"| {overall.ignored} "
        f"| {overall.runtime_error} | {overall.compile_error} "
        f"| {detected} | {undetected} "
        f"| {overall.total_mutants} |"
    )

    sorted_dirs = sorted(
        summary.directories.items(),
        key=lambda item: calculate_scores(item[1])[0],
        reverse=True,
    )

    for dir_name, stats in sorted_dirs:
        dir_score, dir_score_covered = calculate_scores(stats)
        emoji = get_status_emoji(dir_score).value
        detected = stats.killed + stats.timeout
        undetected = stats.survived + stats.no_coverage
        lines.append(
            f"| {dir_name} | {emoji} "
            f"| {format_score(dir_score)} "
            f"| {format_score(dir_score_covered)} "
            f"| {stats.killed} | {stats.survived} "
            f"| {stats.timeout} | {stats.no_coverage} "
            f"| {stats.ignored} "
            f"| {stats.runtime_error} | {stats.compile_error} "
            f"| {detected} | {undetected} "
            f"| {stats.total_mutants} |"
        )

    lines.append(
        "\n_Note: This is an aggregated summary. See individual artifacts for detailed HTML reports._"
    )
    return "\n".join(lines)


def write_github_summary(markdown: str) -> None:
    summary_path_str = os.environ.get(GITHUB_SUMMARY_ENV_VAR)
    if summary_path_str:
        summary_path = Path(summary_path_str)
        try:
            with summary_path.open("a", encoding="utf-8") as f:
                f.write(markdown + "\n\n")
            print(
                f"Successfully wrote aggregated mutation summary to ${GITHUB_SUMMARY_ENV_VAR}"
            )
        except IOError as e:
            print(f"Error writing to GITHUB_STEP_SUMMARY file '{summary_path}': {e}")
            _print_summary_stdout(markdown)
    else:
        print(f"{GITHUB_SUMMARY_ENV_VAR} environment variable not set.")
        _print_summary_stdout(markdown)


def write_github_env(overall_metrics: MutationStats) -> None:
    env_path_str = os.environ.get(GITHUB_ENV_VAR)
    if env_path_str:
        env_path = Path(env_path_str)
        try:
            score, _ = calculate_scores(overall_metrics)
            score_int = int(round(score))

            with env_path.open("a", encoding="utf-8") as env_file:
                env_file.write(f"MUTATION_SCORE={score_int}\n")
                env_file.write(f"MUTATION_SCORE_FLOAT={score:.1f}\n")
                env_file.write(f"KILLED_MUTANTS={overall_metrics.killed}\n")
                env_file.write(f"TOTAL_MUTANTS={overall_metrics.total_mutants}\n")

            print(f"Final Overall Mutation Score set to GITHUB_ENV: {score_int}%")
        except IOError as e:
            print(f"Error writing final metrics to GITHUB_ENV file '{env_path}': {e}")
        except Exception as e:
            print(f"An unexpected error occurred writing to GITHUB_ENV: {e}")
    else:
        print(f"Warning: {GITHUB_ENV_VAR} env var not found. Cannot set final score.")


def _print_summary_stdout(markdown: str) -> None:
    print("\n--- Aggregated Mutation Summary (stdout fallback) ---")
    print(markdown)
    print("--- End Summary ---\n")


def main() -> int:
    parser = argparse.ArgumentParser(description="Aggregate Stryker mutation reports.")
    parser.add_argument(
        "report_dir",
        type=str,
        help="Directory containing the downloaded mutation report JSON files.",
    )
    args = parser.parse_args()

    report_base_path = Path(args.report_dir)
    print(f"Starting aggregation process in directory: {report_base_path}")

    if not report_base_path.is_dir():
        print(f"Error: Input directory '{report_base_path}' not found.")
        write_github_summary(
            f"### Mutation Testing Summary (Aggregated)\n\n🔴 Error: Report directory `{report_base_path}` not found.\n"
        )
        return 1

    json_report_files = list(report_base_path.rglob(STRYKER_REPORT_FILENAME))

    if not json_report_files:
        print(
            f"Error: No '{STRYKER_REPORT_FILENAME}' files found within '{report_base_path}'."
        )
        print("Directory contents:")
        try:
            for item in report_base_path.rglob("*"):
                print(f"- {item}")
        except Exception as e:
            print(f"Error listing directory contents: {e}")
        write_github_summary(
            f"### Mutation Testing Summary (Aggregated)\n\n🔴 Error: No `{STRYKER_REPORT_FILENAME}` files found in `{report_base_path}`.\n"
        )
        return 1

    print(f"Found {len(json_report_files)} report files to aggregate:")
    for f in json_report_files:
        print(f"- {f}")

    aggregated_overall_stats = MutationStats()
    aggregated_directory_stats: dict[str, MutationStats] = defaultdict(MutationStats)
    parse_errors = 0

    for report_file in json_report_files:
        parsed_data = parse_single_stryker_report(report_file)
        if parsed_data:
            individual_overall, individual_dirs = parsed_data
            aggregated_overall_stats += individual_overall
            for dir_name, dir_stats in individual_dirs.items():
                aggregated_directory_stats[dir_name] += dir_stats
        else:
            print(f"Warning: Failed to parse report {report_file}. Skipping.")
            parse_errors += 1

    if parse_errors > 0:
        print(f"Warning: Encountered {parse_errors} errors during parsing.")

    if aggregated_overall_stats.total_mutants == 0 and parse_errors == len(
        json_report_files
    ):
        print("Error: Failed to parse any reports successfully.")
        write_github_summary(
            f"### Mutation Testing Summary (Aggregated)\n\n🔴 Error: Failed to parse any of the {len(json_report_files)} report files.\n"
        )
        return 1

    final_summary = MutationSummary(
        overall=aggregated_overall_stats, directories=dict(aggregated_directory_stats)
    )

    final_markdown = generate_markdown_summary(final_summary)
    write_github_summary(final_markdown)
    write_github_env(final_summary.overall)

    overall_score, _ = calculate_scores(final_summary.overall)
    print(f"Aggregation complete. Final Overall Mutation Score: {overall_score:.1f}%")

    return 0 if parse_errors < len(json_report_files) else 1


if __name__ == "__main__":
    sys.exit(main())
