"""
DCTP Diff Generator - Generates diffs for preview display.

Compares old and new content and produces colored diff output
for the GUI preview.
"""

import difflib
from dataclasses import dataclass
from enum import Enum
from typing import Optional


class DiffType(Enum):
    UNCHANGED = "unchanged"
    ADDED = "added"
    REMOVED = "removed"
    CONTEXT = "context"


@dataclass
class DiffLine:
    """Represents a single line in a diff."""
    type: DiffType
    line_number_old: Optional[int]  # Line number in old file
    line_number_new: Optional[int]  # Line number in new file
    content: str

    @property
    def prefix(self) -> str:
        """Get the diff prefix character."""
        if self.type == DiffType.ADDED:
            return "+"
        elif self.type == DiffType.REMOVED:
            return "-"
        else:
            return " "

    def __str__(self) -> str:
        old_num = str(self.line_number_old) if self.line_number_old else ""
        new_num = str(self.line_number_new) if self.line_number_new else ""
        return f"{old_num:>4} {new_num:>4} {self.prefix} {self.content}"


@dataclass
class DiffBlock:
    """A block of related diff lines."""
    start_old: int
    end_old: int
    start_new: int
    end_new: int
    lines: list[DiffLine]

    @property
    def header(self) -> str:
        """Generate a unified diff style header."""
        return f"@@ -{self.start_old},{self.end_old - self.start_old + 1} +{self.start_new},{self.end_new - self.start_new + 1} @@"


@dataclass
class FileDiff:
    """Complete diff for a file."""
    filename: str
    old_content: list[str]
    new_content: list[str]
    blocks: list[DiffBlock]
    lines: list[DiffLine]

    @property
    def has_changes(self) -> bool:
        return any(line.type in (DiffType.ADDED, DiffType.REMOVED) for line in self.lines)

    @property
    def additions(self) -> int:
        return sum(1 for line in self.lines if line.type == DiffType.ADDED)

    @property
    def deletions(self) -> int:
        return sum(1 for line in self.lines if line.type == DiffType.REMOVED)


