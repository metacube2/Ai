"""
DCTP Backup Manager - Handles backup and undo functionality.

Creates timestamped backups before operations and supports
restoring files to their previous state.
"""

import json
import os
import shutil
from dataclasses import dataclass, asdict
from datetime import datetime
from pathlib import Path
from typing import Optional


@dataclass
class FileBackup:
    """Represents a single file backup."""
    original: str
    backup: str
    existed: bool  # Whether the file existed before (for new file handling)


@dataclass
class BackupSession:
    """Represents a backup session (one execution run)."""
    timestamp: str
    files: list[FileBackup]


@dataclass
class BackupInfo:
    """Info about a backup for display purposes."""
    timestamp: str
    file_count: int
    files: list[str]


class BackupManager:
    """Manages file backups for undo functionality."""

    BACKUP_DIR_NAME = ".dctp_backups"
    MANIFEST_FILE = "manifest.json"
    MAX_SESSIONS = 50  # Keep last 50 sessions

    def __init__(self, project_path: str):
        """
        Initialize backup manager.

        Args:
            project_path: Base project directory
        """
        self.project_path = Path(project_path)
        self.backup_dir = self.project_path / self.BACKUP_DIR_NAME
        self.manifest_path = self.backup_dir / self.MANIFEST_FILE
        self._current_session: Optional[BackupSession] = None
        self._ensure_backup_dir()

    def _ensure_backup_dir(self) -> None:
        """Create backup directory if it doesn't exist."""
        self.backup_dir.mkdir(parents=True, exist_ok=True)

        # Create .gitignore in backup dir
        gitignore_path = self.backup_dir / ".gitignore"
        if not gitignore_path.exists():
            gitignore_path.write_text("*\n")

    def _load_manifest(self) -> dict:
        """Load the manifest file."""
        if self.manifest_path.exists():
            try:
                return json.loads(self.manifest_path.read_text())
            except (json.JSONDecodeError, IOError):
                return {"sessions": []}
        return {"sessions": []}

    def _save_manifest(self, manifest: dict) -> None:
        """Save the manifest file."""
        self.manifest_path.write_text(json.dumps(manifest, indent=2))

    def start_session(self) -> None:
        """Start a new backup session."""
        timestamp = datetime.now().strftime("%Y-%m-%dT%H:%M:%S")
        self._current_session = BackupSession(timestamp=timestamp, files=[])

    def backup(self, file_path: str) -> Optional[str]:
        """
        Create a backup of a file.

        Args:
            file_path: Path to the file (relative to project or absolute)

        Returns:
            Backup filename if successful, None if file doesn't exist
        """
        if self._current_session is None:
            self.start_session()

        # Normalize path
        if os.path.isabs(file_path):
            full_path = Path(file_path)
            rel_path = full_path.relative_to(self.project_path)
        else:
            rel_path = Path(file_path)
            full_path = self.project_path / rel_path

        # Check if file exists
        existed = full_path.exists()

        if existed:
            # Generate backup filename
            timestamp = datetime.now().strftime("%Y-%m-%d_%H%M%S")
            safe_name = str(rel_path).replace(os.sep, "_").replace("/", "_")
            backup_name = f"{timestamp}_{safe_name}"

            # Copy file to backup
            backup_path = self.backup_dir / backup_name
            shutil.copy2(full_path, backup_path)
        else:
            backup_name = ""

        # Add to current session
        self._current_session.files.append(FileBackup(
            original=str(rel_path),
            backup=backup_name,
            existed=existed
        ))

        return backup_name if existed else None

    def end_session(self) -> None:
        """End the current backup session and save to manifest."""
        if self._current_session is None or len(self._current_session.files) == 0:
            self._current_session = None
            return

        manifest = self._load_manifest()

        # Convert to dict for JSON storage
        session_dict = {
            "timestamp": self._current_session.timestamp,
            "files": [asdict(f) for f in self._current_session.files]
        }
        manifest["sessions"].append(session_dict)

        # Limit number of sessions
        if len(manifest["sessions"]) > self.MAX_SESSIONS:
            # Remove old sessions and their backup files
            old_sessions = manifest["sessions"][:-self.MAX_SESSIONS]
            for session in old_sessions:
                for file_info in session["files"]:
                    backup_file = self.backup_dir / file_info["backup"]
                    if backup_file.exists():
                        backup_file.unlink()
            manifest["sessions"] = manifest["sessions"][-self.MAX_SESSIONS:]

        self._save_manifest(manifest)
        self._current_session = None

    def restore_last(self) -> tuple[bool, list[str]]:
        """
        Restore files from the last backup session.

        Returns:
            Tuple of (success, list of restored files)
        """
        manifest = self._load_manifest()

        if not manifest["sessions"]:
            return False, []

        # Get last session
        last_session = manifest["sessions"].pop()
        restored_files = []

        for file_info in last_session["files"]:
            original_path = self.project_path / file_info["original"]

            if file_info["existed"]:
                # Restore from backup
                backup_path = self.backup_dir / file_info["backup"]
                if backup_path.exists():
                    # Ensure parent directory exists
                    original_path.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copy2(backup_path, original_path)
                    backup_path.unlink()  # Remove backup file
                    restored_files.append(file_info["original"])
            else:
                # File was newly created, delete it
                if original_path.exists():
                    original_path.unlink()
                    restored_files.append(f"{file_info['original']} (deleted)")

        self._save_manifest(manifest)
        return True, restored_files

    def list_backups(self) -> list[BackupInfo]:
        """
        List all backup sessions.

        Returns:
            List of BackupInfo objects, newest first
        """
        manifest = self._load_manifest()
        backups = []

        for session in reversed(manifest["sessions"]):
            files = [f["original"] for f in session["files"]]
            backups.append(BackupInfo(
                timestamp=session["timestamp"],
                file_count=len(files),
                files=files
            ))

        return backups

    def get_file_backup_path(self, file_path: str) -> Optional[Path]:
        """
        Get the backup path for a file from the most recent session.

        Args:
            file_path: Original file path (relative to project)

        Returns:
            Path to backup file if found, None otherwise
        """
        manifest = self._load_manifest()

        if not manifest["sessions"]:
            return None

        # Search from newest to oldest
        for session in reversed(manifest["sessions"]):
            for file_info in session["files"]:
                if file_info["original"] == file_path and file_info["existed"]:
                    backup_path = self.backup_dir / file_info["backup"]
                    if backup_path.exists():
                        return backup_path

        return None

    def clear_all_backups(self) -> int:
        """
        Clear all backups.

        Returns:
            Number of backup files deleted
        """
        count = 0
        if self.backup_dir.exists():
            for item in self.backup_dir.iterdir():
                if item.name != ".gitignore":
                    if item.is_file():
                        item.unlink()
                        count += 1
                    elif item.is_dir():
                        shutil.rmtree(item)
                        count += 1

        # Reset manifest
        self._save_manifest({"sessions": []})
        return count


def main():
    """Test the backup manager."""
    import tempfile

    # Create a temporary project directory
    with tempfile.TemporaryDirectory() as tmpdir:
        # Create some test files
        test_file = Path(tmpdir) / "test.py"
        test_file.write_text("print('hello')\n")

        # Initialize backup manager
        manager = BackupManager(tmpdir)

        # Start a session and backup the file
        manager.start_session()
        backup_name = manager.backup("test.py")
        print(f"Created backup: {backup_name}")

        # Modify the file
        test_file.write_text("print('modified')\n")
        print(f"File content after modification: {test_file.read_text()}")

        # End session
        manager.end_session()

        # List backups
        backups = manager.list_backups()
        print(f"Backup sessions: {len(backups)}")
        for b in backups:
            print(f"  {b.timestamp}: {b.file_count} files")

        # Restore
        success, restored = manager.restore_last()
        print(f"Restore successful: {success}")
        print(f"Restored files: {restored}")
        print(f"File content after restore: {test_file.read_text()}")


if __name__ == "__main__":
    main()
