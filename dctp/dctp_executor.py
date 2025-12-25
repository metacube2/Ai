"""
DCTP Executor - Executes DCTP operations on files.

Handles CREATE, DELETE, INSERT_AFTER, REPLACE, and RENUMBER operations
with backup support and checksum validation.
"""

import hashlib
import os
import re
from dataclasses import dataclass
from enum import Enum
from pathlib import Path
from typing import Optional

from dctp_parser import DCTPParser, Operation, OperationType
from dctp_backup import BackupManager
from dctp_diff import DiffGenerator, FileDiff


class ResultStatus(Enum):
    SUCCESS = "success"
    WARNING = "warning"
    ERROR = "error"
    SKIPPED = "skipped"


@dataclass
class ExecutionResult:
    """Result of executing a single operation."""
    status: ResultStatus
    operation: Operation
    message: str
    diff: Optional[FileDiff] = None

    def __str__(self) -> str:
        status_symbols = {
            ResultStatus.SUCCESS: "✅",
            ResultStatus.WARNING: "⚠️",
            ResultStatus.ERROR: "❌",
            ResultStatus.SKIPPED: "⏭️",
        }
        return f"{status_symbols[self.status]} {self.message}"


@dataclass
class PreviewResult:
    """Result of previewing operations before execution."""
    operation: Operation
    description: str
    diff: Optional[FileDiff] = None
    warnings: list[str] = None

    def __post_init__(self):
        if self.warnings is None:
            self.warnings = []


