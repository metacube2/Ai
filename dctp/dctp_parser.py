"""
DCTP Parser - Parses DCTP control commands and code blocks.

Handles line-numbered code with language-specific comment formats:
- Python/Shell: #Z1
- JavaScript/Java/C/C++: //Z1
- HTML: <!--Z1-->
- CSS: /*Z1*/
- SQL: --Z1
"""

import re
from dataclasses import dataclass, field
from typing import Optional
from enum import Enum


class OperationType(Enum):
    NEW = "NEW"
    DELETE = "DELETE"
    INSERT_AFTER = "INSERT_AFTER"
    REPLACE = "REPLACE"
    RENUMBER = "RENUMBER"


@dataclass
class Operation:
    """Represents a single DCTP operation."""
    type: OperationType
    file: str
    start_line: Optional[int] = None
    end_line: Optional[int] = None
    content: list[str] = field(default_factory=list)
    checksum: Optional[str] = None
    raw_content: list[str] = field(default_factory=list)  # Content with line numbers

    def __str__(self) -> str:
        if self.type == OperationType.NEW:
            return f"CREATE {self.file} ({len(self.content)} lines)"
        elif self.type == OperationType.DELETE:
            return f"DELETE {self.file} Z{self.start_line}-Z{self.end_line}"
        elif self.type == OperationType.INSERT_AFTER:
            return f"INSERT_AFTER {self.file} Z{self.start_line} ({len(self.content)} lines)"
        elif self.type == OperationType.REPLACE:
            return f"REPLACE {self.file} Z{self.start_line}-Z{self.end_line} ({len(self.content)} lines)"
        elif self.type == OperationType.RENUMBER:
            return f"RENUMBER {self.file}"
        return f"{self.type.value} {self.file}"


@dataclass
class ParseError:
    """Represents a parsing error."""
    line_number: int
    line_content: str
    message: str


@dataclass
class ParseResult:
    """Result of parsing DCTP input."""
    operations: list[Operation]
    errors: list[ParseError]

    @property
    def has_errors(self) -> bool:
        return len(self.errors) > 0