class DiffGenerator:
    """Generates diffs between old and new content."""

    def __init__(self, context_lines: int = 3):
        """
        Initialize diff generator.

        Args:
            context_lines: Number of context lines around changes
        """
        self.context_lines = context_lines

    def generate(
        self,
        old_lines: list[str],
        new_lines: list[str],
        filename: str = ""
    ) -> FileDiff:
        """
        Generate a diff between old and new content.

        Args:
            old_lines: Original content lines
            new_lines: New content lines
            filename: Optional filename for display

        Returns:
            FileDiff object with all diff information
        """
        diff_lines: list[DiffLine] = []

        # Use difflib to compute differences
        matcher = difflib.SequenceMatcher(None, old_lines, new_lines)

        old_line_num = 1
        new_line_num = 1

        for tag, i1, i2, j1, j2 in matcher.get_opcodes():
            if tag == 'equal':
                for idx in range(i2 - i1):
                    diff_lines.append(DiffLine(
                        type=DiffType.UNCHANGED,
                        line_number_old=old_line_num,
                        line_number_new=new_line_num,
                        content=old_lines[i1 + idx]
                    ))
                    old_line_num += 1
                    new_line_num += 1

            elif tag == 'replace':
                # Show removed lines first, then added
                for idx in range(i2 - i1):
                    diff_lines.append(DiffLine(
                        type=DiffType.REMOVED,
                        line_number_old=old_line_num,
                        line_number_new=None,
                        content=old_lines[i1 + idx]
                    ))
                    old_line_num += 1

                for idx in range(j2 - j1):
                    diff_lines.append(DiffLine(
                        type=DiffType.ADDED,
                        line_number_old=None,
                        line_number_new=new_line_num,
                        content=new_lines[j1 + idx]
                    ))
                    new_line_num += 1

            elif tag == 'delete':
                for idx in range(i2 - i1):
                    diff_lines.append(DiffLine(
                        type=DiffType.REMOVED,
                        line_number_old=old_line_num,
                        line_number_new=None,
                        content=old_lines[i1 + idx]
                    ))
                    old_line_num += 1

            elif tag == 'insert':
                for idx in range(j2 - j1):
                    diff_lines.append(DiffLine(
                        type=DiffType.ADDED,
                        line_number_old=None,
                        line_number_new=new_line_num,
                        content=new_lines[j1 + idx]
                    ))
                    new_line_num += 1

        # Generate blocks with context
        blocks = self._generate_blocks(diff_lines)

        return FileDiff(
            filename=filename,
            old_content=old_lines,
            new_content=new_lines,
            blocks=blocks,
            lines=diff_lines
        )

    def _generate_blocks(self, diff_lines: list[DiffLine]) -> list[DiffBlock]:
        """Generate diff blocks with context."""
        if not diff_lines:
            return []

        blocks: list[DiffBlock] = []
        current_block_lines: list[DiffLine] = []
        in_change = False
        unchanged_count = 0

        for line in diff_lines:
            is_change = line.type in (DiffType.ADDED, DiffType.REMOVED)

            if is_change:
                if not in_change:
                    # Starting a new change block, include context
                    in_change = True
                    unchanged_count = 0
                current_block_lines.append(line)

            else:  # Unchanged line
                if in_change:
                    unchanged_count += 1
                    if unchanged_count <= self.context_lines:
                        current_block_lines.append(line)
                    else:
                        # End current block and start fresh
                        if current_block_lines:
                            blocks.append(self._create_block(current_block_lines))
                        current_block_lines = []
                        in_change = False
                        unchanged_count = 0
                else:
                    # Keep track of potential context lines
                    current_block_lines.append(line)
                    if len(current_block_lines) > self.context_lines:
                        current_block_lines.pop(0)

        # Don't forget the last block
        if current_block_lines and any(
            l.type in (DiffType.ADDED, DiffType.REMOVED) for l in current_block_lines
        ):
            blocks.append(self._create_block(current_block_lines))

        return blocks

    def _create_block(self, lines: list[DiffLine]) -> DiffBlock:
        """Create a DiffBlock from a list of lines."""
        old_nums = [l.line_number_old for l in lines if l.line_number_old is not None]
        new_nums = [l.line_number_new for l in lines if l.line_number_new is not None]

        return DiffBlock(
            start_old=min(old_nums) if old_nums else 0,
            end_old=max(old_nums) if old_nums else 0,
            start_new=min(new_nums) if new_nums else 0,
            end_new=max(new_nums) if new_nums else 0,
            lines=lines
        )

    def generate_unified_diff(
        self,
        old_lines: list[str],
        new_lines: list[str],
        old_filename: str = "a/file",
        new_filename: str = "b/file"
    ) -> str:
        """
        Generate a unified diff string.

        Args:
            old_lines: Original content lines
            new_lines: New content lines
            old_filename: Label for old file
            new_filename: Label for new file

        Returns:
            Unified diff as string
        """
        diff = difflib.unified_diff(
            old_lines,
            new_lines,
            fromfile=old_filename,
            tofile=new_filename,
            lineterm=""
        )
        return "\n".join(diff)

    def generate_side_by_side(
        self,
        old_lines: list[str],
        new_lines: list[str],
        width: int = 80
    ) -> list[tuple[str, str, str]]:
        """
        Generate a side-by-side diff representation.

        Args:
            old_lines: Original content lines
            new_lines: New content lines
            width: Width for each column

        Returns:
            List of tuples (left_line, marker, right_line)
        """
        result = []
        half_width = (width - 3) // 2

        matcher = difflib.SequenceMatcher(None, old_lines, new_lines)

        for tag, i1, i2, j1, j2 in matcher.get_opcodes():
            if tag == 'equal':
                for idx in range(i2 - i1):
                    line = old_lines[i1 + idx][:half_width]
                    result.append((line, " ", line))

            elif tag == 'replace':
                max_len = max(i2 - i1, j2 - j1)
                for idx in range(max_len):
                    old_line = old_lines[i1 + idx][:half_width] if idx < i2 - i1 else ""
                    new_line = new_lines[j1 + idx][:half_width] if idx < j2 - j1 else ""
                    result.append((old_line, "|", new_line))

            elif tag == 'delete':
                for idx in range(i2 - i1):
                    old_line = old_lines[i1 + idx][:half_width]
                    result.append((old_line, "<", ""))

            elif tag == 'insert':
                for idx in range(j2 - j1):
                    new_line = new_lines[j1 + idx][:half_width]
                    result.append(("", ">", new_line))

        return result


def format_diff_for_display(diff: FileDiff, use_colors: bool = True) -> str:
    """
    Format a FileDiff for terminal/GUI display.

    Args:
        diff: The FileDiff to format
        use_colors: Whether to use ANSI colors

    Returns:
        Formatted string
    """
    lines = []

    if diff.filename:
        lines.append(f"--- {diff.filename}")
        lines.append(f"+++ {diff.filename}")

    for line in diff.lines:
        if use_colors:
            if line.type == DiffType.ADDED:
                prefix = "\033[32m+"  # Green
                suffix = "\033[0m"
            elif line.type == DiffType.REMOVED:
                prefix = "\033[31m-"  # Red
                suffix = "\033[0m"
            else:
                prefix = " "
                suffix = ""
        else:
            prefix = line.prefix
            suffix = ""

        lines.append(f"{prefix} {line.content}{suffix}")

    return "\n".join(lines)


def main():
    """Test the diff generator."""
    old_content = [
        "def calculate_tax(amount):",
        "    rate = 0.19",
        "    if amount > 1000:",
        "        rate = 0.25",
        "    return amount * rate",
    ]

    new_content = [
        "def calculate_tax(amount):",
        "    rate = 0.19",
        "    if amount > 10000:",
        "        rate = 0.22",
        "    elif amount > 1000:",
        "        rate = 0.19",
        "    return amount * rate",
    ]

    generator = DiffGenerator()
    diff = generator.generate(old_content, new_content, "calculator.py")

    print(f"File: {diff.filename}")
    print(f"Additions: {diff.additions}, Deletions: {diff.deletions}")
    print()
    print("Diff output:")
    print(format_diff_for_display(diff))


if __name__ == "__main__":
    main()