class DCTPExecutor:
    """Executes DCTP operations on files."""

    # Line number patterns (same as parser)
    LINE_NUMBER_PATTERNS = [
        re.compile(r'\s*#Z(\d+)\s*$'),
        re.compile(r'\s*//Z(\d+)\s*$'),
        re.compile(r'\s*<!--Z(\d+)-->\s*$'),
        re.compile(r'\s*/\*Z(\d+)\*/\s*$'),
        re.compile(r'\s*--Z(\d+)\s*$'),
    ]

    def __init__(
        self,
        project_path: str,
        backup_manager: Optional[BackupManager] = None,
        auto_renumber: bool = True,
        validate_checksums: bool = True
    ):
        """
        Initialize the executor.

        Args:
            project_path: Base project directory
            backup_manager: Optional backup manager for undo support
            auto_renumber: Automatically renumber after operations
            validate_checksums: Validate checksums before operations
        """
        self.project_path = Path(project_path)
        self.backup_manager = backup_manager or BackupManager(project_path)
        self.auto_renumber = auto_renumber
        self.validate_checksums = validate_checksums
        self.diff_generator = DiffGenerator()
        self.parser = DCTPParser()

    def preview(self, operations: list[Operation]) -> list[PreviewResult]:
        """
        Preview operations without executing them.

        Args:
            operations: List of operations to preview

        Returns:
            List of preview results
        """
        previews = []

        for op in operations:
            preview = self._preview_operation(op)
            previews.append(preview)

        return previews

    def _preview_operation(self, op: Operation) -> PreviewResult:
        """Generate preview for a single operation."""
        file_path = self.project_path / op.file
        warnings = []

        if op.type == OperationType.NEW:
            if file_path.exists():
                warnings.append(f"File already exists and will be overwritten")
            return PreviewResult(
                operation=op,
                description=f"CREATE {op.file} ({len(op.content)} lines)",
                warnings=warnings
            )

        elif op.type == OperationType.DELETE:
            if not file_path.exists():
                warnings.append(f"File does not exist")
                return PreviewResult(
                    operation=op,
                    description=f"DELETE {op.file} Z{op.start_line}-Z{op.end_line} (file not found)",
                    warnings=warnings
                )

            old_lines = self._read_file_lines(file_path)
            if op.end_line > len(old_lines):
                warnings.append(f"Line range exceeds file length ({len(old_lines)} lines)")

            new_lines = old_lines.copy()
            start_idx = op.start_line - 1
            end_idx = min(op.end_line, len(old_lines))
            del new_lines[start_idx:end_idx]

            diff = self.diff_generator.generate(old_lines, new_lines, op.file)
            return PreviewResult(
                operation=op,
                description=f"DELETE {op.file} Z{op.start_line}-Z{op.end_line}",
                diff=diff,
                warnings=warnings
            )

        elif op.type == OperationType.INSERT_AFTER:
            if not file_path.exists():
                warnings.append(f"File does not exist")
                return PreviewResult(
                    operation=op,
                    description=f"INSERT_AFTER {op.file} Z{op.start_line} (file not found)",
                    warnings=warnings
                )

            old_lines = self._read_file_lines(file_path)
            if op.start_line > len(old_lines):
                warnings.append(f"Line {op.start_line} exceeds file length ({len(old_lines)} lines)")

            new_lines = old_lines.copy()
            insert_idx = min(op.start_line, len(old_lines))
            for i, line in enumerate(op.content):
                new_lines.insert(insert_idx + i, line)

            diff = self.diff_generator.generate(old_lines, new_lines, op.file)
            return PreviewResult(
                operation=op,
                description=f"INSERT_AFTER {op.file} Z{op.start_line} ({len(op.content)} lines)",
                diff=diff,
                warnings=warnings
            )

        elif op.type == OperationType.REPLACE:
            if not file_path.exists():
                warnings.append(f"File does not exist")
                return PreviewResult(
                    operation=op,
                    description=f"REPLACE {op.file} Z{op.start_line}-Z{op.end_line} (file not found)",
                    warnings=warnings
                )

            old_lines = self._read_file_lines(file_path)
            if op.end_line > len(old_lines):
                warnings.append(f"Line range exceeds file length ({len(old_lines)} lines)")

            new_lines = old_lines.copy()
            start_idx = op.start_line - 1
            end_idx = min(op.end_line, len(old_lines))
            new_lines[start_idx:end_idx] = op.content

            diff = self.diff_generator.generate(old_lines, new_lines, op.file)
            return PreviewResult(
                operation=op,
                description=f"REPLACE {op.file} Z{op.start_line}-Z{op.end_line} ({len(op.content)} lines)",
                diff=diff,
                warnings=warnings
            )

        elif op.type == OperationType.RENUMBER:
            return PreviewResult(
                operation=op,
                description=f"RENUMBER {op.file}",
                warnings=warnings
            )

        return PreviewResult(
            operation=op,
            description=f"UNKNOWN {op.type}",
            warnings=["Unknown operation type"]
        )

    def execute(
        self,
        operations: list[Operation],
        skip_checksum_mismatch: bool = False
    ) -> list[ExecutionResult]:
        """
        Execute a list of operations.

        Args:
            operations: List of operations to execute
            skip_checksum_mismatch: Continue even if checksums don't match

        Returns:
            List of execution results
        """
        results = []

        # Start backup session
        self.backup_manager.start_session()

        try:
            for op in operations:
                result = self._execute_operation(op, skip_checksum_mismatch)
                results.append(result)

                # Stop on error
                if result.status == ResultStatus.ERROR:
                    break

        finally:
            # End backup session
            self.backup_manager.end_session()

        return results

    def _execute_operation(
        self,
        op: Operation,
        skip_checksum_mismatch: bool = False
    ) -> ExecutionResult:
        """Execute a single operation."""
        file_path = self.project_path / op.file

        try:
            if op.type == OperationType.NEW:
                return self._execute_new(op, file_path)
            elif op.type == OperationType.DELETE:
                return self._execute_delete(op, file_path, skip_checksum_mismatch)
            elif op.type == OperationType.INSERT_AFTER:
                return self._execute_insert_after(op, file_path, skip_checksum_mismatch)
            elif op.type == OperationType.REPLACE:
                return self._execute_replace(op, file_path, skip_checksum_mismatch)
            elif op.type == OperationType.RENUMBER:
                return self._execute_renumber(op, file_path)
            else:
                return ExecutionResult(
                    status=ResultStatus.ERROR,
                    operation=op,
                    message=f"Unknown operation type: {op.type}"
                )
        except PermissionError:
            return ExecutionResult(
                status=ResultStatus.ERROR,
                operation=op,
                message=f"Permission denied: {file_path}"
            )
        except Exception as e:
            return ExecutionResult(
                status=ResultStatus.ERROR,
                operation=op,
                message=f"Error: {str(e)}"
            )

    def _execute_new(self, op: Operation, file_path: Path) -> ExecutionResult:
        """Execute a NEW operation (create file)."""
        # Backup if file exists
        if file_path.exists():
            self.backup_manager.backup(str(op.file))

        # Create parent directories
        file_path.parent.mkdir(parents=True, exist_ok=True)

        # Write content
        content = "\n".join(op.content)
        if op.content and not content.endswith("\n"):
            content += "\n"
        file_path.write_text(content)

        return ExecutionResult(
            status=ResultStatus.SUCCESS,
            operation=op,
            message=f"CREATE {op.file} ({len(op.content)} lines)"
        )

    def _execute_delete(
        self,
        op: Operation,
        file_path: Path,
        skip_checksum_mismatch: bool
    ) -> ExecutionResult:
        """Execute a DELETE operation."""
        if not file_path.exists():
            return ExecutionResult(
                status=ResultStatus.WARNING,
                operation=op,
                message=f"File not found: {op.file}"
            )

        # Validate checksum if provided
        if op.checksum and self.validate_checksums:
            if not self._validate_checksum(file_path, op.checksum):
                if not skip_checksum_mismatch:
                    return ExecutionResult(
                        status=ResultStatus.WARNING,
                        operation=op,
                        message=f"Checksum mismatch for {op.file} - file was modified externally"
                    )

        # Backup file
        self.backup_manager.backup(str(op.file))

        # Read file and delete lines
        lines = self._read_file_lines(file_path)
        old_lines = lines.copy()

        if op.end_line > len(lines):
            return ExecutionResult(
                status=ResultStatus.WARNING,
                operation=op,
                message=f"Line range Z{op.start_line}-Z{op.end_line} exceeds file length ({len(lines)} lines)"
            )

        start_idx = op.start_line - 1
        end_idx = op.end_line
        del lines[start_idx:end_idx]

        # Write back
        self._write_file_lines(file_path, lines)

        diff = self.diff_generator.generate(old_lines, lines, op.file)

        return ExecutionResult(
            status=ResultStatus.SUCCESS,
            operation=op,
            message=f"DELETE {op.file} Z{op.start_line}-Z{op.end_line}",
            diff=diff
        )

    def _execute_insert_after(
        self,
        op: Operation,
        file_path: Path,
        skip_checksum_mismatch: bool
    ) -> ExecutionResult:
        """Execute an INSERT_AFTER operation."""
        if not file_path.exists():
            return ExecutionResult(
                status=ResultStatus.WARNING,
                operation=op,
                message=f"File not found: {op.file}"
            )

        # Validate checksum if provided
        if op.checksum and self.validate_checksums:
            if not self._validate_checksum(file_path, op.checksum):
                if not skip_checksum_mismatch:
                    return ExecutionResult(
                        status=ResultStatus.WARNING,
                        operation=op,
                        message=f"Checksum mismatch for {op.file} - file was modified externally"
                    )

        # Backup file
        self.backup_manager.backup(str(op.file))

        # Read file and insert lines
        lines = self._read_file_lines(file_path)
        old_lines = lines.copy()

        if op.start_line > len(lines):
            return ExecutionResult(
                status=ResultStatus.WARNING,
                operation=op,
                message=f"Line Z{op.start_line} exceeds file length ({len(lines)} lines)"
            )

        insert_idx = op.start_line
        for i, line in enumerate(op.content):
            lines.insert(insert_idx + i, line)

        # Write back
        self._write_file_lines(file_path, lines)

        diff = self.diff_generator.generate(old_lines, lines, op.file)

        return ExecutionResult(
            status=ResultStatus.SUCCESS,
            operation=op,
            message=f"INSERT_AFTER {op.file} Z{op.start_line} ({len(op.content)} lines)",
            diff=diff
        )

    def _execute_replace(
        self,
        op: Operation,
        file_path: Path,
        skip_checksum_mismatch: bool
    ) -> ExecutionResult:
        """Execute a REPLACE operation."""
        if not file_path.exists():
            return ExecutionResult(
                status=ResultStatus.WARNING,
                operation=op,
                message=f"File not found: {op.file}"
            )

        # Validate checksum if provided
        if op.checksum and self.validate_checksums:
            if not self._validate_checksum(file_path, op.checksum):
                if not skip_checksum_mismatch:
                    return ExecutionResult(
                        status=ResultStatus.WARNING,
                        operation=op,
                        message=f"Checksum mismatch for {op.file} - file was modified externally"
                    )

        # Backup file
        self.backup_manager.backup(str(op.file))

        # Read file and replace lines
        lines = self._read_file_lines(file_path)
        old_lines = lines.copy()

        if op.end_line > len(lines):
            return ExecutionResult(
                status=ResultStatus.WARNING,
                operation=op,
                message=f"Line range Z{op.start_line}-Z{op.end_line} exceeds file length ({len(lines)} lines)"
            )

        start_idx = op.start_line - 1
        end_idx = op.end_line
        lines[start_idx:end_idx] = op.content

        # Write back
        self._write_file_lines(file_path, lines)

        diff = self.diff_generator.generate(old_lines, lines, op.file)

        return ExecutionResult(
            status=ResultStatus.SUCCESS,
            operation=op,
            message=f"REPLACE {op.file} Z{op.start_line}-Z{op.end_line} ({len(op.content)} lines)",
            diff=diff
        )

    def _execute_renumber(self, op: Operation, file_path: Path) -> ExecutionResult:
        """Execute a RENUMBER operation."""
        if not file_path.exists():
            return ExecutionResult(
                status=ResultStatus.WARNING,
                operation=op,
                message=f"File not found: {op.file}"
            )

        # No backup needed for renumber (just updates line numbers)
        lines = self._read_file_lines(file_path)
        renumbered_lines = []

        for i, line in enumerate(lines, 1):
            # Remove existing line number
            clean_line = self._remove_line_number(line)
            # Add new line number
            suffix = self.parser.get_line_number_suffix(op.file, i)
            renumbered_lines.append(clean_line + suffix)

        self._write_file_lines(file_path, renumbered_lines)

        return ExecutionResult(
            status=ResultStatus.SUCCESS,
            operation=op,
            message=f"RENUMBER {op.file} ({len(lines)} lines)"
        )

    def _read_file_lines(self, file_path: Path) -> list[str]:
        """Read file and return lines without trailing newlines."""
        content = file_path.read_text()
        lines = content.split('\n')
        # Remove trailing empty line if file ends with newline
        if lines and lines[-1] == '':
            lines = lines[:-1]
        return lines

    def _write_file_lines(self, file_path: Path, lines: list[str]) -> None:
        """Write lines to file with trailing newline."""
        content = '\n'.join(lines)
        if lines and not content.endswith('\n'):
            content += '\n'
        file_path.write_text(content)

    def _remove_line_number(self, line: str) -> str:
        """Remove line number marker from end of line."""
        for pattern in self.LINE_NUMBER_PATTERNS:
            match = pattern.search(line)
            if match:
                return line[:match.start()]
        return line

    def _validate_checksum(self, file_path: Path, expected: str) -> bool:
        """Validate file checksum."""
        content = file_path.read_bytes()
        actual = hashlib.md5(content).hexdigest()[:8]
        return actual.lower() == expected.lower()

    @staticmethod
    def calculate_checksum(file_path: Path) -> str:
        """Calculate checksum for a file."""
        content = file_path.read_bytes()
        return hashlib.md5(content).hexdigest()[:8]


def main():
    """Test the executor."""
    import tempfile

    test_input = """###FILE:calculator.py
###NEW
def add(a, b):  #Z1
    return a + b  #Z2
  #Z3
def multiply(a, b):  #Z4
    return a * b  #Z5
###END
"""

    with tempfile.TemporaryDirectory() as tmpdir:
        parser = DCTPParser()
        result = parser.parse(test_input)

        print("Parsed operations:")
        for op in result.operations:
            print(f"  {op}")

        executor = DCTPExecutor(tmpdir)

        # Preview
        print("\nPreviews:")
        previews = executor.preview(result.operations)
        for preview in previews:
            print(f"  {preview.description}")
            if preview.warnings:
                for w in preview.warnings:
                    print(f"    ⚠️ {w}")

        # Execute
        print("\nExecution:")
        exec_results = executor.execute(result.operations)
        for r in exec_results:
            print(f"  {r}")

        # Verify file was created
        file_path = Path(tmpdir) / "calculator.py"
        if file_path.exists():
            print(f"\nFile content:\n{file_path.read_text()}")


if __name__ == "__main__":
    main()