class DCTPParser:
    """Parser for DCTP (Delta Code Transfer Protocol) format."""

    # Regex patterns for line number markers in different languages
    LINE_NUMBER_PATTERNS = [
        re.compile(r'\s*#Z(\d+)\s*$'),           # Python, Shell
        re.compile(r'\s*//Z(\d+)\s*$'),          # JavaScript, Java, C, C++
        re.compile(r'\s*<!--Z(\d+)-->\s*$'),     # HTML
        re.compile(r'\s*/\*Z(\d+)\*/\s*$'),      # CSS
        re.compile(r'\s*--Z(\d+)\s*$'),          # SQL
    ]

    # Control command patterns
    FILE_PATTERN = re.compile(r'^###FILE:(.+)$')
    NEW_PATTERN = re.compile(r'^###NEW\s*$')
    DELETE_PATTERN = re.compile(r'^###DELETE:Z(\d+)(?:-Z(\d+))?\s*$')
    INSERT_AFTER_PATTERN = re.compile(r'^###INSERT_AFTER:Z(\d+)\s*$')
    REPLACE_PATTERN = re.compile(r'^###REPLACE:Z(\d+)(?:-Z(\d+))?\s*$')
    END_PATTERN = re.compile(r'^###END\s*$')
    RENUMBER_PATTERN = re.compile(r'^###RENUMBER\s*$')
    CHECKSUM_PATTERN = re.compile(r'^###CHECKSUM:([a-fA-F0-9]+)\s*$')

    def parse(self, text: str) -> ParseResult:
        """
        Parse DCTP formatted text into a list of operations.

        Args:
            text: The DCTP formatted input text

        Returns:
            ParseResult containing operations and any errors
        """
        operations: list[Operation] = []
        errors: list[ParseError] = []

        current_file: Optional[str] = None
        current_op: Optional[Operation] = None
        buffer: list[str] = []
        raw_buffer: list[str] = []

        lines = text.split('\n')

        for line_num, line in enumerate(lines, 1):
            # Skip empty lines outside of content blocks
            if not line.strip() and current_op is None:
                continue

            # Check for FILE command
            file_match = self.FILE_PATTERN.match(line)
            if file_match:
                current_file = file_match.group(1).strip()
                continue

            # Check for NEW command
            if self.NEW_PATTERN.match(line):
                if current_file is None:
                    errors.append(ParseError(line_num, line, "###NEW without ###FILE"))
                    continue
                current_op = Operation(type=OperationType.NEW, file=current_file)
                buffer = []
                raw_buffer = []
                continue

            # Check for DELETE command
            delete_match = self.DELETE_PATTERN.match(line)
            if delete_match:
                if current_file is None:
                    errors.append(ParseError(line_num, line, "###DELETE without ###FILE"))
                    continue
                start = int(delete_match.group(1))
                end = int(delete_match.group(2)) if delete_match.group(2) else start
                operations.append(Operation(
                    type=OperationType.DELETE,
                    file=current_file,
                    start_line=start,
                    end_line=end
                ))
                continue

            # Check for INSERT_AFTER command
            insert_match = self.INSERT_AFTER_PATTERN.match(line)
            if insert_match:
                if current_file is None:
                    errors.append(ParseError(line_num, line, "###INSERT_AFTER without ###FILE"))
                    continue
                current_op = Operation(
                    type=OperationType.INSERT_AFTER,
                    file=current_file,
                    start_line=int(insert_match.group(1))
                )
                buffer = []
                raw_buffer = []
                continue

            # Check for REPLACE command
            replace_match = self.REPLACE_PATTERN.match(line)
            if replace_match:
                if current_file is None:
                    errors.append(ParseError(line_num, line, "###REPLACE without ###FILE"))
                    continue
                start = int(replace_match.group(1))
                end = int(replace_match.group(2)) if replace_match.group(2) else start
                current_op = Operation(
                    type=OperationType.REPLACE,
                    file=current_file,
                    start_line=start,
                    end_line=end
                )
                buffer = []
                raw_buffer = []
                continue

            # Check for END command
            if self.END_PATTERN.match(line):
                if current_op:
                    current_op.content = buffer.copy()
                    current_op.raw_content = raw_buffer.copy()
                    operations.append(current_op)
                    current_op = None
                    buffer = []
                    raw_buffer = []
                continue

            # Check for RENUMBER command
            if self.RENUMBER_PATTERN.match(line):
                if current_file is None:
                    errors.append(ParseError(line_num, line, "###RENUMBER without ###FILE"))
                    continue
                operations.append(Operation(type=OperationType.RENUMBER, file=current_file))
                continue

            # Check for CHECKSUM command
            checksum_match = self.CHECKSUM_PATTERN.match(line)
            if checksum_match:
                if current_op:
                    current_op.checksum = checksum_match.group(1)
                continue

            # Regular code line - add to buffer if we're in an operation
            if current_op is not None:
                raw_buffer.append(line)
                clean_line = self._remove_line_number(line)
                buffer.append(clean_line)

        # Handle unclosed operation
        if current_op is not None:
            errors.append(ParseError(
                len(lines),
                "",
                f"Unclosed operation: {current_op.type.value} for {current_op.file}"
            ))

        return ParseResult(operations=operations, errors=errors)

    def _remove_line_number(self, line: str) -> str:
        """Remove line number marker from end of line."""
        for pattern in self.LINE_NUMBER_PATTERNS:
            match = pattern.search(line)
            if match:
                return line[:match.start()]
        return line

    def extract_line_number(self, line: str) -> Optional[int]:
        """Extract line number from a code line."""
        for pattern in self.LINE_NUMBER_PATTERNS:
            match = pattern.search(line)
            if match:
                return int(match.group(1))
        return None

    @staticmethod
    def get_line_number_suffix(filename: str, line_num: int) -> str:
        """Get the appropriate line number suffix for a file type."""
        ext = filename.rsplit('.', 1)[-1].lower() if '.' in filename else ''

        if ext in ('py', 'sh', 'bash', 'zsh', 'yaml', 'yml', 'toml', 'ini', 'conf', 'rb', 'pl'):
            return f"  #Z{line_num}"
        elif ext in ('js', 'ts', 'jsx', 'tsx', 'java', 'c', 'cpp', 'h', 'hpp', 'cs', 'go', 'rs', 'swift', 'kt', 'scala'):
            return f"  //Z{line_num}"
        elif ext in ('html', 'htm', 'xml', 'svg'):
            return f"  <!--Z{line_num}-->"
        elif ext in ('css', 'scss', 'sass', 'less'):
            return f"  /*Z{line_num}*/"
        elif ext in ('sql',):
            return f"  --Z{line_num}"
        else:
            # Default to Python style
            return f"  #Z{line_num}"


def main():
    """Test the parser with example input."""
    test_input = """###FILE:src/calculator.py
###NEW
def add(a, b):  #Z1
    return a + b  #Z2
  #Z3
def multiply(a, b):  #Z4
    return a * b  #Z5
###END

###FILE:src/calculator.py
###REPLACE:Z4-Z5
def multiply(a, b):  #Z4
    \"\"\"Multipliziert zwei Zahlen.\"\"\"  #Z5
    return a * b  #Z6
###END
###RENUMBER

###FILE:src/calculator.py
###INSERT_AFTER:Z2
  #Z3
def subtract(a, b):  #Z4
    return a - b  #Z5
###END
###RENUMBER

###FILE:src/calculator.py
###DELETE:Z10-Z15
###RENUMBER
"""

    parser = DCTPParser()
    result = parser.parse(test_input)

    print("Operations found:")
    for op in result.operations:
        print(f"  {op}")

    if result.has_errors:
        print("\nErrors:")
        for error in result.errors:
            print(f"  Line {error.line_number}: {error.message}")
            print(f"    {error.line_content}")


if __name__ == "__main__":
    main()
